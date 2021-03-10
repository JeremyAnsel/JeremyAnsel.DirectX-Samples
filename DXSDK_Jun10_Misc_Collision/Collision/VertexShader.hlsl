
cbuffer ConstantBuffer : register(b0)
{
	matrix WorldViewProjection;
	float4 Color;
}

float4 main(float4 pos : POSITION) : SV_POSITION
{
	return mul(float4(pos.xyz, 1) , WorldViewProjection);
}
