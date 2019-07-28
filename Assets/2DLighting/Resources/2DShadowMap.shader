Shader "Unlit/ShadowMap"
{
	Properties
	{
	}
	SubShader
	{
	Cull Off ZWrite Off ZTest Always
	BlendOp Max
	Blend One One
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"
		#include "2DShadowMapHelper.cginc"
			struct appdata
			{
				float3 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(_ShadowMap2DMVP, float4(v.vertex, 1.0));
				o.vertex.y = (_ShadowMap2DWriteRow)* o.vertex.w;
				return o;
			}

			float frag(v2f i) : SV_Target
			{
#if UNITY_REVERSED_Z
				return i.vertex.z;
#else
				return 1.0f - i.vertex.z;
#endif
			}
			ENDCG
		}
	}
}
