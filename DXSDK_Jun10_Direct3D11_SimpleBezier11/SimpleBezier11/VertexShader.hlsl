
struct VS_CONTROL_POINT_INPUT
{
    float3 vPosition        : POSITION;
};

struct VS_CONTROL_POINT_OUTPUT
{
    float3 vPosition        : POSITION;
};

// This simple vertex shader passes the control points straight through to the
// hull shader.  In a more complex scene, you might transform the control points
// or perform skinning at this step.
// The input to the vertex shader comes from the vertex buffer.
// The output from the vertex shader will go into the hull shader.

VS_CONTROL_POINT_OUTPUT main(VS_CONTROL_POINT_INPUT Input)
{
    VS_CONTROL_POINT_OUTPUT Output;

    Output.vPosition = Input.vPosition;

    return Output;
}
