
struct VSSceneIn
{
    float3 pos : POSITION; //position of the particle
    float3 norm : NORMAL; //velocity of the particle
    float2 tex : TEXTURE0; //tex coords
};

struct VSSceneOut
{
    float4 pos : SV_Position; //position
    float2 tex : TEXTURE0; //texture coordinate
    float3 wPos : TEXTURE1; //world space pos
    float3 wNorm : TEXTURE2; //world space normal
    float4 colorD : COLOR0; //color for gouraud and flat shading
    float4 colorS : COLOR1; //color for specular
    float fogDist : FOGDISTANCE; //distance used for fog calculations
    float3 planeDist : SV_ClipDistance0; //clip distance for 3 planes
};

struct PSSceneIn
{
    float4 pos : SV_Position; //position
    float2 tex : TEXTURE0; //texture coordinate
    float3 wPos : TEXTURE1; //world space pos
    float3 wNorm : TEXTURE2; //world space normal
    float4 colorD : COLOR0; //color for gouraud and flat shading
    float4 colorS : COLOR1; //color for specular
    float fogDist : FOGDISTANCE; //distance used for fog calculations
};

struct ColorsOutput
{
    float4 Diffuse;
    float4 Specular;
};

struct Light
{
    float4 Position;
    float4 Diffuse;
    float4 Specular;
    float4 Ambient;
    float4 Atten;
};

#define FOGMODE_NONE    0
#define FOGMODE_LINEAR  1
#define FOGMODE_EXP     2
#define FOGMODE_EXP2    3

#define E 2.71828

cbuffer cb1 : register(b0)
{
    // cbPerViewChange
    //viewport params
    float g_viewportHeight;
    float g_viewportWidth;
    float g_nearPlane;
    float cb1_padding1;
}

cbuffer cb2 : register(b1)
{
    // cbLights
    float4 g_clipplanes[3];
    Light g_lights[8];
    
    // cbPerFrame
    float4x4 g_mWorld;
    float4x4 g_mView;
    float4x4 g_mProj;
    float4x4 g_mInvProj;
    float4x4 g_mLightViewProj;

    // cbPerTechnique
    bool g_bEnableLighting;
    bool g_bEnableClipping;
    bool g_bPointScaleEnable;
    bool cb2_padding1;
    float g_pointScaleA;
    float g_pointScaleB;
    float g_pointScaleC;
    float g_pointSize;
    //fog params
    int g_fogMode;
    float g_fogStart;
    float g_fogEnd;
    float g_fogDensity;
    float4 g_fogColor;
}

ColorsOutput CalcLighting(float3 worldNormal, float3 worldPos, float3 cameraPos)
{
    ColorsOutput output = (ColorsOutput) 0;
    
    for (int i = 0; i < 8; i++)
    {
        float3 toLight = g_lights[i].Position.xyz - worldPos;
        float lightDist = length(toLight);
        float fAtten = 1.0 / dot(g_lights[i].Atten, float4(1, lightDist, lightDist * lightDist, 0));
        float3 lightDir = normalize(toLight);
        float3 halfAngle = normalize(normalize(-cameraPos) + lightDir);
        
        output.Diffuse += max(0, dot(lightDir, worldNormal) * g_lights[i].Diffuse * fAtten) + g_lights[i].Ambient;
        output.Specular += max(0, pow(abs(dot(halfAngle, worldNormal)), 64) * g_lights[i].Specular * fAtten);
    }
    
    return output;
}

//
// Calculates fog factor based upon distance
//
float CalcFogFactor(float d)
{
    float fogCoeff = 1.0;
    
    if (FOGMODE_LINEAR == g_fogMode)
    {
        fogCoeff = (g_fogEnd - d) / (g_fogEnd - g_fogStart);
    }
    else if (FOGMODE_EXP == g_fogMode)
    {
        fogCoeff = 1.0 / pow(E, d * g_fogDensity);
    }
    else if (FOGMODE_EXP2 == g_fogMode)
    {
        fogCoeff = 1.0 / pow(E, d * d * g_fogDensity * g_fogDensity);
    }
    
    return clamp(fogCoeff, 0, 1);
}
