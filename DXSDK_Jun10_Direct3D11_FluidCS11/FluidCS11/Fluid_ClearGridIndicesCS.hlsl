#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Build Grid Indices
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    GridIndicesRW[DTid.x] = uint2(0, 0);
}
