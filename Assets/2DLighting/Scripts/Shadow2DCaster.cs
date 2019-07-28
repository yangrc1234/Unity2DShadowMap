using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))]
public class Shadow2DCaster : MonoBehaviour
{
    private new SpriteRenderer renderer;
    private Mesh shadowMesh;
    private void Awake() {
        renderer = GetComponent<SpriteRenderer>();
        shadowMesh = new Mesh();
        shadowMesh.vertices = renderer.sprite.vertices.Select(t => new Vector3(t.x, t.y, 0.0f)).ToArray();
        var originalTriangles = renderer.sprite.triangles;
        var newTriangleIndicies = new List<int>();
        for (int i = 0; i < originalTriangles.Length / 3; i++) {
            newTriangleIndicies.Add(originalTriangles[3 * i]);
            newTriangleIndicies.Add(originalTriangles[3 * i + 1]);

            newTriangleIndicies.Add(originalTriangles[3 * i + 1]);
            newTriangleIndicies.Add(originalTriangles[3 * i + 2]);

            newTriangleIndicies.Add(originalTriangles[3 * i + 2]);
            newTriangleIndicies.Add(originalTriangles[3 * i]);
        }
        shadowMesh.SetIndices(newTriangleIndicies.ToArray(), MeshTopology.Lines, 0);
        shadowMesh.UploadMeshData(true);
    }

    public Mesh GetLineMesh() {
        return shadowMesh;
    }

    public static List<Shadow2DCaster> casters = new List<Shadow2DCaster>();
    private static Dictionary<Sprite, Mesh> shadowMeshCache;

    private void OnEnable() {
        casters.Add(this);
    }

    private void OnDisable() {
        casters.Remove(this);
    }
}
