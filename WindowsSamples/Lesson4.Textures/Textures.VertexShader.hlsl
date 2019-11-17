
cbuffer simpleConstantBuffer : register(b0)
{
	matrix model;
	matrix view;
	matrix projection;
};

struct VertexShaderInput
{
	float3 pos : POSITION;
	float3 norm : NORMAL;
	float2 tex : TEXCOORD0;
};

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float3 norm : NORMAL;
	float2 tex : TEXCOORD0;
};

PixelShaderInput main(VertexShaderInput input)
{
	PixelShaderInput vertexShaderOutput;
	float4 pos = float4(input.pos, 1.0f);

		// Transform the vertex position into projection space.
		pos = mul(pos, model);
	pos = mul(pos, view);
	pos = mul(pos, projection);
	vertexShaderOutput.pos = pos;

	// Pass through the texture coordinate without modification.
	vertexShaderOutput.tex = input.tex;

	// Transform the normal into world space to allow world-space lighting.
	float4 norm = float4(normalize(input.norm), 0.0f);
		norm = mul(norm, model);
	vertexShaderOutput.norm = normalize(norm.xyz);
	return vertexShaderOutput;
}
