#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Composite partices into the scene
//--------------------------------------------------------------------------------------

VS_SCREENOUTPUT main(float4 Position : POSITION)
{
    VS_SCREENOUTPUT Output;

    Output.Position = Position;
    
    return Output;
}
