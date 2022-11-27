#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// This domain shader applies contol point weighting to the barycentric coords produced by the FF tessellator 
//--------------------------------------------------------------------------------------
[domain("tri")]
DS_Output main(HS_ConstantOutput HSConstantData, const OutputPatch<HS_ControlPointOutput, 3> I, float3 f3BarycentricCoords : SV_DomainLocation)
{
    DS_Output O = (DS_Output) 0;

    // The barycentric coordinates
    float fU = f3BarycentricCoords.x;
    float fV = f3BarycentricCoords.y;
    float fW = f3BarycentricCoords.z;

    // Precompute squares and squares * 3 
    float fUU = fU * fU;
    float fVV = fV * fV;
    float fWW = fW * fW;
    float fUU3 = fUU * 3.0f;
    float fVV3 = fVV * 3.0f;
    float fWW3 = fWW * 3.0f;
    
    // Compute position from cubic control points and barycentric coords
    float3 f3Position = I[0].f3Position * fWW * fW +
                        I[1].f3Position * fUU * fU +
                        I[2].f3Position * fVV * fV +
                        HSConstantData.f3B210 * fWW3 * fU +
                        HSConstantData.f3B120 * fW * fUU3 +
                        HSConstantData.f3B201 * fWW3 * fV +
                        HSConstantData.f3B021 * fUU3 * fV +
                        HSConstantData.f3B102 * fW * fVV3 +
                        HSConstantData.f3B012 * fU * fVV3 +
                        HSConstantData.f3B111 * 6.0f * fW * fU * fV;
    
    // Compute normal from quadratic control points and barycentric coords
    float3 f3Normal = I[0].f3Normal * fWW +
                        I[1].f3Normal * fUU +
                        I[2].f3Normal * fVV +
                        HSConstantData.f3N110 * fW * fU +
                        HSConstantData.f3N011 * fU * fV +
                        HSConstantData.f3N101 * fW * fV;

    // Normalize the interpolated normal    
    f3Normal = normalize(f3Normal);

    // Linearly interpolate the texture coords
    O.f2TexCoord = I[0].f2TexCoord * fW + I[1].f2TexCoord * fU + I[2].f2TexCoord * fV;

    // Calc diffuse color    
    O.f4Diffuse = g_f4MaterialDiffuseColor * g_f4LightDiffuse * max(0, dot(f3Normal, g_f4LightDir.xyz)) + g_f4MaterialAmbientColor;
    O.f4Diffuse.a = 1.0f;

    // Transform model position with view-projection matrix
    O.f4Position = mul(float4(f3Position.xyz, 1.0), g_f4x4ViewProjection);
        
    return O;
}
