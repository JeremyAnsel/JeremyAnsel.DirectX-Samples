
/*
cbuffer cbblurVS : register(b2)
{
	int2		g_iWidthHeight			: packoffset(c0);
	int		    g_iKernelStart : packoffset(c0.z);
	int		    g_iKernelEnd : packoffset(c0.w);
};
*/

//--------------------------------------------------------------------------------------
// Input/Output structures
//--------------------------------------------------------------------------------------

struct PSIn
{
	float4      Pos	    : SV_Position;		//Position
	float2      Tex	    : TEXCOORD;		    //Texture coordinate
	//float2      ITex    : TEXCOORD2;
};

struct VSIn
{
	uint Pos	: SV_VertexID;
};

PSIn main(VSIn inn)
{
	PSIn output;

	output.Pos.y = -1.0f + (inn.Pos % 2) * 2.0f;
	output.Pos.x = -1.0f + (inn.Pos / 2) * 2.0f;
	output.Pos.z = .5;
	output.Pos.w = 1;
	output.Tex.x = inn.Pos / 2;
	output.Tex.y = 1.0f - inn.Pos % 2;

	//output.ITex.x = (float)(g_iWidthHeight.x * output.Tex.x);
	//output.ITex.y = (float)(g_iWidthHeight.y * output.Tex.y);

	return output;
}
