#include "Common.hlsl"
#include "Compute.hlsl"

//--------------------------------------------------------------------------------------
// Bezier evaluation domain shader section
//--------------------------------------------------------------------------------------
struct DS_OUTPUT
{
    float3 vWorldPos : POSITION;
    float3 vNormal : NORMAL;
    float2 vUV : TEXCOORD;
    float3 vTangent : TANGENT;
    float3 vBiTangent : BITANGENT;
    
    float4 vPosition : SV_POSITION;
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
float3 EvaluateBezier(const OutputPatch<BEZIER_CONTROL_POINT, 16> bezpatch,
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

//--------------------------------------------------------------------------------------
float3 EvaluateBezierTan(const float3 bezpatch[16],
                          float4 BasisU,
                          float4 BasisV)
{
    float3 Value = float3(0, 0, 0);
    Value = BasisV.x * (bezpatch[0] * BasisU.x + bezpatch[1] * BasisU.y + bezpatch[2] * BasisU.z + bezpatch[3] * BasisU.w);
    Value += BasisV.y * (bezpatch[4] * BasisU.x + bezpatch[5] * BasisU.y + bezpatch[6] * BasisU.z + bezpatch[7] * BasisU.w);
    Value += BasisV.z * (bezpatch[8] * BasisU.x + bezpatch[9] * BasisU.y + bezpatch[10] * BasisU.z + bezpatch[11] * BasisU.w);
    Value += BasisV.w * (bezpatch[12] * BasisU.x + bezpatch[13] * BasisU.y + bezpatch[14] * BasisU.z + bezpatch[15] * BasisU.w);
    
    return Value;
}

//--------------------------------------------------------------------------------------
// Compute a two full tangent patches from the Tangent corner data created in the
// HS constant data function.
//--------------------------------------------------------------------------------------
void CreatTangentPatches(in HS_CONSTANT_DATA_OUTPUT input,
                        const OutputPatch<BEZIER_CONTROL_POINT, 16> bezpatch,
                        out float3 TanU[16],
                        out float3 TanV[16])
{
    TanV[0] = input.vTanVCorner[0];
    TanV[3] = input.vTanVCorner[1];
    TanV[15] = input.vTanVCorner[2];
    TanV[12] = input.vTanVCorner[3];
    
    TanU[0] = input.vTanUCorner[0];
    TanU[3] = input.vTanUCorner[1];
    TanU[15] = input.vTanUCorner[2];
    TanU[12] = input.vTanUCorner[3];
    
    float fCWts[4];
    fCWts[0] = input.vCWts.x;
    fCWts[1] = input.vCWts.y;
    fCWts[2] = input.vCWts.z;
    fCWts[3] = input.vCWts.w;

    float3 vCorner[4];
    float3 vCornerLocal[4];
    
    vCorner[0] = TanV[0];
    vCorner[1] = TanV[3];
    vCorner[2] = TanV[15];
    vCorner[3] = TanV[12];
    vCornerLocal[0] = TanU[0];
    vCornerLocal[1] = TanU[3];
    vCornerLocal[2] = TanU[12];
    vCornerLocal[3] = TanU[15];

    ComputeTanPatch(bezpatch, TanU, fCWts, vCorner, vCornerLocal, 1, 4);

    fCWts[3] = input.vCWts.y;
    fCWts[1] = input.vCWts.w;

    vCorner[0] = TanU[0];
    vCorner[3] = TanU[3];
    vCorner[2] = TanU[15];
    vCorner[1] = TanU[12];
    vCornerLocal[0] = TanV[0];
    vCornerLocal[1] = TanV[12];
    vCornerLocal[2] = TanV[3];
    vCornerLocal[3] = TanV[15];

    ComputeTanPatch(bezpatch, TanV, fCWts, vCorner, vCornerLocal, 4, 1);
}

//--------------------------------------------------------------------------------------
// For each input UV (from the Tessellator), evaluate the Bezier patch at this position.
//--------------------------------------------------------------------------------------
[domain("quad")]
DS_OUTPUT main(HS_CONSTANT_DATA_OUTPUT input,
                        float2 UV : SV_DomainLocation,
                        const OutputPatch<BEZIER_CONTROL_POINT, 16> bezpatch)
{
    float4 BasisU = BernsteinBasis(UV.x);
    float4 BasisV = BernsteinBasis(UV.y);
    
    float3 WorldPos = EvaluateBezier(bezpatch, BasisU, BasisV);
    
    float3 TanU[16];
    float3 TanV[16];
    CreatTangentPatches(input, bezpatch, TanU, TanV);
    float3 Tangent = EvaluateBezierTan(TanU, BasisU, BasisV);
    float3 BiTangent = EvaluateBezierTan(TanV, BasisU, BasisV);
    
    // To see what the patch looks like without using the tangent patches to fix the normals, uncomment this section
    /*
    float4 dBasisU = dBernsteinBasis( UV.x );
    float4 dBasisV = dBernsteinBasis( UV.y );
    Tangent = EvaluateBezier( bezpatch, dBasisU, BasisV );
    BiTangent = EvaluateBezier( bezpatch, BasisU, dBasisV );
    */
    
    float3 Norm = normalize(cross(Tangent, BiTangent));

    DS_OUTPUT Output;
    Output.vNormal = Norm;
    
    // Evalulate the tangent vectors through bilinear interpolation.
    // These tangents are the texture-space tangents.  They should not be confused with the parametric
    // tangents that we use to get the normals for the bicubic patch.
    float3 TextureTanU0 = input.vTangent[0];
    float3 TextureTanU1 = input.vTangent[1];
    float3 TextureTanU2 = input.vTangent[2];
    float3 TextureTanU3 = input.vTangent[3];
    
    float3 UVbottom = lerp(TextureTanU0, TextureTanU1, UV.x);
    float3 UVtop = lerp(TextureTanU3, TextureTanU2, UV.x);
    float3 Tan = lerp(UVbottom, UVtop, UV.y);

    Output.vTangent = Tan;

    // This is an optimization.  We assume that the UV mapping of the mesh will result in a "relatively" orthogonal
    // tangent basis.  If we assume this, then we can avoid fetching and bilerping the BiTangent along with the tangent.
    Output.vBiTangent = cross(Norm, Tan);

    // bilerp the texture coordinates    
    float2 tex0 = input.vUV[0];
    float2 tex1 = input.vUV[1];
    float2 tex2 = input.vUV[2];
    float2 tex3 = input.vUV[3];
        
    float2 bottom = lerp(tex0, tex1, UV.x);
    float2 top = lerp(tex3, tex2, UV.x);
    float2 TexUV = lerp(bottom, top, UV.y);
    Output.vUV = TexUV;
    
    if (g_fDisplacementHeight > 0)
    {
        // On this sample displacement can go into or out of the mesh.  This is why we bias the heigh amount.
        float height = g_fDisplacementHeight * (g_txHeight.SampleLevel(g_samPoint, TexUV, 0).a * 2 - 1);
        float3 WorldPosMiddle = Norm * height;
        WorldPos += WorldPosMiddle;
    }
    
    Output.vPosition = mul(float4(WorldPos, 1), g_mViewProjection);
    Output.vWorldPos = WorldPos;
    
    return Output;
}
