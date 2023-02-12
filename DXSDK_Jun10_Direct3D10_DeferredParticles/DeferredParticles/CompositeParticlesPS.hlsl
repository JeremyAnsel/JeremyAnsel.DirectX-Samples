#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Composite partices into the scene
//--------------------------------------------------------------------------------------

float4 main(VS_SCREENOUTPUT input) : SV_TARGET
{
	// Load the particle normal data, opacity, and color
    float3 loadpos = float3(input.Position.xy, 0);
    float4 particlebuffer = g_txMeshTexture.Load(loadpos);
    float4 particlecolor = g_txParticleColor.Load(loadpos);
    
    // Recreate z-component of the normal
    float nz = sqrt(1 - particlebuffer.x * particlebuffer.x + particlebuffer.y * particlebuffer.y);
    float3 normal = float3(particlebuffer.xy, nz);
    float intensity = particlebuffer.z;

    // move normal into world space
    float3 worldnorm;
    worldnorm = -normal.x * g_vRight.xyz;
    worldnorm += normal.y * g_vUp.xyz;
    worldnorm += -normal.z * g_vForward.xyz;
    
    // light
    float lighting = max(0.1, dot(worldnorm, g_LightDir.xyz));
    
    float3 flashcolor = particlecolor.xyz * intensity;
    float3 lightcolor = particlecolor.xyz * lighting;
    float3 lerpcolor = lerp(lightcolor, flashcolor, intensity);
    float4 color = float4(lerpcolor, particlebuffer.a);
    
    return color;
}
