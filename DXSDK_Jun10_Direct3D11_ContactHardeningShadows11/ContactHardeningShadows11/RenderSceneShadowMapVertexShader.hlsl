
cbuffer cbConstants : register(b0)
{
    float4x4 g_f4x4WorldViewProjection;        // World * View * Projection matrix
    float4x4 g_f4x4WorldViewProjLight;        // World * ViewLight * Projection Light matrix
    float4   g_vShadowMapDimensions;
    float4   g_f4LightDir;
    float    g_fSunWidth;
    float3   g_f3Padding;
}

struct VS_RenderSceneInput
{
    float3 f3Position    : POSITION;
    float3 f3Normal      : NORMAL;
    float2 f2TexCoord    : TEXTURE0;
};

struct PS_RenderSceneInput
{
    float4 f4Position   : SV_Position;
    float4 f4Diffuse    : COLOR0;
    float2 f2TexCoord   : TEXTURE0;
    float4 f4SMC        : TEXTURE1;
};

//======================================================================================
// This shader is a simplified vertex shader used for shadow map rendering
//======================================================================================
PS_RenderSceneInput main(VS_RenderSceneInput I)
{
    PS_RenderSceneInput O;

    // Transform the position from object space to homogeneous projection space
    O.f4Position = mul(float4(I.f3Position, 1.0f), g_f4x4WorldViewProjLight);
    O.f4Diffuse = float4(0.0f, 0.0f, 0.0f, 1.0f);
    O.f2TexCoord = I.f2TexCoord;
    O.f4SMC = 0;

    return O;
}
