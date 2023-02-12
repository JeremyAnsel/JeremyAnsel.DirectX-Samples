#include "Common.hlsl"

VS_MESHOUTPUT main(VS_MESHINPUT input, uint instanceID : SV_INSTANCEID)
{
    VS_MESHOUTPUT Output;
	
    float4 wPos = mul(input.Position, g_mWorldInst[instanceID]);
    Output.Position = mul(wPos, g_mViewProj);
    Output.wPos = wPos.xyz;
    Output.Normal = mul(input.Normal, (float3x3) g_mWorldInst[instanceID]);
    Output.TextureUV = input.TextureUV;
    
    return Output;
}
