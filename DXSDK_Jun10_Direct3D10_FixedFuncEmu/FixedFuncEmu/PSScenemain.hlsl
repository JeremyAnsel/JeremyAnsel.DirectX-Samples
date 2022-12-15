#include "Common.hlsl"

Texture2D g_txDiffuse : register(t0);
Texture2D g_txProjected : register(t1);

SamplerState g_samLinear : register(s0);

//
// PS for rendering with clip planes
//

float4 main(PSSceneIn input) : SV_Target
{
    //calculate the fog factor  
    float fog = CalcFogFactor(input.fogDist);
    
    //calculate the color based off of the normal, textures, etc
    float4 normalColor = g_txDiffuse.Sample(g_samLinear, input.tex) * input.colorD + input.colorS;
    
    //calculate the color from the projected texture
    float4 cookieCoord = mul(float4(input.wPos, 1), g_mLightViewProj);
    //since we don't have texldp, we must perform the w divide ourselves befor the texture lookup
    cookieCoord.xy = 0.5 * cookieCoord.xy / cookieCoord.w + float2(0.5, 0.5);
    float4 cookieColor = float4(0, 0, 0, 0);
    if (cookieCoord.z > 0)
    {
        cookieColor = g_txProjected.Sample(g_samLinear, cookieCoord.xy);
    }
    
    //for standard light-modulating effects just multiply normalcolor and coookiecolor
    normalColor += cookieColor;
    
    return fog * normalColor + (1.0 - fog) * g_fogColor;
}
