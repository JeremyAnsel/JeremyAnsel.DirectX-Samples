#include "Common.hlsl"

//
// GS for flat shaded rendering
//

[maxvertexcount(3)]
void main(triangle VSSceneOut input[3], inout TriangleStream<VSSceneOut> FlatTriStream)
{
    VSSceneOut output;
    
    //
    // Calculate the face normal
    //
    float3 faceEdgeA = input[1].wPos - input[0].wPos;
    float3 faceEdgeB = input[2].wPos - input[0].wPos;

    //
    // Cross product
    //
    float3 faceNormal = cross(faceEdgeA, faceEdgeB);
    
    //
    //calculate the face center
    //
    float3 faceCenter = (input[0].wPos + input[1].wPos + input[2].wPos) / 3.0;
    
    //find world pos and camera pos
    float4 worldPos = float4(faceCenter, 1);
    float4 cameraPos = mul(worldPos, g_mView);
    
    //do shading
    float3 worldNormal = normalize(faceNormal);
    ColorsOutput cOut = CalcLighting(worldNormal, worldPos.xyz, cameraPos.xyz);
    
    for (int i = 0; i < 3; i++)
    {
        output = input[i];
        output.colorD = cOut.Diffuse;
        output.colorS = cOut.Specular;
        
        FlatTriStream.Append(output);
    }
    FlatTriStream.RestartStrip();
}
