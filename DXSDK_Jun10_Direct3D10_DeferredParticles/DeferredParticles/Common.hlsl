
#define MAX_GLOWLIGHTS 8
#define MAX_INSTANCES 200

cbuffer cbGlowLights : register(b0)
{
    uint g_NumGlowLights;
    uint3 cbGlowLights_unused0;

    float4 g_vGlowLightPosIntensity[MAX_GLOWLIGHTS];
    float4 g_vGlowLightColor[MAX_GLOWLIGHTS];
	
    float4 g_vGlowLightAttenuation;
    float4 g_vMeshLightAttenuation;
};

cbuffer cbPerFrame : register(b1)
{
    float g_fTime;
    float3 cbPerFrame_unused0;
    float4 g_LightDir;
    float4 g_vEyePt;
    float4 g_vRight;
    float4 g_vUp;
    float4 g_vForward;
    float4x4 g_mWorldViewProjection;
    float4x4 g_mViewProj;
    float4x4 g_mInvViewProj;
    float4x4 g_mWorld;
};

cbuffer cbInstancedGlobals : register(b2)
{
    float4x4 g_mWorldInst[MAX_INSTANCES];
};

struct VS_PARTICLEINPUT
{
    float4 Position : POSITION;
    float2 TextureUV : TEXCOORD0;
    float fLife : LIFE;
    float fRot : THETA;
    float4 Color : COLOR0;
};

struct VS_MESHINPUT
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD0;
};

struct VS_MESHOUTPUT
{
    float4 Position : SV_POSITION;
    float3 wPos : WORLDPOS;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD0;
};

struct VS_PARTICLEOUTPUT
{
    float4 Position : SV_POSITION; // vertex position 
    float3 TextureUVI : TEXCOORD0; // vertex texture coords
    float3 SinCosThetaLife : TEXCOORD1;
    float4 Color : COLOR0;
};

struct VS_SCREENOUTPUT
{
    float4 Position : SV_POSITION; // vertex position  
};

struct PBUFFER_OUTPUT
{
    float4 color0 : SV_TARGET0;
    float4 color1 : SV_TARGET1;
};

Texture2D g_txMeshTexture : register(t0); // Color texture for mesh
Texture2D g_txParticleColor : register(t1); // Particle color buffer

SamplerState g_samLinear : register(s0);
