//
// PS for rendering shadow scene
//

#include "Common.hlsl"

float4 main(PSShadowIn input) : SV_Target
{
    return float4(g_vShadowColor.xyz, 0.1f);
}
