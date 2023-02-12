#include "Common.hlsl"

float4 main(VS_MESHOUTPUT input) : SV_TARGET
{
    float3 normal = normalize(input.Normal);
	
    uint count = g_NumGlowLights;
    float4 lightColor = float4(0, 0, 0, 0);
    for (uint i = 0; i < count; i++)
    {
        float3 delta = g_vGlowLightPosIntensity[i].xyz - input.wPos;
        float distSq = dot(delta, delta);
        float dist = sqrt(distSq);
        float3 toLight = delta / dist;
        float3 d = float3(1, dist, distSq);
		
        float fatten = 1.0 / dot(g_vMeshLightAttenuation.xyz, d);
		
        float intensity = fatten * g_vGlowLightPosIntensity[i].w;
        lightColor += intensity * g_vGlowLightColor[i] * saturate(dot(toLight, normal));
    }
	
    float lighting = max(0.1, dot(normal, g_LightDir.xyz));
    return (lightColor + lighting.xxxx) * 0.9;
}
