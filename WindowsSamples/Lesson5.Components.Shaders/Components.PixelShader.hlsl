
Texture2D simpleTexture : register(t0);
SamplerState simpleSampler : register(s0);

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float3 norm : NORMAL;
	float2 tex : TEXCOORD0;
};

float4 main(PixelShaderInput input) : SV_TARGET
{
	float3 lightDirection = normalize(float3(1, -1, 0));
	float4 color =  simpleTexture.Sample(simpleSampler, input.tex) * (0.8f * saturate(dot(input.norm, -lightDirection)) + 0.2f);
	return float4(color.xyz, 1.0f);
}
