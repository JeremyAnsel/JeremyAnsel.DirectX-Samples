// A simple inclusive prefix sum(scan) implemented in CS4.0, 
// using a typical up sweep and down sweep scheme

#include "Scan.hlsl"

StructuredBuffer<uint2> Input : register(t0);     // Change uint2 here if scan other types, and
StructuredBuffer<uint2> Input1 : register(t1);
RWStructuredBuffer<uint2> Result : register(u0);  // also here

// add the bucket scanned result to each bucket to get the final result
[numthreads(groupthreads, 1, 1)]
void main(uint3 Gid : SV_GroupID, uint3 DTid : SV_DispatchThreadID, uint3 GTid : SV_GroupThreadID, uint GI : SV_GroupIndex)
{
	Result[DTid.x] = Input[DTid.x] + Input1[Gid.x];
}
