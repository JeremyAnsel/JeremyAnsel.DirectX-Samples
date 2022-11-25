#ifndef COMMON_HLSL
#define COMMON_HLSL

//--------------------------------------------------------------------------------------
// This file contains functions to convert from a Catmull-Clark subdivision
// representation to a bicubic patch representation.
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

//--------------------------------------------------------------------------------------
// A sample extraordinary SubD quad is represented by the following diagram:
//
//                        15              Valences:
//                       /  \               Vertex 0: 5
//                      /    14             Vertex 1: 4
//          17---------16   /  \            Vertex 2: 5
//          | \         |  /    \           Vertex 3: 3
//          |  \        | /      13
//          |   \       |/      /         Prefixes:
//          |    3------2------12           Vertex 0: 9
//          |    |      |      |            Vertex 1: 12
//          |    |      |      |            Vertex 2: 16
//          4----0------1------11           Vertex 3: 18
//         /    /|      |      |
//        /    / |      |      |
//       5    /  8------9------10
//        \  /  /
//         6   /
//          \ /
//           7
//
// Where the quad bounded by vertices 0,1,2,3 represents the actual subd surface of interest
// The 1-ring neighborhood of the quad is represented by vertices 4 through 17.  The counter-
// clockwise winding of this 1-ring neighborhood is important, especially when it comes to compute
// the corner vertices of the bicubic patch that we will use to approximate the subd quad (0,1,2,3).
// 
// The resulting bicubic patch fits within the subd quad (0,1,2,3) and has the following control
// point layout:
//
//     12--13--14--15
//      8---9--10--11
//      4---5---6---7
//      0---1---2---3
//
// The inner 4 control points of the bicubic patch are a combination of only the vertices (0,1,2,3)
// of the subd quad.  However, the corner control points for the bicubic patch (0,3,15,12) are actually
// a much more complex weighting of the subd patch and the 1-ring neighborhood.  In the example above
// the bicubic control point 0 is actually a weighted combination of subd points 0,1,2,3 and 1-ring
// neighborhood points 17, 4, 5, 6, 7, 8, and 9.  We can see that the 1-ring neighbor hood is simply
// walked from the prefix value from the previous corner (corner 3 in this case) to the prefix 
// prefix value for the current corner.  We add one more vertex on either side of the prefix values
// and we have all the data necessary to calculate the value for the corner points.
//
// The edge control points of the bicubic patch (1,2,13,14,4,8,7,11) are also combinations of their 
// neighbors, but fortunately each one is only a combination of 6 values and no walk is required.
//--------------------------------------------------------------------------------------

#define MOD4(x) ((x)&3)

#define MAX_POINTS 32
#define MAX_BONE_MATRICES 80

//--------------------------------------------------------------------------------------
// Textures
//--------------------------------------------------------------------------------------
Texture2D g_txHeight : register(t0); // Height and Bump texture
Texture2D g_txDiffuse : register(t1); // Diffuse texture
Texture2D g_txSpecular : register(t2); // Specular texture

//--------------------------------------------------------------------------------------
// Samplers
//--------------------------------------------------------------------------------------
SamplerState g_samLinear : register(s0);
SamplerState g_samPoint : register(s0);

//--------------------------------------------------------------------------------------
// Constant Buffers
//--------------------------------------------------------------------------------------
cbuffer cbTangentStencilConstants : register(b0)
{
    float g_TanM[1024]; // Tangent patch stencils precomputed by the application
    float g_fCi[16]; // Valence coefficients precomputed by the application
};

cbuffer cbPerMesh : register(b1)
{
    matrix g_mConstBoneWorld[MAX_BONE_MATRICES];
};

cbuffer cbPerFrame : register(b2)
{
    matrix g_mViewProjection;
    float4 g_vCameraPosWorld;
    float4 g_vSolidColor;
    float g_fTessellationFactor;
    float g_fDisplacementHeight;
};

cbuffer cbPerSubset : register(b3)
{
    int g_iPatchStartIndex;
}

//--------------------------------------------------------------------------------------
Buffer<uint4> g_ValencePrefixBuffer : register(t0);

//--------------------------------------------------------------------------------------
struct VS_CONTROL_POINT_OUTPUT
{
    float3 vPosition : WORLDPOS;
    float2 vUV : TEXCOORD0;
    float3 vTangent : TANGENT;
};

struct BEZIER_CONTROL_POINT
{
    float3 vPosition : BEZIERPOS;
};

struct PS_INPUT
{
    float3 vWorldPos : POSITION;
    float3 vNormal : NORMAL;
    float2 vUV : TEXCOORD;
    float3 vTangent : TANGENT;
    float3 vBiTangent : BITANGENT;
};

//--------------------------------------------------------------------------------------
// SubD to Bezier helper functions
//--------------------------------------------------------------------------------------
// Helps with getting tangent stencils from the g_TanM constant array
#define TANM(a,v) ( g_TanM[ Val[v]*64 + (a) ] )

#endif
