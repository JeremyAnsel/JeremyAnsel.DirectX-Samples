#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Shared Memory Optimized N^2 Algorithm
//--------------------------------------------------------------------------------------

groupshared
struct
{
    float2 position;
    float2 velocity;
    float density;
} force_shared_pos[SIMULATION_BLOCK_SIZE];

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
    [loop]
    for (uint N_block_ID = 0; N_block_ID < g_iNumParticles; N_block_ID += SIMULATION_BLOCK_SIZE)
    {
        // Cache a tile of particles unto shared memory to increase IO efficiency
        force_shared_pos[GI].position = ParticlesRO[N_block_ID + GI].position;
        force_shared_pos[GI].velocity = ParticlesRO[N_block_ID + GI].velocity;
        force_shared_pos[GI].density = ParticlesDensityRO[N_block_ID + GI].density;
       
        GroupMemoryBarrierWithGroupSync();

        [loop]
        for (uint N_tile_ID = 0; N_tile_ID < SIMULATION_BLOCK_SIZE; N_tile_ID++)
        {
            uint N_ID = N_block_ID + N_tile_ID;
            float2 N_position = force_shared_pos[N_tile_ID].position;
            
            float2 diff = N_position - P_position;
            float r_sq = dot(diff, diff);
            if (r_sq < h_sq && P_ID != N_ID)
            {
                float2 N_velocity = force_shared_pos[N_tile_ID].velocity;
                float N_density = force_shared_pos[N_tile_ID].density;
                float N_pressure = CalculatePressure(N_density);
                float r = sqrt(r_sq);

                // Pressure Term
                acceleration += CalculateGradPressure(r, P_pressure, N_pressure, N_density, diff);
                
                // Viscosity Term
                acceleration += CalculateLapVelocity(r, P_velocity, N_velocity, N_density);
            }
        }
        
        GroupMemoryBarrierWithGroupSync();
    }
    
    ParticlesForcesRW[P_ID].acceleration = acceleration / P_density;
}
