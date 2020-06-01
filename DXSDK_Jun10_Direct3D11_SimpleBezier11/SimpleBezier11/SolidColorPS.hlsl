
struct DS_OUTPUT
{
    float4 vPosition        : SV_POSITION;
    float3 vWorldPos        : WORLDPOS;
    float3 vNormal          : NORMAL;
};

//--------------------------------------------------------------------------------------
// Solid color shading pixel shader (used for wireframe overlay)
//--------------------------------------------------------------------------------------
float4 main(DS_OUTPUT Input) : SV_TARGET
{
    // Return a solid green color
    return float4(0, 1, 0, 1);
}
