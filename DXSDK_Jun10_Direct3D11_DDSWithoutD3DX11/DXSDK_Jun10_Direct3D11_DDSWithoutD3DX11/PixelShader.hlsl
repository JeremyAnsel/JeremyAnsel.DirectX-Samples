//--------------------------------------------------------------------------------------
// File: DDSWithoutD3DX.hlsl
//
// The HLSL file for the DDSWithoutD3DX sample for the Direct3D 11 device
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------


//-----------------------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------------------
Texture2D    g_txDiffuse : register(t0);
SamplerState g_samLinear : register(s0);

//--------------------------------------------------------------------------------------
// shader input/output structure
//--------------------------------------------------------------------------------------
struct VS_OUTPUT
{
    float4 Position     : SV_POSITION; // vertex position 
    float4 Diffuse      : COLOR0;      // vertex diffuse color (note that COLOR0 is clamped from 0..1)
    float2 TextureUV    : TEXCOORD0;   // vertex texture coords 
};

//--------------------------------------------------------------------------------------
// This shader outputs the pixel's color by modulating the texture's
// color with diffuse material color
//--------------------------------------------------------------------------------------
float4 main(VS_OUTPUT In) : SV_TARGET
{
    // Lookup mesh texture and modulate it with diffuse
    return g_txDiffuse.Sample(g_samLinear, In.TextureUV) * In.Diffuse;
}
