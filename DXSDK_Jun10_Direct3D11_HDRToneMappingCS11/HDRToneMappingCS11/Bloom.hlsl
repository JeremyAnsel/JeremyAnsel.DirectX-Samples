//--------------------------------------------------------------------------------------
// File: PSApproach.hlsl
//
// The PSs for doing post-processing, used in PS path of 
// HDRToneMappingCS11 sample
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

SamplerState PointSampler : register(s0);
SamplerState LinearSampler : register(s1);

struct QuadVS_Output
{
    float4 Pos : SV_POSITION;
    float2 Tex : TEXCOORD0;
};

Texture2D s0 : register(t0);
Texture2D s1 : register(t1);
Texture2D s2 : register(t2);

cbuffer cb0
{
    float2 g_avSampleOffsets[15];
    float4 g_avSampleWeights[15];
}

float4 main(QuadVS_Output Input) : SV_TARGET
{
    float4 vSample = 0.0f;
    float4 vColor = 0.0f;
    float2 vSamplePosition;
    
    for (int iSample = 0; iSample < 15; iSample++)
    {
        // Sample from adjacent points
        vSamplePosition = Input.Tex + g_avSampleOffsets[iSample];
        vColor = s0.Sample(PointSampler, vSamplePosition);
        
        vSample += g_avSampleWeights[iSample] * vColor;
    }
    
    return vSample;
}