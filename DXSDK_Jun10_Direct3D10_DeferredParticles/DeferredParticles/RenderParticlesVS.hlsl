#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Render particle information into the particle buffer
//--------------------------------------------------------------------------------------

VS_PARTICLEOUTPUT main(VS_PARTICLEINPUT input)
{
    VS_PARTICLEOUTPUT Output;
    
    // Standard transform
    Output.Position = mul(input.Position, g_mWorldViewProjection);
    Output.TextureUVI.xy = input.TextureUV;
    Output.Color = input.Color;
    
    // Get the world position
    float3 WorldPos = mul(input.Position, g_mWorld).xyz;

	// Loop over the glow lights (from the explosions) and light our particle
    float runningintensity = 0;
    uint count = g_NumGlowLights;

    for (uint i = 0; i < count; i++)
    {
        float3 delta = g_vGlowLightPosIntensity[i].xyz - WorldPos;
        float distSq = dot(delta, delta);
        float3 d = float3(1, /*sqrt(distSq)*/0, distSq);
		
        float fatten = 1.0 / dot(g_vGlowLightAttenuation.xyz, d);
		
        float intensity = fatten * g_vGlowLightPosIntensity[i].w * g_vGlowLightColor[i].w;
        runningintensity += intensity;
        Output.Color += intensity * g_vGlowLightColor[i];
    }

    Output.TextureUVI.z = runningintensity;
    
    // Rotate our texture coordinates
    float fRot = -input.fRot;
    Output.SinCosThetaLife.x = sin(fRot);
    Output.SinCosThetaLife.y = cos(fRot);
    Output.SinCosThetaLife.z = input.fLife;
    
    return Output;
}
