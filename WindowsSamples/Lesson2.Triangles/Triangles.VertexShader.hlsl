
float4 main(float2 pos : POSITION) : SV_POSITION
{
	// For this lesson, set the vertex depth value to 0.5 so it is guaranteed to be drawn.
	return float4(pos, 0.5f, 1.0f);
}
