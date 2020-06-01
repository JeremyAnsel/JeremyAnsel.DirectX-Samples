
cbuffer cbPerFrame : register(b0)
{
    matrix g_mViewProjection;
    float3 g_vCameraPosWorld;
    float  g_fTessellationFactor;
};

struct DS_OUTPUT
{
    float4 vPosition        : SV_POSITION;
    float3 vWorldPos        : WORLDPOS;
    float3 vNormal          : NORMAL;
};

//--------------------------------------------------------------------------------------
// Smooth shading pixel shader section
//--------------------------------------------------------------------------------------

// The pixel shader works the same as it would in a normal graphics pipeline.
// In this sample, it performs very simple N dot L lighting.

float4 main(DS_OUTPUT Input) : SV_TARGET
{
    float3 N = normalize(Input.vNormal);
    float3 L = normalize(Input.vWorldPos - g_vCameraPosWorld);
    return float4(abs(dot(N, L)) * float3(1, 0, 0), 1);
}
