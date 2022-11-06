//--------------------------------------------------------------------------------------
// File: FluidCS11.hlsl
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

//--------------------------------------------------------------------------------------
// Smoothed Particle Hydrodynamics Algorithm Based Upon:
// Particle-Based Fluid Simulation for Interactive Applications
// Matthias Müller
//--------------------------------------------------------------------------------------

//--------------------------------------------------------------------------------------
// Optimized Grid Algorithm Based Upon:
// Broad-Phase Collision Detection with CUDA
// Scott Le Grand
//--------------------------------------------------------------------------------------

struct Particle
{
    float2 position;
    float2 velocity;
};

struct ParticleForces
{
    float2 acceleration;
};

struct ParticleDensity
{
    float density;
};

cbuffer cbSimulationConstants : register(b0)
{
    uint g_iNumParticles;
    uint3 padding;

    float g_fTimeStep;
    float g_fSmoothlen;
    float g_fPressureStiffness;
    float g_fRestDensity;
    float g_fDensityCoef;
    float g_fGradPressureCoef;
    float g_fLapViscosityCoef;
    float g_fWallStiffness;

    float2 g_vGravity;
    float2 padding2;
    float4 g_vGridDim;
    float4 g_vPlanes[4];
};

//--------------------------------------------------------------------------------------
// Fluid Simulation
//--------------------------------------------------------------------------------------

#define SIMULATION_BLOCK_SIZE 256

//--------------------------------------------------------------------------------------
// Structured Buffers
//--------------------------------------------------------------------------------------
RWStructuredBuffer<Particle> ParticlesRW : register(u0);
StructuredBuffer<Particle> ParticlesRO : register(t0);

RWStructuredBuffer<ParticleDensity> ParticlesDensityRW : register(u0);
StructuredBuffer<ParticleDensity> ParticlesDensityRO : register(t1);

RWStructuredBuffer<ParticleForces> ParticlesForcesRW : register(u0);
StructuredBuffer<ParticleForces> ParticlesForcesRO : register(t2);

RWStructuredBuffer<unsigned int> GridRW : register(u0);
StructuredBuffer<unsigned int> GridRO : register(t3);

RWStructuredBuffer<uint2> GridIndicesRW : register(u0);
StructuredBuffer<uint2> GridIndicesRO : register(t4);

//--------------------------------------------------------------------------------------
// Grid Construction
//--------------------------------------------------------------------------------------

// For simplicity, this sample uses a 16-bit hash based on the grid cell and
// a 16-bit particle ID to keep track of the particles while sorting
// This imposes a limitation of 64K particles and 256x256 grid work
// You could extended the implementation to support large scenarios by using a uint2

float2 GridCalculateCell(float2 position)
{
    return clamp(position * g_vGridDim.xy + g_vGridDim.zw, float2(0, 0), float2(255, 255));
}

unsigned int GridConstuctKey(uint2 xy)
{
    // Bit pack [-----UNUSED-----][----Y---][----X---]
    //                16-bit         8-bit     8-bit
    return dot(xy.yx, uint2(256, 1));
}

unsigned int GridConstuctKeyValuePair(uint2 xy, uint value)
{
    // Bit pack [----Y---][----X---][-----VALUE------]
    //             8-bit     8-bit        16-bit
    return dot(uint3(xy.yx, value), uint3(256 * 256 * 256, 256 * 256, 1));
}

unsigned int GridGetKey(unsigned int keyvaluepair)
{
    return (keyvaluepair >> 16);
}

unsigned int GridGetValue(unsigned int keyvaluepair)
{
    return (keyvaluepair & 0xFFFF);
}

//--------------------------------------------------------------------------------------
// Density Calculation
//--------------------------------------------------------------------------------------

float CalculateDensity(float r_sq)
{
    const float h_sq = g_fSmoothlen * g_fSmoothlen;
    // Implements this equation:
    // W_poly6(r, h) = 315 / (64 * pi * h^9) * (h^2 - r^2)^3
    // g_fDensityCoef = fParticleMass * 315.0f / (64.0f * PI * fSmoothlen^9)
    return g_fDensityCoef * (h_sq - r_sq) * (h_sq - r_sq) * (h_sq - r_sq);
}

//--------------------------------------------------------------------------------------
// Force Calculation
//--------------------------------------------------------------------------------------

float CalculatePressure(float density)
{
    // Implements this equation:
    // Pressure = B * ((rho / rho_0)^y  - 1)
    return g_fPressureStiffness * max(pow(density / g_fRestDensity, 3) - 1, 0);
}

float2 CalculateGradPressure(float r, float P_pressure, float N_pressure, float N_density, float2 diff)
{
    const float h = g_fSmoothlen;
    float avg_pressure = 0.5f * (N_pressure + P_pressure);
    // Implements this equation:
    // W_spkiey(r, h) = 15 / (pi * h^6) * (h - r)^3
    // GRAD( W_spikey(r, h) ) = -45 / (pi * h^6) * (h - r)^2
    // g_fGradPressureCoef = fParticleMass * -45.0f / (PI * fSmoothlen^6)
    return g_fGradPressureCoef * avg_pressure / N_density * (h - r) * (h - r) / r * (diff);
}

float2 CalculateLapVelocity(float r, float2 P_velocity, float2 N_velocity, float N_density)
{
    const float h = g_fSmoothlen;
    float2 vel_diff = (N_velocity - P_velocity);
    // Implements this equation:
    // W_viscosity(r, h) = 15 / (2 * pi * h^3) * (-r^3 / (2 * h^3) + r^2 / h^2 + h / (2 * r) - 1)
    // LAPLACIAN( W_viscosity(r, h) ) = 45 / (pi * h^6) * (h - r)
    // g_fLapViscosityCoef = fParticleMass * fViscosity * 45.0f / (PI * fSmoothlen^6)
    return g_fLapViscosityCoef / N_density * (h - r) * vel_diff;
}
