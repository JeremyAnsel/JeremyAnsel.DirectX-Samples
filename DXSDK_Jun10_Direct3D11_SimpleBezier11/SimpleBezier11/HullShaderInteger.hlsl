
// The input patch size.  In this sample, it is 16 control points.
// This value should match the call to IASetPrimitiveTopology()
#define INPUT_PATCH_SIZE 16

// The output patch size.  In this sample, it is also 16 control points.
#define OUTPUT_PATCH_SIZE 16

cbuffer cbPerFrame : register(b0)
{
    matrix g_mViewProjection;
    float3 g_vCameraPosWorld;
    float  g_fTessellationFactor;
};

struct VS_CONTROL_POINT_OUTPUT
{
    float3 vPosition        : POSITION;
};

struct HS_CONSTANT_DATA_OUTPUT
{
    float Edges[4]             : SV_TessFactor;
    float Inside[2]            : SV_InsideTessFactor;
};

struct HS_OUTPUT
{
    float3 vPosition           : BEZIERPOS;
};

// This constant hull shader is executed once per patch.  For the simple Mobius strip
// model, it will be executed 4 times.  In this sample, we set the tessellation factor
// via SV_TessFactor and SV_InsideTessFactor for each patch.  In a more complex scene,
// you might calculate a variable tessellation factor based on the camera's distance.

HS_CONSTANT_DATA_OUTPUT BezierConstantHS(InputPatch<VS_CONTROL_POINT_OUTPUT, INPUT_PATCH_SIZE> ip,
    uint PatchID : SV_PrimitiveID)
{
    HS_CONSTANT_DATA_OUTPUT Output;

    float TessAmount = g_fTessellationFactor;

    Output.Edges[0] = Output.Edges[1] = Output.Edges[2] = Output.Edges[3] = TessAmount;
    Output.Inside[0] = Output.Inside[1] = TessAmount;

    return Output;
}

// The hull shader is called once per output control point, which is specified with
// outputcontrolpoints.  For this sample, we take the control points from the vertex
// shader and pass them directly off to the domain shader.  In a more complex scene,
// you might perform a basis conversion from the input control points into a Bezier
// patch, such as the SubD11 Sample.

// The input to the hull shader comes from the vertex shader

// The output from the hull shader will go to the domain shader.
// The tessellation factor, topology, and partition mode will go to the fixed function
// tessellator stage to calculate the UVW and domain points.

[domain("quad")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(OUTPUT_PATCH_SIZE)]
[patchconstantfunc("BezierConstantHS")]
HS_OUTPUT main(InputPatch<VS_CONTROL_POINT_OUTPUT, INPUT_PATCH_SIZE> p,
    uint i : SV_OutputControlPointID,
    uint PatchID : SV_PrimitiveID)
{
    HS_OUTPUT Output;
    Output.vPosition = p[i].vPosition;
    return Output;
}
