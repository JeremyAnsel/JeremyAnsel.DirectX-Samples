
// The output patch size.  In this sample, it is also 16 control points.
#define OUTPUT_PATCH_SIZE 16

cbuffer cbPerFrame : register(b0)
{
    matrix g_mViewProjection;
    float3 g_vCameraPosWorld;
    float  g_fTessellationFactor;
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

struct DS_OUTPUT
{
    float4 vPosition        : SV_POSITION;
    float3 vWorldPos        : WORLDPOS;
    float3 vNormal            : NORMAL;
};

//--------------------------------------------------------------------------------------
float4 BernsteinBasis(float t)
{
    float invT = 1.0f - t;

    return float4(invT * invT * invT,
        3.0f * t * invT * invT,
        3.0f * t * t * invT,
        t * t * t);
}

//--------------------------------------------------------------------------------------
float4 dBernsteinBasis(float t)
{
    float invT = 1.0f - t;

    return float4(-3 * invT * invT,
        3 * invT * invT - 6 * t * invT,
        6 * t * invT - 3 * t * t,
        3 * t * t);
}

//--------------------------------------------------------------------------------------
float3 EvaluateBezier(const OutputPatch<HS_OUTPUT, OUTPUT_PATCH_SIZE> bezpatch,
    float4 BasisU,
    float4 BasisV)
{
    float3 Value = float3(0, 0, 0);
    Value = BasisV.x * (bezpatch[0].vPosition * BasisU.x + bezpatch[1].vPosition * BasisU.y + bezpatch[2].vPosition * BasisU.z + bezpatch[3].vPosition * BasisU.w);
    Value += BasisV.y * (bezpatch[4].vPosition * BasisU.x + bezpatch[5].vPosition * BasisU.y + bezpatch[6].vPosition * BasisU.z + bezpatch[7].vPosition * BasisU.w);
    Value += BasisV.z * (bezpatch[8].vPosition * BasisU.x + bezpatch[9].vPosition * BasisU.y + bezpatch[10].vPosition * BasisU.z + bezpatch[11].vPosition * BasisU.w);
    Value += BasisV.w * (bezpatch[12].vPosition * BasisU.x + bezpatch[13].vPosition * BasisU.y + bezpatch[14].vPosition * BasisU.z + bezpatch[15].vPosition * BasisU.w);

    return Value;
}

// The domain shader is run once per vertex and calculates the final vertex's position
// and attributes.  It receives the UVW from the fixed function tessellator and the
// control point outputs from the hull shader.  Since we are using the DirectX 11
// Tessellation pipeline, it is the domain shader's responsibility to calculate the
// final SV_POSITION for each vertex.  In this sample, we evaluate the vertex's
// position using a Bernstein polynomial and the normal is calculated as the cross
// product of the U and V derivatives.

// The input SV_DomainLocation to the domain shader comes from fixed function
// tessellator.  And the OutputPatch comes from the hull shader.  From these, you
// must calculate the final vertex position, color, texcoords, and other attributes.

// The output from the domain shader will be a vertex that will go to the video card's
// rasterization pipeline and get drawn to the screen.

[domain("quad")]
DS_OUTPUT main(HS_CONSTANT_DATA_OUTPUT input,
    float2 UV : SV_DomainLocation,
    const OutputPatch<HS_OUTPUT, OUTPUT_PATCH_SIZE> bezpatch)
{
    float4 BasisU = BernsteinBasis(UV.x);
    float4 BasisV = BernsteinBasis(UV.y);
    float4 dBasisU = dBernsteinBasis(UV.x);
    float4 dBasisV = dBernsteinBasis(UV.y);

    float3 WorldPos = EvaluateBezier(bezpatch, BasisU, BasisV);
    float3 Tangent = EvaluateBezier(bezpatch, dBasisU, BasisV);
    float3 BiTangent = EvaluateBezier(bezpatch, BasisU, dBasisV);
    float3 Norm = normalize(cross(Tangent, BiTangent));

    DS_OUTPUT Output;
    Output.vPosition = mul(float4(WorldPos, 1), g_mViewProjection);
    Output.vWorldPos = WorldPos;
    Output.vNormal = Norm;

    return Output;
}
