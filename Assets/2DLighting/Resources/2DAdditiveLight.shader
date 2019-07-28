Shader "Unlit/2DAdditiveLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "2DShadowMapHelper.cginc"

			float4x4 _LightM;
			float4 _LightParams;
			fixed4 _LightColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float4 worldPos:TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(_LightM, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float shadow = SampleShadow(i.worldPos);
				float distance = length(i.worldPos.xy - _LightParams.zw);
				float attenuation = (distance / _LightParams.x) * (distance / _LightParams.x);
				float3 light = _LightColor.rgb * _LightParams.y * saturate(1.0f - attenuation);
				return float4(light * (1.0f - shadow), 1.0) * _LightColor.a;
            }
            ENDCG
        }
    }
}
