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
    class PatchPiece
    {
        public SdkMeshMesh m_pMesh;

        public int m_MeshIndex;

        // Index buffer for patches
        public D3D11Buffer m_pExtraordinaryPatchIB;

        // Stores the control points for mesh
        public D3D11Buffer m_pControlPointVB;

        // Stores valences and prefixes on a per-patch basis
        public D3D11Buffer m_pPerPatchDataVB;

        // Per-patch SRV
        public D3D11ShaderResourceView m_pPerPatchDataSRV;

        public int m_iPatchCount;

        public int m_iRegularExtraodinarySplitPoint;

        public D3D11Buffer m_pMyRegularPatchIB;

        public D3D11Buffer m_pMyExtraordinaryPatchIB;

        public D3D11Buffer m_pMyRegularPatchData;

        public D3D11Buffer m_pMyExtraordinaryPatchData;

        public D3D11ShaderResourceView m_pMyRegularPatchDataSRV;

        public D3D11ShaderResourceView m_pMyExtraordinaryPatchDataSRV;

        public readonly List<int> RegularPatchStart = new();

        public readonly List<int> ExtraordinaryPatchStart = new();

        public readonly List<int> RegularPatchCount = new();

        public readonly List<int> ExtraordinaryPatchCount = new();

        public XMFloat3 m_vCenter;

        public XMFloat3 m_vExtents;

        public int m_iFrameIndex;

        public void Destroy()
        {
            D3D11Utils.DisposeAndNull(ref this.m_pPerPatchDataSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pMyExtraordinaryPatchData);
            D3D11Utils.DisposeAndNull(ref this.m_pMyExtraordinaryPatchDataSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pMyRegularPatchData);
            D3D11Utils.DisposeAndNull(ref this.m_pMyRegularPatchDataSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pMyRegularPatchIB);
            D3D11Utils.DisposeAndNull(ref this.m_pMyExtraordinaryPatchIB);
        }
    }
}
