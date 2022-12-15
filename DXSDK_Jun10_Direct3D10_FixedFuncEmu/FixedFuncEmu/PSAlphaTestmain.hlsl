#include "Common.hlsl"

Texture2D g_txDiffuse : register(t0);
Texture2D g_txProjected : register(t1);

SamplerState g_samLinear : register(s0);

//
// PS for rendering with alpha test
//

float4 main(PSSceneIn input) : SV_Target
{
    float4 color = g_txDiffuse.Sample(g_samLinear, input.tex) * input.colorD;

    if (color.a < 0.5)
    {
        discard;
    }

    return color;
}
