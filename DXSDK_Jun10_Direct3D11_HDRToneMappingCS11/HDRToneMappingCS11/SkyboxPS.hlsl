
TextureCube g_EnvironmentTexture : register(t0);
SamplerState g_sam : register(s0);

struct SkyboxVS_Output
{
    float4 Pos : SV_POSITION;
    float3 Tex : TEXCOORD0;
};

float4 main(SkyboxVS_Output Input) : SV_TARGET
{
    float4 color = g_EnvironmentTexture.Sample(g_sam, Input.Tex);
    return color;
}
