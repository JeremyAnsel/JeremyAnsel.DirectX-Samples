
//TODO: Use structured buffers
RWTexture2D<uint> fragmentCount : register(u1);

struct SceneVS_Output
{
	float4 pos : SV_POSITION;
	float4 color : COLOR0;
};

void main(SceneVS_Output input)
{
	// Increments need to be done atomically
	InterlockedAdd(fragmentCount[input.pos.xy], 1);
}
