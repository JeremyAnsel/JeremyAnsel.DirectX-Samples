#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Render particle information into the particle buffer
//--------------------------------------------------------------------------------------

PBUFFER_OUTPUT main(VS_PARTICLEOUTPUT input)
{
    PBUFFER_OUTPUT output;
	
    float4 diffuse = g_txMeshTexture.Sample(g_samLinear, input.TextureUVI.xy);
	
	// unbias
    float3 norm = diffuse.xyz * 2 - 1;
	
	// rotate our texture coordinate and our normal basis
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
    float4 normalpha = float4(norm.xy * alpha, input.TextureUVI.z * alpha, alpha);

    output.color0 = normalpha;
    output.color1 = input.Color * alpha;
	
    return output;
}
