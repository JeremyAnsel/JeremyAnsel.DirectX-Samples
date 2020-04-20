
Texture2D txDiffuse : register(t0);
SamplerState samLinear : register(s0);

cbuffer cbChangesEveryFrame : register(b2)
{
	matrix World;
	float4 vMeshColor;
};

struct PS_INPUT
{
	float4 Pos : SV_POSITION;
	float2 Tex : TEXCOORD0;
};

float4 main(PS_INPUT input) : SV_TARGET
{
	float4 color = txDiffuse.Sample(samLinear, input.Tex) * vMeshColor;

	return float4(color.xyz, 1.0f);
}
