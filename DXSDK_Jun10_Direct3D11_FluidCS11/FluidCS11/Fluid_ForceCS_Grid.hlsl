#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Optimized Grid + Sort Algorithm
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
    
    // Calculate the acceleration based on neighbors from the 8 adjacent cells + current cell
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
        }
    }

    ParticlesForcesRW[P_ID].acceleration = acceleration / P_density;
}
