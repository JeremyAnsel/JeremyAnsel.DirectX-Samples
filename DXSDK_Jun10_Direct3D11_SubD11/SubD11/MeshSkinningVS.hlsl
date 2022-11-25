#include "Common.hlsl"

struct VS_MESH_POINT_INPUT
{
    float3 vPosition : POSITION;
    float2 vUV : TEXCOORD0;
    float3 vNormal : NORMAL;
    float3 vTangent : TANGENT;
    uint4 vBones : BONES;
    float4 vWeights : WEIGHTS;
};

struct VS_MESH_POINT_OUTPUT
{
    float3 vWorldPos : POSITION;
    float3 vNormal : NORMAL;
    float2 vUV : TEXCOORD;
    float3 vTangent : TANGENT;
    float3 vBiTangent : BITANGENT;
    
    float4 vPosition : SV_POSITION;
};

VS_MESH_POINT_OUTPUT main(VS_MESH_POINT_INPUT Input)
{
    VS_MESH_POINT_OUTPUT Output;
    
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
    
    float3 vWorldNormal = float3(0, 0, 0);
    vWorldNormal += mul(Input.vNormal, (float3x3) g_mConstBoneWorld[Input.vBones.x]) * Input.vWeights.x;
    vWorldNormal += mul(Input.vNormal, (float3x3) g_mConstBoneWorld[Input.vBones.y]) * Input.vWeights.y;
    vWorldNormal += mul(Input.vNormal, (float3x3) g_mConstBoneWorld[Input.vBones.z]) * Input.vWeights.z;
    vWorldNormal += mul(Input.vNormal, (float3x3) g_mConstBoneWorld[Input.vBones.w]) * Input.vWeights.w;
    
    Output.vWorldPos = vWorldPos.xyz;
    Output.vPosition = mul(float4(vWorldPos.xyz, 1), g_mViewProjection);
    Output.vUV = Input.vUV;
    Output.vTangent = vWorldTan;
    Output.vNormal = vWorldNormal;
    Output.vBiTangent = cross(vWorldNormal, vWorldTan);
    
    return Output;
}
