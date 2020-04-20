
struct SceneVS_Output
{
	float4 pos : SV_POSITION;
	float4 color : COLOR0;
};

float4 main(SceneVS_Output input) : SV_TARGET
{
	return float4(input.color.xyz, 1.0f);
}
