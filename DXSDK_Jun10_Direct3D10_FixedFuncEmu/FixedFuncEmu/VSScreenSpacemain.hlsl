#include "Common.hlsl"

//
// VS for rendering in screen space
//

PSSceneIn main(VSSceneIn input)
{
    PSSceneIn output = (PSSceneIn) 0;

    //output our final position
    output.pos.x = (input.pos.x / (g_viewportWidth / 2.0)) - 1;
    output.pos.y = -(input.pos.y / (g_viewportHeight / 2.0)) + 1;
    output.pos.z = input.pos.z;
    output.pos.w = 1;
    
    //propogate texture coordinate
    output.tex = input.tex;
    output.colorD = float4(1, 1, 1, 1);
    
    return output;
}
