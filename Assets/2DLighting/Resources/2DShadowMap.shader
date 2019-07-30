Shader "Unlit/ShadowMap"
{
	Properties
	{
	}
	SubShader
	{
	Cull Off ZWrite Off ZTest Always
	BlendOp Max
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
				float4 vertex_a : TEXCOORD0;	//A duplicate of vertex. vertex can't be directly used, it doesn't behave same on different API.
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(_ShadowMap2DMVP, float4(v.vertex, 1.0));
				o.vertex.y = (_ShadowMap2DWriteRow)* o.vertex.w;
				o.vertex_a = o.vertex;
				return o;
			}

			float frag(v2f i) : SV_Target
			{
				i.vertex_a /= i.vertex_a.w;
				return ConvertClipPosZTo10(i.vertex_a.z);
			}
			ENDCG
		}
	}
}
