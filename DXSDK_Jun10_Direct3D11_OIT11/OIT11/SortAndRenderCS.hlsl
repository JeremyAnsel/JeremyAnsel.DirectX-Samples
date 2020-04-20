
// TODO: use structured buffers
RWBuffer<float> deepBufferDepth : register(u0);
RWStructuredBuffer<float4> deepBufferColor : register(u1);
RWTexture2D<float4> frameBuffer : register(u2);
RWBuffer<uint> prefixSum : register(u3);

Texture2D<uint> fragmentCount : register (t0);

cbuffer CB : register(b0)
{
	uint g_nFrameWidth : packoffset(c0.x);
	uint g_nFrameHeight : packoffset(c0.y);
	uint g_nPassSize : packoffset(c0.z);
	uint g_nReserved : packoffset(c0.w);
}

#define blocksize 1
#define groupthreads (blocksize*blocksize)
groupshared float accum[groupthreads];

// Sort the fragments using a bitonic sort, then accumulate the fragments into the final result.
groupshared int nIndex[32];
#define NUM_THREADS 8

[numthreads(1, 1, 1)]
void main(uint3 nGid : SV_GroupID, uint3 nDTid : SV_DispatchThreadID, uint3 nGTid : SV_GroupThreadID)
{
	uint nThreadNum = nGid.y * g_nFrameWidth + nGid.x;

	//    uint r0, r1, r2;
	//    float rd0, rd1, rd2, rd3, rd4, rd5, rd6, rd7;

	uint N = fragmentCount[nDTid.xy];

	uint N2 = 1 << (int)(ceil(log2(N)));

	float fDepth[32];

	uint i;

	for (i = 0; i < N; i++)
	{
		nIndex[i] = i;
		fDepth[i] = deepBufferDepth[prefixSum[nThreadNum - 1] + i];
	}

	for (i = N; i < N2; i++)
	{
		nIndex[i] = i;
		fDepth[i] = 1.1f;
	}

	uint idx = blocksize * nGTid.y + nGTid.x;

	// Bitonic sort
	for (uint k = 2; k <= N2; k = 2 * k)
	{
		for (uint j = k >> 1; j > 0; j = j >> 1)
		{
			for (uint i = 0; i < N2; i++)
			{
				//GroupMemoryBarrierWithGroupSync();
				//i = idx;

				float di = fDepth[nIndex[i]];
				uint ixj = i ^ j;
				if ((ixj) > i)
				{
					float dixj = fDepth[nIndex[ixj]];
					if ((i & k) == 0 && di > dixj)
					{
						int temp = nIndex[i];
						nIndex[i] = nIndex[ixj];
						nIndex[ixj] = temp;
					}
					if ((i & k) != 0 && di < dixj)
					{
						int temp = nIndex[i];
						nIndex[i] = nIndex[ixj];
						nIndex[ixj] = temp;
					}
				}
			}
		}
	}

	// Output the final result to the frame buffer
	if (idx == 0)
	{
		/*
		// Debug
		uint color[8];
		for(int i = 0; i < 8; i++)
		{
			color[i] = deepBufferColorUINT[prefixSum[nThreadNum-1] + i];
		}

		for(int i = 0; i < 8; i++)
		{
			deepBufferDepth[nThreadNum * 8 + i] = fDepth[i]; //fDepth[nIndex[i]];
			deepBufferColorUINT[nThreadNum * 8 + i] = color[nIndex[i]];
		}
		*/

		// Accumulate fragments into final result
		float4 result = 0.0f;
		for (int x = N - 1; x >= 0; x--)
		{
			float4 color = deepBufferColor[prefixSum[nThreadNum - 1] + nIndex[x]];
			result = lerp(result, color, color.a);
		}
		result.a = 1.0f;
		frameBuffer[nGid.xy] = result;
	}
}
