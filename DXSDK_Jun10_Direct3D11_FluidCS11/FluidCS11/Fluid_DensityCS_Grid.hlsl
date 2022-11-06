#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Optimized Grid + Sort Algorithm
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x;
    const float h_sq = g_fSmoothlen * g_fSmoothlen;
    float2 P_position = ParticlesRO[P_ID].position;
    
    float density = 0;
    
    // Calculate the density based on neighbors from the 8 adjacent cells + current cell
    int2 G_XY = (int2) GridCalculateCell(P_position);
    for (int Y = max(G_XY.y - 1, 0); Y <= min(G_XY.y + 1, 255); Y++)
    {
        for (int X = max(G_XY.x - 1, 0); X <= min(G_XY.x + 1, 255); X++)
        {
            unsigned int G_CELL = GridConstuctKey(uint2(X, Y));
            uint2 G_START_END = GridIndicesRO[G_CELL];
            for (unsigned int N_ID = G_START_END.x; N_ID < G_START_END.y; N_ID++)
            {
                float2 N_position = ParticlesRO[N_ID].position;
                
                float2 diff = N_position - P_position;
                float r_sq = dot(diff, diff);
                if (r_sq < h_sq)
                {
                    density += CalculateDensity(r_sq);
                }
            }
        }
    }
    
    ParticlesDensityRW[P_ID].density = density;
}
