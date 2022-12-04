
struct VSSceneIn
{
    float3 pos : POSITION;
    float3 norm : NORMAL;
    float2 tex : TEXCOORD0;
};

struct GSShadowIn
{
    float3 pos : POS;
    float3 norm : TEXCOORD0;
};

struct PSShadowIn
{
    float4 pos : SV_Position;
};

struct PSSceneIn
{
    float4 pos : SV_Position;
    float4 color : COLOR0;
    float2 tex : TEXCOORD0;
};

cbuffer cb1
{
    matrix g_mWorldViewProj;
    matrix g_mViewProj;
    matrix g_mWorld;
    float4 g_vLightPos;
    float4 g_vLightColor;
    float4 g_vAmbient;
    float4 g_vShadowColor;
    float g_fExtrudeAmt;
    float g_fExtrudeBias;
    float2 padding;
};
