//--------------------------------------------------------------------------------------
// File: ParticleDraw.hlsl
//
// Shaders for rendering the particle as point sprite
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

struct VSParticleIn
{
    float4  color   : COLOR;
    uint    id      : SV_VERTEXID;
};

struct VSParticleDrawOut
{
    float3 pos			: POSITION;
    float4 color		: COLOR;
};

struct PosVelo
{
    float4 pos;
    float4 velo;
};

StructuredBuffer<PosVelo>   g_bufPosVelo;

//
// Vertex shader for drawing the point-sprite particles
//
VSParticleDrawOut main(VSParticleIn input)
{
    VSParticleDrawOut output;

    output.pos = g_bufPosVelo[input.id].pos.xyz;

    float mag = g_bufPosVelo[input.id].velo.w / 9;
    output.color = lerp(float4(1, 0.1, 0.1, 1), input.color, mag);

    return output;
}
