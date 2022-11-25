#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Solid color shading pixel shader (used for wireframe overlay)
//--------------------------------------------------------------------------------------
float4 main(PS_INPUT Input) : SV_TARGET
{
    return g_vSolidColor;
}
