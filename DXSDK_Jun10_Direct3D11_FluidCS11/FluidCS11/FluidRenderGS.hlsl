
cbuffer cbRenderConstants : register(b0)
{
    matrix g_mViewProjection;
    float g_fParticleSize;
    float3 padding;
};

struct VSParticleOut
{
    float2 position : POSITION;
    float4 color : COLOR;
};

struct GSParticleOut
{
    float4 position : SV_Position;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD;
};

static const float2 g_positions[4] = { float2(-1, 1), float2(1, 1), float2(-1, -1), float2(1, -1) };
static const float2 g_texcoords[4] = { float2(0, 1), float2(1, 1), float2(0, 0), float2(1, 0) };

[maxvertexcount(4)]
void main(point VSParticleOut In[1], inout TriangleStream<GSParticleOut> SpriteStream)
{
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        GSParticleOut Out = (GSParticleOut) 0;
        float4 position = float4(In[0].position, 0, 1) + g_fParticleSize * float4(g_positions[i], 0, 0);
        Out.position = mul(position, g_mViewProjection);
        Out.color = In[0].color;
        Out.texcoord = g_texcoords[i];
        SpriteStream.Append(Out);
    }

    SpriteStream.RestartStrip();
}
