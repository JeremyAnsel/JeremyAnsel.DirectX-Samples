#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Skinning vertex shader Section
//--------------------------------------------------------------------------------------
struct VS_CONTROL_POINT_INPUT
{
    float3 vPosition : POSITION;
    float2 vUV : TEXCOORD0;
    float3 vTangent : TANGENT;
    uint4 vBones : BONES;
    float4 vWeights : WEIGHTS;
};

VS_CONTROL_POINT_OUTPUT main(VS_CONTROL_POINT_INPUT Input)
{
    VS_CONTROL_POINT_OUTPUT Output;
    
    float4 vInputPos = float4(Input.vPosition, 1);
    float4 vWorldPos = float4(0, 0, 0, 0);
    
    vWorldPos += mul(vInputPos, g_mConstBoneWorld[Input.vBones.x]) * Input.vWeights.x;
    vWorldPos += mul(vInputPos, g_mConstBoneWorld[Input.vBones.y]) * Input.vWeights.y;
    vWorldPos += mul(vInputPos, g_mConstBoneWorld[Input.vBones.z]) * Input.vWeights.z;
    vWorldPos += mul(vInputPos, g_mConstBoneWorld[Input.vBones.w]) * Input.vWeights.w;
    
    float3 vWorldTan = float3(0, 0, 0);
    vWorldTan += mul(Input.vTangent, (float3x3) g_mConstBoneWorld[Input.vBones.x]) * Input.vWeights.x;
    vWorldTan += mul(Input.vTangent, (float3x3) g_mConstBoneWorld[Input.vBones.y]) * Input.vWeights.y;
    vWorldTan += mul(Input.vTangent, (float3x3) g_mConstBoneWorld[Input.vBones.z]) * Input.vWeights.z;
    vWorldTan += mul(Input.vTangent, (float3x3) g_mConstBoneWorld[Input.vBones.w]) * Input.vWeights.w;
    
    Output.vPosition = vWorldPos.xyz;
    Output.vUV = Input.vUV;
    Output.vTangent = vWorldTan;
    
    return Output;
}
