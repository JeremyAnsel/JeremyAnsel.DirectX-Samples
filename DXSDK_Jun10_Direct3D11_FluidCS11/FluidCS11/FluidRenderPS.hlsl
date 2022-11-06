
struct GSParticleOut
{
    float4 position : SV_Position;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD;
};

float4 main(GSParticleOut In) : SV_Target
{
    return In.color;
}
