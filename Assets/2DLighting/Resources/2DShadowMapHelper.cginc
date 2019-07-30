#ifndef __2D_SHADOW_MAP_HELPER__
#define __2D_SHADOW_MAP_HELPER__

float2 _ShadowMap2DSize;

//Used during rendering shadowmap.
float4x4 _ShadowMap2DMVP;
float _ShadowMap2DWriteRow;

//Used during sampling shadowmap
sampler2D _ShadowMap2D;
float _ShadowMap2DLightIndex;
float4x4 _ShadowMap2DVP_Right;
float4x4 _ShadowMap2DVP_Down;
float4x4 _ShadowMap2DVP_Left;
float4x4 _ShadowMap2DVP_Up;

//Convert raw platform-dependent z to uniform range(1, 0)
float ConvertClipPosZTo10(float rawZ) {
#if UNITY_REVERSED_Z	
	return rawZ / (UNITY_NEAR_CLIP_VALUE);
#else
	rawZ = (1.0f - rawZ) / (1.0 - UNITY_NEAR_CLIP_VALUE);
	return rawZ;
#endif
}

float GetShadowMapVStartAndInterval(out float interval) {
	float VStart = (_ShadowMap2DLightIndex * 4 + 0.5)* _ShadowMap2DSize.y;
	interval = _ShadowMap2DSize.y;

#if UNITY_UV_STARTS_AT_TOP
	interval *= -1.0f;
	VStart = 1.0f - VStart;
#endif
	return VStart;
}

float2 GetClipPosXZ(float2 worldPos, float4x4 vp, out half outofbound) {
	float4 clipPos = mul(vp, float4(worldPos, 0.0, 1.0));
	clipPos /= clipPos.w;
	clipPos.z = ConvertClipPosZTo10(clipPos.z);
	outofbound = (abs(clipPos.x) - 1.0) > 0.0 ? 1.0 : 0.0;
	outofbound += clipPos.z - saturate(clipPos.z) != 0.0 ? 1.0 : 0.0;
	outofbound = saturate(outofbound);
	return float2(clipPos.x, clipPos.z);
}

float SampleShadow(float2 worldPos) {
	float shadowMapInterval;
	float shadowMapVStart = GetShadowMapVStartAndInterval(shadowMapInterval);

	//Calculate clip pos XZ on four directions.
	float2 clipPosXZ[4];
	half4 outofbound;
	clipPosXZ[0] = GetClipPosXZ(worldPos, _ShadowMap2DVP_Right, outofbound[0]);
	clipPosXZ[1] = GetClipPosXZ(worldPos, _ShadowMap2DVP_Down, outofbound[1]);
	clipPosXZ[2] = GetClipPosXZ(worldPos, _ShadowMap2DVP_Left, outofbound[2]);
	clipPosXZ[3] = GetClipPosXZ(worldPos, _ShadowMap2DVP_Up, outofbound[3]);
	
	//Sample shadowmap.
	float4 inShadow;
	UNITY_UNROLL
	for (int i = 0; i < 4; i++) {
		float2 shadowMapUV = float2((clipPosXZ[i].x + 1.0f) / 2.0f, shadowMapVStart + i * shadowMapInterval);
		inShadow[i] = tex2D(_ShadowMap2D, shadowMapUV) > clipPosXZ[i].y ? 1.0 : 0.0;
	}

	float shadow = saturate(dot(inShadow, 1.0 - outofbound)); //Clip all shadow values out of bound.
	
	return shadow;
}

#endif