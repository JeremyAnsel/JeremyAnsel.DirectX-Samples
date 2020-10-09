// A simple inclusive prefix sum(scan) implemented in CS4.0, 
// using a typical up sweep and down sweep scheme

#include "Scan.hlsl"

StructuredBuffer<uint2> Input : register(t0);     // Change uint2 here if scan other types, and
RWStructuredBuffer<uint2> Result : register(u0);  // also here

// scan in each bucket
[numthreads(groupthreads, 1, 1)]
void main(uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
	uint2 x = Input[DTid.x];                    // Change the type of x here if scan other types 
	Result[DTid.x] = CSScan(DTid, GI, x);
}
