#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Simple N^2 Algorithm
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x;
    const float h_sq = g_fSmoothlen * g_fSmoothlen;
    float2 P_position = ParticlesRO[P_ID].position;
    
    float density = 0;
    
    // Calculate the density based on all neighbors
    for (uint N_ID = 0; N_ID < g_iNumParticles; N_ID++)
    {
        float2 N_position = ParticlesRO[N_ID].position;
        
        float2 diff = N_position - P_position;
        float r_sq = dot(diff, diff);
        if (r_sq < h_sq)
        {
            density += CalculateDensity(r_sq);
        }
    }
    
    ParticlesDensityRW[P_ID].density = density;
}
