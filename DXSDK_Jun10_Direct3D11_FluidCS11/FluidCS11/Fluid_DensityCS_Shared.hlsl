#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Shared Memory Optimized N^2 Algorithm
//--------------------------------------------------------------------------------------

groupshared float2 density_shared_pos[SIMULATION_BLOCK_SIZE];

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x;
    const float h_sq = g_fSmoothlen * g_fSmoothlen;
    float2 P_position = ParticlesRO[P_ID].position;
    
    float density = 0;
    
    // Calculate the density based on all neighbors
    [loop]
    for (uint N_block_ID = 0; N_block_ID < g_iNumParticles; N_block_ID += SIMULATION_BLOCK_SIZE)
    {
        // Cache a tile of particles unto shared memory to increase IO efficiency
        density_shared_pos[GI] = ParticlesRO[N_block_ID + GI].position;
       
        GroupMemoryBarrierWithGroupSync();

        for (uint N_tile_ID = 0; N_tile_ID < SIMULATION_BLOCK_SIZE; N_tile_ID++)
        {
            float2 N_position = density_shared_pos[N_tile_ID];
            
            float2 diff = N_position - P_position;
            float r_sq = dot(diff, diff);
            if (r_sq < h_sq)
            {
                density += CalculateDensity(r_sq);
            }
        }
        
        GroupMemoryBarrierWithGroupSync();
    }
    
    ParticlesDensityRW[P_ID].density = density;
}
