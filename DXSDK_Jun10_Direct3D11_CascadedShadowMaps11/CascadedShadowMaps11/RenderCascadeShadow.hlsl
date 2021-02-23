
//--------------------------------------------------------------------------------------
// File: RenderCascadeShadow.hlsl
//
// The shader file for the RenderCascadeScene sample.  
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------
// Globals
//--------------------------------------------------------------------------------------
cbuffer cbPerObject : register(b0)
{
    matrix        g_mWorldViewProjection    : packoffset(c0);
};

//--------------------------------------------------------------------------------------
// Input / Output structures
//--------------------------------------------------------------------------------------
struct VS_INPUT
{
    float4 vPosition    : POSITION;
};

struct VS_OUTPUT
{
    float4 vPosition    : SV_POSITION;
};

//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------
VS_OUTPUT main(VS_INPUT Input)
{
    VS_OUTPUT Output;

    // There is nothing special here, just transform and write out the depth.
    Output.vPosition = mul(Input.vPosition, g_mWorldViewProjection);

    // VSMainPancake
    // after transform move clipped geometry to near plane
    //Output.vPosition.z = max( Output.vPosition.z, 0.0f );

    return Output;
}
