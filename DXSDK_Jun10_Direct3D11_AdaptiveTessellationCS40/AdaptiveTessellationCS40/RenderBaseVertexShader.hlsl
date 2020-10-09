// The shaders for rendering tessellated mesh and base mesh

cbuffer cbPerObject : register(b0)
{
	row_major matrix    g_mWorldViewProjection    : packoffset(c0);
}

struct BaseVertex
{
	float4 pos : POSITION;
};

float4 main(BaseVertex input) : SV_POSITION
{
	return mul(input.pos, g_mWorldViewProjection);
}
