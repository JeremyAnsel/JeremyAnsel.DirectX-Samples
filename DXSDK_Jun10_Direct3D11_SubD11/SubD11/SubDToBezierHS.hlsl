#include "Common.hlsl"
#include "Compute.hlsl"

//--------------------------------------------------------------------------------------
// Constant data function for the SubDToBezierHS.  This is executed once per patch.
//--------------------------------------------------------------------------------------
HS_CONSTANT_DATA_OUTPUT SubDToBezierConstantsHS(InputPatch<VS_CONTROL_POINT_OUTPUT, MAX_POINTS> ip,
                                                 uint PatchID : SV_PrimitiveID)
{
    HS_CONSTANT_DATA_OUTPUT Output;
    
    float TessAmount = g_fTessellationFactor;

    Output.Edges[0] = Output.Edges[1] = Output.Edges[2] = Output.Edges[3] = TessAmount;
    Output.Inside[0] = Output.Inside[1] = TessAmount;
    
    Output.vTangent[0] = ip[0].vTangent;
    Output.vTangent[1] = ip[1].vTangent;
    Output.vTangent[2] = ip[2].vTangent;
    Output.vTangent[3] = ip[3].vTangent;
    
    Output.vUV[0] = ip[0].vUV;
    Output.vUV[1] = ip[1].vUV;
    Output.vUV[2] = ip[2].vUV;
    Output.vUV[3] = ip[3].vUV;
    
    // Compute part of our tangent patch here
    uint Val[4];
    uint Prefixes[4];
    LoadValenceAndPrefixData(PatchID, Val, Prefixes);

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float3 CornerB, CornerU, CornerV;
        ComputeCornerVertex(i, CornerB, CornerU, CornerV, ip, Val, Prefixes);
        Output.vTanUCorner[i] = CornerU;
        Output.vTanVCorner[i] = CornerV;
    }
    
    float fCWts[4];
    Output.vCWts.x = g_fCi[Val[0] - 3];
    Output.vCWts.y = g_fCi[Val[1] - 3];
    Output.vCWts.z = g_fCi[Val[2] - 3];
    Output.vCWts.w = g_fCi[Val[3] - 3];
    
    return Output;
}

//--------------------------------------------------------------------------------------
// HS for SubDToBezier.  This outputcontrolpoints(16) specifies that we will produce
// 16 control points.  Therefore this function will be invoked 16x, one for each output
// control point.
//
// !! PERFORMANCE NOTE: This hull shader is written for maximum readability, and its
// performance is not expected to be optimal on D3D11 hardware.  The switch statement
// below that determines the codepath for each patch control point generates sub-optimal
// code for parallel execution on the GPU.  A future implementation of this hull shader
// will combine the 16 codepaths and 3 variants (corner, edge, interior) into one shared
// codepath; this change is expected to increase performance at the expense of readability.
//--------------------------------------------------------------------------------------
[domain("quad")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(16)]
[patchconstantfunc("SubDToBezierConstantsHS")]
BEZIER_CONTROL_POINT main(InputPatch<VS_CONTROL_POINT_OUTPUT, MAX_POINTS> p,
                                     uint i : SV_OutputControlPointID,
                                     uint PatchID : SV_PrimitiveID)
{
    // Valences and prefixes are loaded from a buffer
    uint Val[4];
    uint Prefixes[4];
    LoadValenceAndPrefixData(PatchID, Val, Prefixes);
    
    float3 CornerB = float3(0, 0, 0);
    float3 CornerU = float3(0, 0, 0);
    float3 CornerV = float3(0, 0, 0);
    
    BEZIER_CONTROL_POINT Output;
    Output.vPosition = float3(0, 0, 0);
    
    // !! PERFORMANCE NOTE: As mentioned above, this switch statement generates
    // inefficient code for the sake of readability.
    switch (i)
    {
    // Interior vertices
        case 5:
            Output.vPosition = ComputeInteriorVertex(0, Val, p);
            break;
        case 6:
            Output.vPosition = ComputeInteriorVertex(1, Val, p);
            break;
        case 10:
            Output.vPosition = ComputeInteriorVertex(2, Val, p);
            break;
        case 9:
            Output.vPosition = ComputeInteriorVertex(3, Val, p);
            break;
        
    // Corner vertices
        case 0:
            ComputeCornerVertex(0, CornerB, CornerU, CornerV, p, Val, Prefixes);
            Output.vPosition = CornerB;
            break;
        case 3:
            ComputeCornerVertex(1, CornerB, CornerU, CornerV, p, Val, Prefixes);
            Output.vPosition = CornerB;
            break;
        case 15:
            ComputeCornerVertex(2, CornerB, CornerU, CornerV, p, Val, Prefixes);
            Output.vPosition = CornerB;
            break;
        case 12:
            ComputeCornerVertex(3, CornerB, CornerU, CornerV, p, Val, Prefixes);
            Output.vPosition = CornerB;
            break;
        
    // Edge vertices
        case 1:
            Output.vPosition = ComputeEdgeVertex(0, p, Val, Prefixes);
            break;
        case 2:
            Output.vPosition = ComputeEdgeVertex(1, p, Val, Prefixes);
            break;
        case 13:
            Output.vPosition = ComputeEdgeVertex(2, p, Val, Prefixes);
            break;
        case 14:
            Output.vPosition = ComputeEdgeVertex(3, p, Val, Prefixes);
            break;
        case 4:
            Output.vPosition = ComputeEdgeVertex(4, p, Val, Prefixes);
            break;
        case 8:
            Output.vPosition = ComputeEdgeVertex(5, p, Val, Prefixes);
            break;
        case 7:
            Output.vPosition = ComputeEdgeVertex(6, p, Val, Prefixes);
            break;
        case 11:
            Output.vPosition = ComputeEdgeVertex(7, p, Val, Prefixes);
            break;
    }
    
    return Output;
}
