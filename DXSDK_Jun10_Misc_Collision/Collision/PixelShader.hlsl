
cbuffer ConstantBuffer : register(b0)
{
	matrix WorldViewProjection;
	float4 Color;
}

float4 main() : SV_TARGET
{
	return Color;
}
