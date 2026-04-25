using JeremyAnsel.DirectX.DXMath;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PNTriangles11
{
    /// <summary>
    /// Constant buffer layout for transfering data to the PN-Triangles HLSL functions
    /// </summary>
    unsafe struct PNTrianglesConstantBufferData
    {
        // World matrix for object
        public XMMatrix f4x4World;

        // View * Projection matrix
        public XMMatrix f4x4ViewProjection;

        // World * View * Projection matrix
        public XMMatrix f4x4WorldViewProjection;

        // Light direction vector
        public XMVector fLightDir;

        // Eye
        public XMVector fEye;

        // View Vector
        public XMVector fViewVector;

        // Tessellation factors ( x=Edge, y=Inside, z=MinDistance, w=Range )
        public XMVector fTessFactors;

        // Screen params ( x=Current width, y=Current height )
        public XMVector fScreenParams;

        // GUI params1 ( x=BackFace Epsilon, y=Silhouette Epsilon, z=Range scale, w=Edge size )
        public XMVector fGUIParams1;

        // GUI params2 ( x=Screen resolution scale, y=View Frustum Epsilon )
        public XMVector fGUIParams2;

        // View frustum planes
        public fixed float f4ViewFrustumPlanes[4 * 4];

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PNTrianglesConstantBufferData));
    }
}
