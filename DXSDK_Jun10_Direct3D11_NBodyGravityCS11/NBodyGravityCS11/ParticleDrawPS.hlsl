//--------------------------------------------------------------------------------------
// File: ParticleDraw.hlsl
//
// Shaders for rendering the particle as point sprite
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

struct PSParticleDrawIn
{
	float2 tex			: TEXCOORD0;
	float4 color		: COLOR;
};

Texture2D		            g_txDiffuse;

SamplerState g_samLinear
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Clamp;
	AddressV = Clamp;
};

//
// PS for drawing particles
//
float4 main(PSParticleDrawIn input) : SV_Target
{
	return g_txDiffuse.Sample(g_samLinear, input.tex) * input.color;
}
