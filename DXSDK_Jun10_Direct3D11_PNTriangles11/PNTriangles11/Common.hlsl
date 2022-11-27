
//--------------------------------------------------------------------------------------
// Constant buffer
//--------------------------------------------------------------------------------------

cbuffer cbPNTriangles : register(b0)
{
    float4x4 g_f4x4World; // World matrix for object
    float4x4 g_f4x4ViewProjection; // View * Projection matrix
    float4x4 g_f4x4WorldViewProjection; // World * View * Projection matrix
    float4 g_f4LightDir; // Light direction vector
    float4 g_f4Eye; // Eye
    float4 g_f4ViewVector; // View Vector
    float4 g_f4TessFactors; // Tessellation factors ( x=Edge, y=Inside, z=MinDistance, w=Range )
    float4 g_f4ScreenParams; // Screen resolution ( x=Current width, y=Current height )
    float4 g_f4GUIParams1; // GUI params1 ( x=BackFace Epsilon, y=Silhouette Epsilon, z=Range scale, w=Edge size )
    float4 g_f4GUIParams2; // GUI params2 ( x=Screen resolution scale, y=View Frustum Epsilon )
    float4 g_f4ViewFrustumPlanes[4]; // View frustum planes ( x=left, y=right, z=top, w=bottom )
}

// Some global lighting constants
static float4 g_f4MaterialDiffuseColor = float4(1.0f, 1.0f, 1.0f, 1.0f);
static float4 g_f4LightDiffuse = float4(1.0f, 1.0f, 1.0f, 1.0f);
static float4 g_f4MaterialAmbientColor = float4(0.2f, 0.2f, 0.2f, 1.0f);

//--------------------------------------------------------------------------------------
// Buffers, Textures and Samplers
//--------------------------------------------------------------------------------------

// Textures
Texture2D g_txDiffuse : register(t0);

// Samplers
SamplerState g_SamplePoint : register(s0);
SamplerState g_SampleLinear : register(s1);


//--------------------------------------------------------------------------------------
// Shader structures
//--------------------------------------------------------------------------------------

struct VS_RenderSceneInput
{
    float3 f3Position : POSITION;
    float3 f3Normal : NORMAL;
    float2 f2TexCoord : TEXCOORD;
};

struct HS_Input
{
    float3 f3Position : POSITION;
    float3 f3Normal : NORMAL;
    float2 f2TexCoord : TEXCOORD;
};

struct HS_ConstantOutput
{
    // Tess factor for the FF HW block
    float fTessFactor[3] : SV_TessFactor;
    float fInsideTessFactor : SV_InsideTessFactor;
    
    // Geometry cubic generated control points
    float3 f3B210 : POSITION3;
    float3 f3B120 : POSITION4;
    float3 f3B021 : POSITION5;
    float3 f3B012 : POSITION6;
    float3 f3B102 : POSITION7;
    float3 f3B201 : POSITION8;
    float3 f3B111 : CENTER;
    
    // Normal quadratic generated control points
    float3 f3N110 : NORMAL3;
    float3 f3N011 : NORMAL4;
    float3 f3N101 : NORMAL5;
};

struct HS_ControlPointOutput
{
    float3 f3Position : POSITION;
    float3 f3Normal : NORMAL;
    float2 f2TexCoord : TEXCOORD;
};

struct DS_Output
{
    float4 f4Position : SV_Position;
    float2 f2TexCoord : TEXCOORD0;
    float4 f4Diffuse : COLOR0;
};

struct PS_RenderSceneInput
{
    float4 f4Position : SV_Position;
    float2 f2TexCoord : TEXCOORD0;
    float4 f4Diffuse : COLOR0;
};

struct PS_RenderOutput
{
    float4 f4Color : SV_Target0;
};
