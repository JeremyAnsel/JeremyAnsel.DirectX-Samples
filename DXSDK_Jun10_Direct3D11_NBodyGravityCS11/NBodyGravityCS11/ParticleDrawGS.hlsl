//--------------------------------------------------------------------------------------
// File: ParticleDraw.hlsl
//
// Shaders for rendering the particle as point sprite
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

struct VSParticleDrawOut
{
	float3 pos			: POSITION;
	float4 color		: COLOR;
};

struct GSParticleDrawOut
{
	float2 tex			: TEXCOORD0;
	float4 color		: COLOR;
	float4 pos			: SV_POSITION;
};

cbuffer cb0
{
	row_major float4x4 g_mWorldViewProj;
	row_major float4x4 g_mInvView;
};

cbuffer cb1
{
	static float g_fParticleRad = 10.0f;
};

cbuffer cbImmutable
{
	static float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3(1, 1, 0),
		float3(-1, -1, 0),
		float3(1, -1, 0),
	};

	static float2 g_texcoords[4] =
	{
		float2(0,0),
		float2(1,0),
		float2(0,1),
		float2(1,1),
	};
};

//
// GS for rendering point sprite particles.  Takes a point and turns it into 2 tris.
//
[maxvertexcount(4)]
void main(point VSParticleDrawOut input[1], inout TriangleStream<GSParticleDrawOut> SpriteStream)
{
	GSParticleDrawOut output;

	//
	// Emit two new triangles
	//
	for (int i = 0; i < 4; i++)
	{
		float3 position = g_positions[i] * g_fParticleRad;
		position = mul(position, (float3x3)g_mInvView) + input[0].pos;
		output.pos = mul(float4(position, 1.0), g_mWorldViewProj);

		output.color = input[0].color;
		output.tex = g_texcoords[i];
		SpriteStream.Append(output);
	}

	SpriteStream.RestartStrip();
}
