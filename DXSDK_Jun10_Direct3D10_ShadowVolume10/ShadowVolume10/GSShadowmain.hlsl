//
// GS for generating shadow volumes
//

#include "Common.hlsl"

//
// Helper to detect a silhouette edge and extrude a volume from it
//
void DetectAndProcessSilhouette(float3 N, // Un-normalized triangle normal
                                 GSShadowIn v1, // Shared vertex
                                 GSShadowIn v2, // Shared vertex
                                 inout TriangleStream<PSShadowIn> ShadowTriangleStream // triangle stream
                                 )
{
    float3 outpos[4];
    float3 extrude1 = normalize(v1.pos - g_vLightPos.xyz);
    float3 extrude2 = normalize(v2.pos - g_vLightPos.xyz);
        
    outpos[0] = v1.pos + g_fExtrudeBias * extrude1;
    outpos[1] = v1.pos + g_fExtrudeAmt * extrude1;
    outpos[2] = v2.pos + g_fExtrudeBias * extrude2;
    outpos[3] = v2.pos + g_fExtrudeAmt * extrude2;
        
    // Extrude silhouette to create two new triangles
    PSShadowIn Out;
    for (int v = 0; v < 4; v++)
    {
        Out.pos = mul(float4(outpos[v], 1), g_mViewProj);
        ShadowTriangleStream.Append(Out);
    }
    ShadowTriangleStream.RestartStrip();
}

[maxvertexcount(18)]
void main(triangle GSShadowIn In[3], inout TriangleStream<PSShadowIn> ShadowTriangleStream)
{
    // Compute un-normalized triangle normal
    float3 N = cross(In[1].pos - In[0].pos, In[2].pos - In[0].pos);
    
    // Compute direction from this triangle to the light
    float3 lightDir = g_vLightPos.xyz - In[0].pos;
    
    //if we're facing the light
    if (dot(N, lightDir) > 0.0f)
    {
        // For each edge of the triangle, determine if it is a silhouette edge
        DetectAndProcessSilhouette(lightDir, In[0], In[1], ShadowTriangleStream);
        DetectAndProcessSilhouette(lightDir, In[1], In[2], ShadowTriangleStream);
        DetectAndProcessSilhouette(lightDir, In[2], In[0], ShadowTriangleStream);
        
        PSShadowIn Out;
        int v;

        //near cap
        for (v = 0; v < 3; v++)
        {
            float3 extrude = normalize(In[v].pos - g_vLightPos.xyz);
            
            float3 pos = In[v].pos + g_fExtrudeBias * extrude;
            Out.pos = mul(float4(pos, 1), g_mViewProj);
            ShadowTriangleStream.Append(Out);
        }
        ShadowTriangleStream.RestartStrip();
        
        //far cap (reverse the order)
        for (v = 2; v >= 0; v--)
        {
            float3 extrude = normalize(In[v].pos - g_vLightPos.xyz);
        
            float3 pos = In[v].pos + g_fExtrudeAmt * extrude;
            Out.pos = mul(float4(pos, 1), g_mViewProj);
            ShadowTriangleStream.Append(Out);
        }
        ShadowTriangleStream.RestartStrip();
    }
}
