
cbuffer cbPerObject : register(b0)
{
    row_major matrix g_mWorldViewProjection : packoffset(c0);
}

struct SkyboxVS_Input
{
    float4 Pos : POSITION;
};

struct SkyboxVS_Output
{
    float4 Pos : SV_POSITION;
    float3 Tex : TEXCOORD0;
};

SkyboxVS_Output main(SkyboxVS_Input Input)
{
    SkyboxVS_Output Output;
    
    Output.Pos = Input.Pos;
    Output.Tex = normalize(mul(Input.Pos, g_mWorldViewProjection)).xyz;
    
    return Output;
}
