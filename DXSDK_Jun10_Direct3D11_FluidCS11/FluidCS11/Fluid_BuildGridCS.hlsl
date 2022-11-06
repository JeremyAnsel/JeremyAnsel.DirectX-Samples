#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Build Grid
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int P_ID = DTid.x; // Particle ID to operate on
    
    float2 position = ParticlesRO[P_ID].position;
    float2 grid_xy = GridCalculateCell(position);
    
    GridRW[P_ID] = GridConstuctKeyValuePair((uint2) grid_xy, P_ID);
}
