#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// This vertex shader computes standard transform and lighting, with no tessellation stages following
//--------------------------------------------------------------------------------------
PS_RenderSceneInput main(VS_RenderSceneInput I)
{
    PS_RenderSceneInput O;
    float3 f3NormalWorldSpace;
    
    // Transform the position from object space to homogeneous projection space
    O.f4Position = mul(float4(I.f3Position, 1.0f), g_f4x4WorldViewProjection);
    
    // Transform the normal from object space to world space    
    f3NormalWorldSpace = normalize(mul(I.f3Normal, (float3x3) g_f4x4World));
    
    // Calc diffuse color    
    O.f4Diffuse = g_f4MaterialDiffuseColor * g_f4LightDiffuse * max(0, dot(f3NormalWorldSpace, g_f4LightDir.xyz)) + g_f4MaterialAmbientColor;
    O.f4Diffuse.a = 1.0f;
    
    // Pass through texture coords
    O.f2TexCoord = I.f2TexCoord;
    
    return O;
}
