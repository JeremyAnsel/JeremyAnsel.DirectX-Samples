//
// PS for rendering lit and textured triangles
//

#include "Common.hlsl"

Texture2D g_txDiffuse : register(t0);
SamplerState g_samLinear : register(s0);

float4 main(PSSceneIn input) : SV_Target
{
    float4 diffuse = g_txDiffuse.Sample(g_samLinear, input.tex) * input.color;
    return diffuse;
}
