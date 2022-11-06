#include "Fluid_common.hlsl"

//--------------------------------------------------------------------------------------
// Build Grid Indices
//--------------------------------------------------------------------------------------

[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
    const unsigned int G_ID = DTid.x; // Grid ID to operate on
    unsigned int G_ID_PREV = (G_ID == 0) ? g_iNumParticles : G_ID;
    G_ID_PREV--;
    unsigned int G_ID_NEXT = G_ID + 1;
    if (G_ID_NEXT == g_iNumParticles)
    {
        G_ID_NEXT = 0;
    }
    
    unsigned int cell = GridGetKey(GridRO[G_ID]);
    unsigned int cell_prev = GridGetKey(GridRO[G_ID_PREV]);
    unsigned int cell_next = GridGetKey(GridRO[G_ID_NEXT]);
    if (cell != cell_prev)
    {
        // I'm the start of a cell
        GridIndicesRW[cell].x = G_ID;
    }
    if (cell != cell_next)
    {
        // I'm the end of a cell
        GridIndicesRW[cell].y = G_ID + 1;
    }
}
