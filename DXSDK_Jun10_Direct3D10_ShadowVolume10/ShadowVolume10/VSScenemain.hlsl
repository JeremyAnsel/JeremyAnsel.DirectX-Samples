//
// VS for rendering basic textured and lit objects
//

#include "Common.hlsl"

#define LIGHT_FALLOFF 1.2f

PSSceneIn main(VSSceneIn input)
{
    PSSceneIn output = (PSSceneIn) 0;

    //output our final position in clipspace
    output.pos = mul(float4(input.pos, 1), g_mWorldViewProj);
    
    //world space normal
    float3 norm = mul(input.norm, (float3x3) g_mWorld);

    //find the light dir
    float3 wpos = mul(input.pos, (float3x3) g_mWorld);
    
    float3 lightDir = normalize(g_vLightPos.xyz - wpos);
    float lightLenSq = dot(lightDir, lightDir);
        
    output.color = saturate(dot(lightDir, norm)) *
                    (g_vLightColor / 15.0f) *
                    ((LIGHT_FALLOFF * LIGHT_FALLOFF)) / lightLenSq;
    
    //propogate the texture coordinate
    output.tex = input.tex;
    
    return output;
}
