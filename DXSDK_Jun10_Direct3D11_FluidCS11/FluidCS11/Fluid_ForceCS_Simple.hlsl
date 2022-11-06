#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Simple N^2 Algorithm
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x; // Particle ID to operate on
    
    float2 P_position = ParticlesRO[P_ID].position;
    float2 P_velocity = ParticlesRO[P_ID].velocity;
    float P_density = ParticlesDensityRO[P_ID].density;
    float P_pressure = CalculatePressure(P_density);
    
    const float h_sq = g_fSmoothlen * g_fSmoothlen;
    
    float2 acceleration = float2(0, 0);

    // Calculate the acceleration based on all neighbors
    for (uint N_ID = 0; N_ID < g_iNumParticles; N_ID++)
    {
        float2 N_position = ParticlesRO[N_ID].position;
        
        float2 diff = N_position - P_position;
        float r_sq = dot(diff, diff);
        if (r_sq < h_sq && P_ID != N_ID)
        {
            float2 N_velocity = ParticlesRO[N_ID].velocity;
            float N_density = ParticlesDensityRO[N_ID].density;
            float N_pressure = CalculatePressure(N_density);
            float r = sqrt(r_sq);

            // Pressure Term
            acceleration += CalculateGradPressure(r, P_pressure, N_pressure, N_density, diff);
            
            // Viscosity Term
            acceleration += CalculateLapVelocity(r, P_velocity, N_velocity, N_density);
        }
    }
    
    ParticlesForcesRW[P_ID].acceleration = acceleration / P_density;
}
