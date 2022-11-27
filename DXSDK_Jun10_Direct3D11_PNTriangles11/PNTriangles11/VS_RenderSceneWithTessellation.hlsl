#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// This vertex shader is a pass through stage, with HS, tessellation, and DS stages following
//--------------------------------------------------------------------------------------
HS_Input main(VS_RenderSceneInput I)
{
    HS_Input O;
    
    // Pass through world space position
    O.f3Position = mul(I.f3Position, (float3x3) g_f4x4World);
    
    // Pass through normalized world space normal    
    O.f3Normal = normalize(mul(I.f3Normal, (float3x3) g_f4x4World));
        
    // Pass through texture coordinates
    O.f2TexCoord = I.f2TexCoord;
    
    return O;
}
