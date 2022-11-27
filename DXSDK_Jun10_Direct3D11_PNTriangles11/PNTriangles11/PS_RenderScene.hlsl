#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// This shader outputs the pixel's color by passing through the lit 
// diffuse material color
//--------------------------------------------------------------------------------------
PS_RenderOutput main(PS_RenderSceneInput I)
{
    PS_RenderOutput O;
    
    O.f4Color = I.f4Diffuse;
    
    return O;
}
