#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Integration
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x; // Particle ID to operate on
    
    float2 position = ParticlesRO[P_ID].position;
    float2 velocity = ParticlesRO[P_ID].velocity;
    float2 acceleration = ParticlesForcesRO[P_ID].acceleration;
    
    // Apply the forces from the map walls
    [unroll]
    for (unsigned int i = 0; i < 4; i++)
    {
        float dist = dot(float3(position, 1), g_vPlanes[i].xyz);
        acceleration += min(dist, 0) * -g_fWallStiffness * g_vPlanes[i].xy;
    }
    
    // Apply gravity
    acceleration += g_vGravity.xy;
    
    // Integrate
    velocity += g_fTimeStep * acceleration;
    position += g_fTimeStep * velocity;
    
    // Update
    ParticlesRW[P_ID].position = position;
    ParticlesRW[P_ID].velocity = velocity;
}
