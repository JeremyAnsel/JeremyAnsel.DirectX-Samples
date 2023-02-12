#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Render particle information into the screen
//--------------------------------------------------------------------------------------

float4 main(VS_PARTICLEOUTPUT input) : SV_TARGET
{
    float4 diffuse = g_txMeshTexture.Sample(g_samLinear, input.TextureUVI.xy);
	
	// unbias
    float3 norm = diffuse.xyz * 2 - 1;
	
	// rotate
    float3 rotnorm;
    float fSinTheta = input.SinCosThetaLife.x;
    float fCosTheta = input.SinCosThetaLife.y;
	
    rotnorm.x = fCosTheta * norm.x - fSinTheta * norm.y;
    rotnorm.y = fSinTheta * norm.x + fCosTheta * norm.y;
    rotnorm.z = norm.z;
	
	// rebias
    norm = rotnorm;
	
	// Fade
    float alpha = diffuse.a * (1.0f - input.SinCosThetaLife.z);
	
	// rebias	
    float intensity = input.TextureUVI.z * alpha;
	
	// move normal into world space
    float3 worldnorm;
    worldnorm = -norm.x * g_vRight.xyz;
    worldnorm += norm.y * g_vUp.xyz;
    worldnorm += -norm.z * g_vForward.xyz;
    
    float lighting = max(0.1, dot(worldnorm, g_LightDir.xyz));
    
    float3 flashcolor = input.Color.xyz * intensity;
    float3 lightcolor = input.Color.xyz * lighting;
    float3 lerpcolor = lerp(lightcolor, flashcolor, intensity);
    float4 color = float4(lerpcolor, alpha);
	
    return color;
}
