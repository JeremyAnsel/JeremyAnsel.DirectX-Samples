#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Rearrange Particles
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int ID = DTid.x; // Particle ID to operate on
    const unsigned int G_ID = GridGetValue(GridRO[ID]);
    ParticlesRW[ID] = ParticlesRO[G_ID];
}
