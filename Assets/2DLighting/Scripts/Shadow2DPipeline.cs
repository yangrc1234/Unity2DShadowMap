using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class Shadow2DPipeline : MonoBehaviour {
    private Material shadowMapMaterial;
    private Material lightingMaterial;
    private RenderTexture shadowMap;
    private CommandBuffer shadowMapCmdBuffer;
    private CommandBuffer lightingCmdBuffer;
    private MaterialPropertyBlock propertyBlock;
    private Mesh lightingQuad;

    private const int MAX_LIGHTS_COUNT = 64;
    private const int SHADOW_MAP_WIDTH = 256;
    private int ShadowMapHeight {
        get {
            return MAX_LIGHTS_COUNT * 4;
        }
    }

    private void Start() {
        propertyBlock = new MaterialPropertyBlock();
        shadowMap = new RenderTexture(SHADOW_MAP_WIDTH, ShadowMapHeight, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        shadowMap.filterMode = FilterMode.Point;
        shadowMap.antiAliasing = 1;
        shadowMap.anisoLevel = 0;
        shadowMapMaterial = new Material(Resources.Load<Shader>("2DShadowMap"));
        lightingMaterial = new Material(Resources.Load<Shader>("2DAdditiveLight"));
        {
            lightingQuad = new Mesh();
            lightingQuad.vertices = new Vector3[] {
                new Vector3(-1.0f, -1.0f),
                new Vector3(-1.0f, 1.0f),
                new Vector3(1.0f, -1.0f),
                new Vector3(1.0f, 1.0f),
            };
            lightingQuad.triangles = new int[] {
                0, 1, 2, 2, 1, 3
            };
            lightingQuad.UploadMeshData(true);
        }
    }

    private void OnPreRender() {

        var cam = GetComponent<Camera>();
        var shadowCasters = Shadow2DCaster.casters;
        var lights = Shadow2DLight.lights;

        {   //Update shadow map rendering command buffer.
            if (shadowMapCmdBuffer == null) {
                shadowMapCmdBuffer = new CommandBuffer();
                cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, shadowMapCmdBuffer);
            }
            shadowMapCmdBuffer.Clear();
            /*
             * Shadow caster mesh is line-strip mesh.
             * 
             * In shadow map, each 4 rows belong to one light, corresponding to depth in (0, 90), (90, 180)...(270, 360).
             * 
             * To render shadow mesh at correct position, a naive MVP matrix is used.
             * Then, after transforming to clip space, manually edit y component to the corresponding row.
             */
            shadowMapCmdBuffer.SetRenderTarget(shadowMap);
            shadowMapCmdBuffer.ClearRenderTarget(true, true, Color.black);
            shadowMapCmdBuffer.SetGlobalVector(ShaderKeys.ShadowMap2DSize, new Vector4(1.0f / shadowMap.width, 1.0f / shadowMap.height, shadowMap.width, shadowMap.height));

            //Calculate V and P for each light.
            foreach (var light in lights) {
                for (int i = 0; i < 4; i++) {   //four directions.

                    //This TRS here calculates light->world matrix.
                    //The Quaternion.Euler(90.0f * i, 90.0f, 270.0f) aligns light to right, down, left, up.
                    //-1.0f in z-component in scale corrects camera forward direction.
                    var V = Matrix4x4.TRS(light.transform.position, Quaternion.Euler(90.0f * i, 90.0f, 270.0f), new Vector3(1.0f, 1.0f, -1.0f));
                    //Inverse it, so it's a world->light matrix.
                    V = V.inverse;

                    var P = Matrix4x4.Perspective(90.0f, 1.0f, 0.01f, 10.0f);
                    P = GL.GetGPUProjectionMatrix(P, true);
                    light.V[i] = V;
                    light.P = P;
                }
            }

            foreach (var shadowCaster in shadowCasters) {
                var M = shadowCaster.transform.localToWorldMatrix;
                for (int iLight = 0; iLight < Mathf.Min(MAX_LIGHTS_COUNT, lights.Count); iLight++) {
                    //Render shadow map of each light, by drawing shadow line mesh onto the shadowmap.
                    for (int iDir = 0; iDir < 4; iDir++) { //Four directions, right, down, left, up.

                        var light = lights[iLight];
                        var MVP = light.P * light.V[iDir] * M;

                        shadowMapCmdBuffer.SetGlobalMatrix(ShaderKeys.ShadowMap2DMVP, MVP);
                        float writeClipSpaceY = (iLight * 4 + iDir + 0.5f) / (float)(ShadowMapHeight);
                        shadowMapCmdBuffer.SetGlobalFloat(ShaderKeys.ShadowMap2DWriteRow, (writeClipSpaceY - 0.5f) * 2.0f);
                        shadowMapCmdBuffer.DrawMesh(shadowCaster.GetLineMesh(), Matrix4x4.identity /*Use our own MVP matrix*/, shadowMapMaterial);
                    }

                }
            }
        }

        {   //Update lighting command buffer.

            //Draw a "light quad" for each light.
            //
            //There's lots of ways to add light to 2d scene.
            //Here we just use an additive "layer"
            //Edit to what you like.
            if (lightingCmdBuffer == null) {
                lightingCmdBuffer = new CommandBuffer();
                lightingCmdBuffer.name = "2D Lighting";
                cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, lightingCmdBuffer);
            }
            lightingCmdBuffer.Clear();

            lightingCmdBuffer.SetGlobalTexture(ShaderKeys.ShadowMap2D, shadowMap);
            for (int iLight = 0; iLight < Mathf.Min(MAX_LIGHTS_COUNT, lights.Count); iLight++) {
                var light = lights[iLight];
                lightingCmdBuffer.SetGlobalVector(ShaderKeys.LightParams, new Vector4(light.range, light.intensity, light.transform.position.x, light.transform.position.y));
                lightingCmdBuffer.SetGlobalVector(ShaderKeys.LightColor, light.color);

                lightingCmdBuffer.SetGlobalFloat(ShaderKeys.ShadowMap2DLightIndex, (float)iLight);
                for (int iVP = 0; iVP < ShaderKeys._ShadowMap2DVP.Length; iVP++) {
                    lightingCmdBuffer.SetGlobalMatrix(ShaderKeys._ShadowMap2DVP[iVP], light.P * light.V[iVP]);
                }
                var M = Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one * light.range);
                lightingCmdBuffer.SetGlobalMatrix(ShaderKeys.LightM, M);
                lightingCmdBuffer.DrawMesh(lightingQuad, M, lightingMaterial);
            }
        }
    }

    public static class ShaderKeys {
        public static int ShadowMap2DSize = Shader.PropertyToID("_ShadowMap2DSize");
        public static int ShadowMap2DMVP = Shader.PropertyToID("_ShadowMap2DMVP");
        public static int ShadowMap2DWriteRow = Shader.PropertyToID("_ShadowMap2DWriteRow"); 
        public static int ShadowMap2D = Shader.PropertyToID("_ShadowMap2D");
        public static int ShadowMap2DLightIndex = Shader.PropertyToID("_ShadowMap2DLightIndex");
        public static int[] _ShadowMap2DVP = new int[] {
            Shader.PropertyToID("_ShadowMap2DVP_Right"),
            Shader.PropertyToID("_ShadowMap2DVP_Down"),
            Shader.PropertyToID("_ShadowMap2DVP_Left"),
            Shader.PropertyToID("_ShadowMap2DVP_Up"),
        };
        public static int LightM = Shader.PropertyToID("_LightM");
        public static int LightParams = Shader.PropertyToID("_LightParams");
        public static int LightColor = Shader.PropertyToID("_LightColor");
    }
}
