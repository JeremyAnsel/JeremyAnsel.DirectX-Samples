#include "Common.hlsl"

//
// VS for emulating fixed function pipeline
//

VSSceneOut main(VSSceneIn input)
{
    VSSceneOut output = (VSSceneOut) 0;

    //output our final position in clipspace
    float4 worldPos = mul(float4(input.pos, 1), g_mWorld);
    float4 cameraPos = mul(worldPos, g_mView); //Save cameraPos for fog calculations
    output.pos = mul(cameraPos, g_mProj);
    
    //save world pos for later
    output.wPos = worldPos.xyz;
    
    //save the fog distance for later
    output.fogDist = cameraPos.z;
    
    //find our clipping planes (fixed function clipping is done in world space)
    if (g_bEnableClipping)
    {
        worldPos.w = 1;
        
        //calc the distance from the 3 clipping planes
        output.planeDist.x = dot(worldPos, g_clipplanes[0]);
        output.planeDist.y = dot(worldPos, g_clipplanes[1]);
        output.planeDist.z = dot(worldPos, g_clipplanes[2]);
    }
    else
    {
        output.planeDist.x = 1;
        output.planeDist.y = 1;
        output.planeDist.z = 1;
    }
    
    //do gouraud lighting
    if (g_bEnableLighting)
    {
        float3 worldNormal = normalize(mul(input.norm, (float3x3) g_mWorld));
        output.wNorm = worldNormal;
        ColorsOutput cOut = CalcLighting(worldNormal, worldPos.xyz, cameraPos.xyz);
        output.colorD = cOut.Diffuse;
        output.colorS = cOut.Specular;
    }
    else
    {
        output.colorD = float4(1, 1, 1, 1);
    }
    
    //propogate texture coordinate
    output.tex = input.tex;
    
    return output;
}
