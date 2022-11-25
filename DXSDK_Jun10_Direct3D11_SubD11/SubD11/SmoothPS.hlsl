#include "Common.hlsl"

//--------------------------------------------------------------------------------------
// Smooth shading pixel shader section
//--------------------------------------------------------------------------------------

float3 safe_normalize(float3 vInput)
{
    float len2 = dot(vInput, vInput);
    if (len2 > 0)
    {
        return vInput * rsqrt(len2);
    }
    return vInput;
}

static const float g_fSpecularExponent = 32.0f;
static const float g_fSpecularIntensity = 0.6f;
static const float g_fNormalMapIntensity = 1.5f;

float2 ComputeDirectionalLight(float3 vWorldPos, float3 vWorldNormal, float3 vDirLightDir)
{
    // Result.x is diffuse illumination, Result.y is specular illumination
    float2 Result = float2(0, 0);
    Result.x = pow(saturate(dot(vWorldNormal, -vDirLightDir)), 2);
    
    float3 vPointToCamera = normalize(g_vCameraPosWorld.xyz - vWorldPos);
    float3 vHalfAngle = normalize(vPointToCamera - vDirLightDir);
    Result.y = pow(saturate(dot(vHalfAngle, vWorldNormal)), g_fSpecularExponent);
    
    return Result;
}

float3 ColorGamma(float3 Input)
{
    return pow(Input, 2.2f);
}

float4 main(PS_INPUT Input) : SV_TARGET
{
    float4 vNormalMapSampleRaw = g_txHeight.Sample(g_samLinear, Input.vUV);
    float3 vNormalMapSampleBiased = (vNormalMapSampleRaw.xyz * 2) - 1;
    vNormalMapSampleBiased.xy *= g_fNormalMapIntensity;
    float3 vNormalMapSample = normalize(vNormalMapSampleBiased);
    
    float3 vNormal = safe_normalize(Input.vNormal) * vNormalMapSample.z;
    vNormal += safe_normalize(Input.vTangent) * vNormalMapSample.x;
    vNormal += safe_normalize(Input.vBiTangent) * vNormalMapSample.y;
                     
    //float3 vColor = float3( 1, 1, 1 );
    float3 vColor = g_txDiffuse.Sample(g_samLinear, Input.vUV).rgb;
    float vSpecular = g_txSpecular.Sample(g_samLinear, Input.vUV).r * g_fSpecularIntensity;
    
    const float3 DirLightDirections[4] =
    {
        // key light
        normalize(float3(-63.345150, -58.043934, 27.785097)),
        // fill light
        normalize(float3(23.652107, -17.391443, 54.972504)),
        // back light 1
        normalize(float3(20.470509, -22.939510, -33.929531)),
        // back light 2
        normalize(float3(-31.003685, 24.242104, -41.352859)),
    };
    
    const float3 DirLightColors[4] =
    {
        // key light
        ColorGamma(float3(1.0f, 0.964f, 0.706f) * 1.0f),
        // fill light
        ColorGamma(float3(0.446f, 0.641f, 1.0f) * 1.0f),
        // back light 1
        ColorGamma(float3(1.0f, 0.862f, 0.419f) * 1.0f),
        // back light 2
        ColorGamma(float3(0.405f, 0.630f, 1.0f) * 1.0f),
    };
        
    float3 fLightColor = 0;
    for (int i = 0; i < 4; ++i)
    {
        float2 LightDiffuseSpecular = ComputeDirectionalLight(Input.vWorldPos, vNormal, DirLightDirections[i]);
        fLightColor += DirLightColors[i] * vColor * LightDiffuseSpecular.x;
        fLightColor += DirLightColors[i] * LightDiffuseSpecular.y * vSpecular;
    }
    
    return float4(fLightColor, 1);
}
