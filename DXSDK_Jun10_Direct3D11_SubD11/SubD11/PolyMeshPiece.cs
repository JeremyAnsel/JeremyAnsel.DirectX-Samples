using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    class PolyMeshPiece
    {
        public SdkMeshMesh m_pMesh;

        public int m_MeshIndex;

        public int m_iFrameIndex;

        public XMFloat3 m_vCenter;

        public XMFloat3 m_vExtents;

        public D3D11Buffer m_pIndexBuffer;

        public D3D11Buffer m_pVertexBuffer;
    }
}
