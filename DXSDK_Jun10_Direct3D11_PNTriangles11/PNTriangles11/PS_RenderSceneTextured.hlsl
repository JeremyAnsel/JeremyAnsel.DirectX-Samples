#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// This shader outputs the pixel's color by passing through the lit 
// diffuse material color & modulating with the diffuse texture
//--------------------------------------------------------------------------------------
PS_RenderOutput main(PS_RenderSceneInput I)
{
    PS_RenderOutput O;
    
    O.f4Color = g_txDiffuse.Sample(g_SampleLinear, I.f2TexCoord) * I.f4Diffuse;
    
    return O;
}
