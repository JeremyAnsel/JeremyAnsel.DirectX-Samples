//
// VS for sending information to the shadow GS
//

#include "Common.hlsl"

GSShadowIn main(VSSceneIn input)
{
    GSShadowIn output = (GSShadowIn) 0;

    //output our position in world space
    float4 pos = mul(float4(input.pos, 1), g_mWorld);
    output.pos = pos.xyz;
    
    //world space normal
    output.norm = mul(input.norm, (float3x3) g_mWorld);
    
    return output;
}
