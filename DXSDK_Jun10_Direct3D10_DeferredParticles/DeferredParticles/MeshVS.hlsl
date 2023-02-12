#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Composite partices into the scene
//--------------------------------------------------------------------------------------

VS_MESHOUTPUT main(VS_MESHINPUT input)
{
    VS_MESHOUTPUT Output;

    Output.Position = mul(input.Position, g_mWorldViewProjection);
    Output.wPos = mul(input.Position, g_mWorld).xyz;
    Output.Normal = mul(input.Normal, (float3x3) g_mWorld);
    Output.TextureUV = input.TextureUV;
    
    return Output;
}
