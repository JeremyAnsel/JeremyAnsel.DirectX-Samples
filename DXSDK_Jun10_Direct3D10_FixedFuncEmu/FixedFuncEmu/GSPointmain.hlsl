#include "Common.hlsl"

//
// GS for point rendering
//

static const float3 g_positions[4] =
{
    float3(-0.5, 0.5, 0),
        float3(0.5, 0.5, 0),
        float3(-0.5, -0.5, 0),
        float3(0.5, -0.5, 0),
};

[maxvertexcount(12)]
void main(triangle VSSceneOut input[3], inout TriangleStream<VSSceneOut> PointTriStream)
{
    VSSceneOut output;
    
    //
    // Calculate the point size
    //
    //float fSizeX = (g_pointSize/g_viewportWidth)/4.0;
    float fSizeY = (g_pointSize / g_viewportHeight) / 4.0;
    float fSizeX = fSizeY;
    
    for (int i = 0; i < 3; i++)
    {
        output = input[i];
    
        //find world pos and camera pos
        float4 worldPos = float4(input[i].wPos, 1);
        float4 cameraPos = mul(worldPos, g_mView);
        
        //find our size
        if (g_bPointScaleEnable)
        {
            float dEye = length(cameraPos.xyz);
            fSizeX = fSizeY = g_viewportHeight * g_pointSize *
                    sqrt(1.0f / (g_pointScaleA + g_pointScaleB * dEye + g_pointScaleC * (dEye * dEye)));
        }
        
        //do shading
        if (g_bEnableLighting)
        {
            float3 worldNormal = input[i].wNorm;
            ColorsOutput cOut = CalcLighting(worldNormal, worldPos.xyz, cameraPos.xyz);
        
            output.colorD = cOut.Diffuse;
            output.colorS = cOut.Specular;
        }
        else
        {
            output.colorD = float4(1, 1, 1, 1);
        }
        
        output.tex = input[i].tex;
        
        //
        // Emit two new triangles
        //
        for (int i = 0; i < 4; i++)
        {
            float4 outPos = mul(worldPos, g_mView);
            output.pos = mul(outPos, g_mProj);
            float zoverNear = (outPos.z) / g_nearPlane;
            float4 posSize = float4(g_positions[i].x * fSizeX * zoverNear,
                                     g_positions[i].y * fSizeY * zoverNear,
                                     0,
                                     0);
            output.pos += posSize;
            
            PointTriStream.Append(output);
        }
        PointTriStream.RestartStrip();
    }
}
