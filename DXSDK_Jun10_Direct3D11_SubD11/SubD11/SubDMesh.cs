using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    /// <summary>
    /// This class handles most of the loading and conversion for a subd mesh.
    /// It also creates and tracks buffers used by the mesh.
    /// </summary>
    class SubDMesh
    {
        private const uint g_iBindPerSubset = 3;

        private readonly D3D11Device d3dDevice;
        private readonly D3D11DeviceContext d3dDeviceContext;

        private D3D11Texture2D g_pDefaultDiffuseTexture;
        private D3D11Texture2D g_pDefaultNormalTexture;
        private D3D11Texture2D g_pDefaultSpecularTexture;
        private D3D11ShaderResourceView g_pDefaultDiffuseSRV;
        private D3D11ShaderResourceView g_pDefaultNormalSRV;
        private D3D11ShaderResourceView g_pDefaultSpecularSRV;

        private readonly List<PatchPiece> m_PatchPieces = new();

        private readonly List<PolyMeshPiece> m_PolyMeshPieces = new();

        private SdkMeshFile m_pMeshFile;

        private D3D11Buffer m_pPerSubsetCB;

        private int m_iCameraFrameIndex;

        public SubDMesh(D3D11Device pd3dDevice, D3D11DeviceContext pd3dDeviceContext)
        {
            this.d3dDevice = pd3dDevice ?? throw new ArgumentNullException(nameof(pd3dDevice));
            this.d3dDeviceContext = pd3dDeviceContext ?? throw new ArgumentNullException(nameof(pd3dDeviceContext));
        }

        /// <summary>
        /// Creates a 1x1 uncompressed texture containing the specified color.
        /// </summary>
        private static void CreateSolidTexture(D3D11Device pd3dDevice, uint ColorRGBA, out D3D11Texture2D ppTexture2D, out D3D11ShaderResourceView ppSRV)
        {
            D3D11Texture2DDesc desc = new(
                DxgiFormat.R8G8B8A8UNorm,
                1,
                1,
                1,
                1,
                D3D11BindOptions.ShaderResource,
                D3D11Usage.Default);

            uint[] pixels = new[]
            {
                ColorRGBA
            };

            D3D11SubResourceData[] data = new[]
            {
                new D3D11SubResourceData(pixels, 4)
            };

            ppTexture2D = pd3dDevice.CreateTexture2D(desc, data);
            ppSRV = pd3dDevice.CreateShaderResourceView(ppTexture2D, null);

#if DEBUG
            string clr = $"CLR: {ColorRGBA:X8}";
            ppTexture2D.SetDebugName(clr);
            ppSRV.SetDebugName(clr);
#endif
        }

        /// <summary>
        /// Creates three default textures to be used to replace missing content in the mesh file.
        /// </summary>
        private void CreateDefaultTextures()
        {
            if (this.g_pDefaultDiffuseTexture is null)
            {
                CreateSolidTexture(this.d3dDevice, 0xFF808080, out this.g_pDefaultDiffuseTexture, out this.g_pDefaultDiffuseSRV);
            }

            if (this.g_pDefaultNormalTexture is null)
            {
                CreateSolidTexture(this.d3dDevice, 0x80FF8080, out this.g_pDefaultNormalTexture, out this.g_pDefaultNormalSRV);
            }

            if (this.g_pDefaultSpecularTexture is null)
            {
                CreateSolidTexture(this.d3dDevice, 0xFF000000, out this.g_pDefaultSpecularTexture, out this.g_pDefaultSpecularSRV);
            }
        }

        public int GetNumInfluences(int iMeshIndex)
        {
            return this.m_pMeshFile.Meshes[iMeshIndex].FrameInfluencesIndices.Count;
        }

        public XMMatrix GetInfluenceMatrix(int iMeshIndex, int iInfluence)
        {
            return this.m_pMeshFile.GetMeshInfluenceMatrix(iMeshIndex, iInfluence);
        }

        public int GetPatchMeshIndex(int iPatchPiece)
        {
            return this.m_PatchPieces[iPatchPiece].m_MeshIndex;
        }

        public int GetNumPatchPieces()
        {
            return this.m_PatchPieces.Count;
        }

        public bool GetPatchPieceTransform(int iPatchPiece, out XMMatrix pTransform)
        {
            int iFrameIndex = this.m_PatchPieces[iPatchPiece].m_iFrameIndex;

            if (iFrameIndex == -1)
            {
                pTransform = XMMatrix.Identity;
            }
            else
            {
                pTransform = this.m_pMeshFile.GetWorldMatrix(iFrameIndex);
            }

            return true;
        }

        public int GetPolyMeshIndex(int iPolyMeshPiece)
        {
            return this.m_PolyMeshPieces[iPolyMeshPiece].m_MeshIndex;
        }

        public int GetNumPolyMeshPieces()
        {
            return this.m_PolyMeshPieces.Count;
        }

        public bool GetPolyMeshPieceTransform(int iPolyMeshPiece, out XMMatrix pTransform)
        {
            int iFrameIndex = this.m_PolyMeshPieces[iPolyMeshPiece].m_iFrameIndex;

            if (iFrameIndex == -1)
            {
                pTransform = XMMatrix.Identity;
            }
            else
            {
                pTransform = this.m_pMeshFile.GetWorldMatrix(iFrameIndex);
            }

            return true;
        }

        public void Destroy()
        {
            for (int i = 0; i < this.m_PatchPieces.Count; i++)
            {
                this.m_PatchPieces[i].Destroy();
            }

            this.m_PatchPieces.Clear();

            this.m_PolyMeshPieces.Clear();

            D3D11Utils.DisposeAndNull(ref this.m_pPerSubsetCB);

            D3D11Utils.DisposeAndNull(ref this.g_pDefaultDiffuseSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pDefaultDiffuseTexture);
            D3D11Utils.DisposeAndNull(ref this.g_pDefaultNormalSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pDefaultNormalTexture);
            D3D11Utils.DisposeAndNull(ref this.g_pDefaultSpecularSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pDefaultSpecularTexture);

            this.m_pMeshFile?.Release();
            this.m_pMeshFile = null;
        }

        /// <summary>
        /// Sets the specified material parameters (textures) into the D3D device.
        /// </summary>
        /// <param name="MaterialID"></param>
        private void SetupMaterial(int MaterialID)
        {
            SdkMeshMaterial pMaterial = this.m_pMeshFile.Materials[MaterialID];

            D3D11ShaderResourceView[] Resources = new[]
            {
                pMaterial.NormalTextureView,
                pMaterial.DiffuseTextureView,
                pMaterial.SpecularTextureView
            };

            if (Resources[0] is null)
            {
                Resources[0] = this.g_pDefaultNormalSRV;
            }

            if (Resources[1] is null)
            {
                Resources[1] = this.g_pDefaultDiffuseSRV;
            }

            if (Resources[2] is null)
            {
                Resources[2] = this.g_pDefaultSpecularSRV;
            }

            // The domain shader only needs the heightmap, so we only set 1 slot here.
            this.d3dDeviceContext.DomainShaderSetShaderResources(0, new[] { Resources[0] });

            // The pixel shader samples from all 3 textures.
            this.d3dDeviceContext.PixelShaderSetShaderResources(0, Resources);
        }

        public void Update(XMMatrix pWorld, double fTime)
        {
            this.m_pMeshFile.TransformMesh(pWorld, fTime);
        }

        public bool GetCameraViewMatrix(ref XMMatrix pViewMatrix, ref XMFloat3 pCameraPosWorld)
        {
            if (this.m_iCameraFrameIndex == -1)
            {
                return false;
            }

            XMMatrix matRotation = XMMatrix.RotationY(XMMath.PIDivTwo);
            XMMatrix pCameraWorld = this.m_pMeshFile.GetWorldMatrix(this.m_iCameraFrameIndex);
            pCameraPosWorld = new(pCameraWorld.M41, pCameraWorld.M42, pCameraWorld.M43);

            XMMatrix matCamera = matRotation * pCameraWorld;
            //XMMatrix matCamera = pCameraWorld * matRotation;
            pViewMatrix = matCamera.Inverse();

            return true;
        }

        public float GetAnimationDuration()
        {
            bool bAnimationPresent = this.m_pMeshFile.GetAnimationProperties(out int iKeyCount, out float fFrameTime);

            if (!bAnimationPresent)
            {
                return 0.0f;
            }

            return iKeyCount * fFrameTime;
        }

        private static void ExpandAABB(ref XMVector vCenter, ref XMVector vExtents, XMVector vAddCenter, XMVector vAddExtents)
        {
            if (vExtents.X == 0 && vExtents.Y == 0 && vExtents.Z == 0)
            {
                vCenter = vAddCenter;
                vExtents = vAddExtents;
                return;
            }

            XMVector vCurrentMin = XMVector.Subtract(vCenter, vExtents);
            XMVector vCurrentMax = XMVector.Add(vCenter, vExtents);

            XMVector vAddMin = XMVector.Subtract(vAddCenter, vAddExtents);
            XMVector vAddMax = XMVector.Add(vAddCenter, vAddExtents);

            vCurrentMax = XMVector.Max(vCurrentMax, vAddMax);
            vCurrentMin = XMVector.Min(vCurrentMin, vAddMin);

            vCenter = (vCurrentMax + vCurrentMin) / 2.0f;
            vExtents = (vCurrentMax - vCurrentMin) / 2.0f;
        }

        public void GetBounds(out XMFloat3 pvCenter, out XMFloat3 pvExtents)
        {
            XMVector vCenter = XMVector.Zero;
            XMVector vExtents = XMVector.Zero;

            int iCount;

            iCount = this.GetNumPatchPieces();

            for (int i = 0; i < iCount; i++)
            {
                PatchPiece Piece = this.m_PatchPieces[i];
                GetPatchPieceTransform(i, out XMMatrix matTransform);
                XMVector vPieceCenter, vPieceExtents;
                vPieceCenter = XMVector3.TransformCoord(Piece.m_vCenter, matTransform);
                vPieceExtents = XMVector3.TransformNormal(Piece.m_vExtents, matTransform);
                ExpandAABB(ref vCenter, ref vExtents, vPieceCenter, vPieceExtents);
            }

            iCount = GetNumPolyMeshPieces();

            for (int i = 0; i < iCount; i++)
            {
                PolyMeshPiece Piece = this.m_PolyMeshPieces[i];
                GetPolyMeshPieceTransform(i, out XMMatrix matTransform);
                XMVector vPieceCenter, vPieceExtents;
                vPieceCenter = XMVector3.TransformCoord(Piece.m_vCenter, matTransform);
                vPieceExtents = XMVector3.TransformNormal(Piece.m_vExtents, matTransform);
                ExpandAABB(ref vCenter, ref vExtents, vPieceCenter, vPieceExtents);
            }

            pvCenter = vCenter;
            pvExtents = vExtents;
        }

        private static int nRegular = 0;
        private static int nHighestVal = 0;
        private static int nLowestVal = 100;
        private static int nPatches = 0;
        private static int nSubsets = 0;

        /// <summary>
        /// Loads a specially constructed SDKMESH file from disk.  This SDKMESH file contains a
        /// preprocessed Catmull-Clark subdivision surface, complete with topology and adjacency
        /// data, as well as the typical mesh vertex data.
        /// </summary>
        public void LoadSubDFromSDKMesh(string strFileName, string strCameraName)
        {
            // Load the file
            this.m_pMeshFile = SdkMeshFile.FromFile(this.d3dDevice, this.d3dDeviceContext, strFileName);
            SdkMeshRawFile rawFile = SdkMeshRawFile.FromFile(strFileName);

            int MeshCount = this.m_pMeshFile.Meshes.Count;

            if (MeshCount == 0)
            {
                return;
            }

            int FrameCount = this.m_pMeshFile.Frames.Count;

            // Find camera frame
            this.m_iCameraFrameIndex = -1;

            for (int i = 0; i < FrameCount; ++i)
            {
                SdkMeshFrame pFrame = this.m_pMeshFile.Frames[i];

                if (string.Equals(pFrame.Name, strCameraName, StringComparison.OrdinalIgnoreCase))
                {
                    m_iCameraFrameIndex = i;
                }
            }

            // Load mesh pieces
            for (int meshIndex = 0; meshIndex < MeshCount; meshIndex++)
            {
                SdkMeshMesh pMesh = this.m_pMeshFile.Meshes[meshIndex];

                nSubsets += pMesh.Subsets.Count;

                if (pMesh.VertexBuffers.Length == 1)
                {
                    PolyMeshPiece pPolyMeshPiece = new()
                    {
                        m_pMesh = pMesh,
                        m_MeshIndex = meshIndex,
                        m_pIndexBuffer = pMesh.IndexBuffer.Buffer,
                        m_pVertexBuffer = pMesh.VertexBuffers[0].Buffer
                    };

                    // Find frame that corresponds to this mesh
                    pPolyMeshPiece.m_iFrameIndex = -1;

                    for (int j = 0; j < FrameCount; j++)
                    {
                        SdkMeshFrame pFrame = this.m_pMeshFile.Frames[j];

                        if (pFrame.MeshIndex == pPolyMeshPiece.m_MeshIndex)
                        {
                            pPolyMeshPiece.m_iFrameIndex = j;
                        }
                    }

                    pPolyMeshPiece.m_vCenter = pMesh.BoundingBoxCenter;
                    pPolyMeshPiece.m_vExtents = pMesh.BoundingBoxExtents;

                    this.m_PolyMeshPieces.Add(pPolyMeshPiece);
                }
                else
                {
                    // SubD meshes have 2 vertex buffers: a control point VB and a patch data VB
                    Trace.Assert(pMesh.VertexBuffers.Length == 2);
                    // Make sure the control point VB has the correct stride
                    Trace.Assert(this.m_pMeshFile.Meshes[meshIndex].VertexBuffers[0].StrideBytes == SubDControlPoint.Size);
                    // Make sure we have at least one subset
                    Trace.Assert(this.m_pMeshFile.Meshes[meshIndex].Subsets.Count > 0);
                    // Make sure the first subset is made up of quad patches
                    Trace.Assert(this.m_pMeshFile.Meshes[meshIndex].Subsets[0].PrimitiveTopology == D3D11PrimitiveTopology.TriangleList);
                    // Make sure the IB is a multiple of the max point size
                    Trace.Assert(this.m_pMeshFile.Meshes[meshIndex].IndexBuffer.NumIndices % Constants.MaxExtraordinaryPoints == 0);

                    // Create a new mesh piece and fill it in with all of the buffer pointers
                    PatchPiece pPatchPiece = new()
                    {
                        m_pMesh = pMesh,
                        m_MeshIndex = meshIndex,
                        m_pExtraordinaryPatchIB = pMesh.IndexBuffer.Buffer,
                        m_pControlPointVB = pMesh.VertexBuffers[0].Buffer,
                        m_pPerPatchDataVB = pMesh.VertexBuffers[1].Buffer
                    };

                    int iNumPatches = pMesh.IndexBuffer.NumIndices / Constants.MaxExtraordinaryPoints;
                    pPatchPiece.m_iPatchCount = iNumPatches;

                    // This is the same data as what's in pPatchPiece->m_pPerPatchDataVB
                    var patchData = new PatchData[iNumPatches];

                    byte[] patchDataBytes = rawFile.VertexBufferBytes[rawFile.Meshes[meshIndex].VertexBuffers[1]];
                    IntPtr patchDataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(patchDataBytes, 0);

                    for (int i = 0; i < iNumPatches; i++)
                    {
                        patchData[i] = Marshal.PtrToStructure<PatchData>(patchDataPtr + i * (int)PatchData.Size);
                    }

                    //D3D11MappedSubResource patchDataMap = this.d3dDeviceContext.Map(pMesh.VertexBuffers[1].Buffer, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                    //try
                    //{
                    //    for (int i = 0; i < iNumPatches; i++)
                    //    {
                    //        patchData[i] = Marshal.PtrToStructure<PatchData>(patchDataMap.Data + i * (int)PatchData.Size);
                    //    }
                    //}
                    //finally
                    //{
                    //    this.d3dDeviceContext.Unmap(pMesh.VertexBuffers[meshIndex].Buffer, 0);
                    //}

                    nPatches += iNumPatches;

                    pPatchPiece.m_iRegularExtraodinarySplitPoint = -1;

                    // Loop through all patches inside this patch piece (patch piece is one mesh inside our sdkmesh) to get some statistical data
                    for (int i = 0; i < iNumPatches; i++)
                    {
                        // How many regular patches do we have?
                        if (patchData[i].val[0] == 4 && patchData[i].val[1] == 4 && patchData[i].val[2] == 4 && patchData[i].val[3] == 4)
                        {
                            nRegular++;
                        }

                        // What's the highest and lowest valence?
                        for (int j = 0; j < 4; j++)
                        {
                            if (patchData[i].val[j] > nHighestVal)
                            {
                                nHighestVal = patchData[i].val[j];
                            }

                            if (patchData[i].val[j] < nLowestVal)
                            {
                                nLowestVal = patchData[i].val[j];
                            }
                        }

                        // Met with the first patch which is extraordinary?
                        if ((patchData[i].val[0] != 4 || patchData[i].val[1] != 4 || patchData[i].val[2] != 4 || patchData[i].val[3] != 4)
                            && pPatchPiece.m_iRegularExtraodinarySplitPoint == -1)
                        {
                            pPatchPiece.m_iRegularExtraodinarySplitPoint = i;
                        }

                        // Ensure that all patches after the regular-extraordinary split point are extraordinary patches
                        if (pPatchPiece.m_iRegularExtraodinarySplitPoint > 0)
                        {
                            Trace.Assert(patchData[i].val[0] != 4 || patchData[i].val[1] != 4 || patchData[i].val[2] != 4 || patchData[i].val[3] != 4);
                        }
                    }

                    // this is the same data as what's in pPatchPiece->m_pExtraordinaryPatchIB
                    int[] idx = new int[pMesh.IndexBuffer.NumIndices];

                    byte[] idxBytes = rawFile.IndexBufferBytes[rawFile.Meshes[meshIndex].IndexBuffer];
                    IntPtr idxPtr = Marshal.UnsafeAddrOfPinnedArrayElement(idxBytes, 0);

                    for (int i = 0; i < idx.Length; i++)
                    {
                        idx[i] = Marshal.ReadInt32(idxPtr + i * 4);
                    }

                    //D3D11MappedSubResource idxMap = this.d3dDeviceContext.Map(pMesh.IndexBuffer.Buffer, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                    //try
                    //{
                    //    for (int i = 0; i < idx.Length; i++)
                    //    {
                    //        idx[i] = Marshal.ReadInt32(idxMap.Data + i * 4);
                    //    }
                    //}
                    //finally
                    //{
                    //    this.d3dDeviceContext.Unmap(pMesh.IndexBuffer.Buffer, 0);
                    //}

                    List<int> vRegularIdxBuf = new();
                    List<int> vExtraordinaryIdxBuf = new();
                    List<PatchData> vRegularPatchData = new();
                    List<PatchData> vExtraordinaryPatchData = new();

                    int nNumSub = this.m_pMeshFile.Meshes[pPatchPiece.m_MeshIndex].Subsets.Count;

                    // loop through all subsets inside this patch piece
                    for (int i = 0; i < nNumSub; i++)
                    {
                        SdkMeshSubset pSubset = this.m_pMeshFile.Meshes[pPatchPiece.m_MeshIndex].Subsets[i];

                        int NumIndices = pSubset.IndexCount; // this is actually the number of patches in current subset
                        int StartIndex = pSubset.IndexStart; // this is actually the patch start index of current subset in patch data

                        pPatchPiece.RegularPatchStart.Add(vRegularPatchData.Count);
                        pPatchPiece.ExtraordinaryPatchStart.Add(vExtraordinaryPatchData.Count);

                        // loop through all patches inside this subset
                        for (int j = 0; j < NumIndices; j++)
                        {
                            if (patchData[j + StartIndex].val[0] == 4 && patchData[j + StartIndex].val[1] == 4 &&
                                    patchData[j + StartIndex].val[2] == 4 && patchData[j + StartIndex].val[3] == 4)
                            {
                                // this patch is regular
                                for (int k = 0; k < Constants.MaxExtraordinaryPoints; k++)
                                {
                                    vRegularIdxBuf.Add(idx[(StartIndex + j) * Constants.MaxExtraordinaryPoints + k]);
                                }

                                vRegularPatchData.Add(patchData[j + StartIndex]);
                            }
                            else
                            {
                                // this patch is extraordinary
                                for (int k = 0; k < Constants.MaxExtraordinaryPoints; k++)
                                {
                                    vExtraordinaryIdxBuf.Add(idx[(StartIndex + j) * Constants.MaxExtraordinaryPoints + k]);
                                }

                                vExtraordinaryPatchData.Add(patchData[j + StartIndex]);
                            }
                        }

                        pPatchPiece.RegularPatchCount.Add(vRegularPatchData.Count - pPatchPiece.RegularPatchStart[pPatchPiece.RegularPatchStart.Count - 1]);
                        pPatchPiece.ExtraordinaryPatchCount.Add(vExtraordinaryPatchData.Count - pPatchPiece.ExtraordinaryPatchStart[pPatchPiece.ExtraordinaryPatchStart.Count - 1]);
                    }

                    D3D11BufferDesc desc;
                    D3D11SubResourceData initdata;

                    // Create index buffer for the regular patches
                    desc = new D3D11BufferDesc((uint)vRegularIdxBuf.Count * 4, D3D11BindOptions.IndexBuffer);
                    initdata = new(vRegularIdxBuf.ToArray(), 0);
                    pPatchPiece.m_pMyRegularPatchIB = this.d3dDevice.CreateBuffer(desc, initdata);
                    pPatchPiece.m_pMyRegularPatchIB.SetDebugName("SubDMesh IB");

                    // Create index buffer for the extraordinary patches
                    desc = new D3D11BufferDesc((uint)vExtraordinaryIdxBuf.Count * 4, D3D11BindOptions.IndexBuffer);
                    initdata = new(vExtraordinaryIdxBuf.ToArray(), 0);
                    pPatchPiece.m_pMyExtraordinaryPatchIB = this.d3dDevice.CreateBuffer(desc, initdata);
                    pPatchPiece.m_pMyExtraordinaryPatchIB.SetDebugName("SubDMesh Xord IB");

                    // Create per-patch data buffer for regular patches
                    desc = new D3D11BufferDesc((uint)vRegularPatchData.Count * PatchData.Size, D3D11BindOptions.ShaderResource);
                    initdata = new(vRegularPatchData.ToArray(), 0);
                    pPatchPiece.m_pMyRegularPatchData = this.d3dDevice.CreateBuffer(desc, initdata);
                    pPatchPiece.m_pMyRegularPatchData.SetDebugName("SubDMesh PerPatch");

                    // Create per-patch data buffer for extraordinary patches
                    desc = new D3D11BufferDesc((uint)vExtraordinaryPatchData.Count * PatchData.Size, D3D11BindOptions.ShaderResource);
                    initdata = new(vExtraordinaryPatchData.ToArray(), 0);
                    pPatchPiece.m_pMyExtraordinaryPatchData = this.d3dDevice.CreateBuffer(desc, initdata);
                    pPatchPiece.m_pMyExtraordinaryPatchData.SetDebugName("SubDMesh Xord PerPatch");

                    D3D11ShaderResourceViewDesc SRVDesc;

                    // Create a SRV for the per-patch data
                    SRVDesc = new D3D11ShaderResourceViewDesc(pPatchPiece.m_pPerPatchDataVB, DxgiFormat.R8G8B8A8UInt, 0U, (uint)iNumPatches * 2);
                    pPatchPiece.m_pPerPatchDataSRV = this.d3dDevice.CreateShaderResourceView(pPatchPiece.m_pPerPatchDataVB, SRVDesc);
                    pPatchPiece.m_pPerPatchDataSRV.SetDebugName("SubDMesh PatchVB SRV");

                    // Create SRV for regular per-patch data
                    SRVDesc = new D3D11ShaderResourceViewDesc(pPatchPiece.m_pMyRegularPatchData, DxgiFormat.R8G8B8A8UInt, 0U, (uint)vRegularPatchData.Count * 2);
                    pPatchPiece.m_pMyRegularPatchDataSRV = this.d3dDevice.CreateShaderResourceView(pPatchPiece.m_pPerPatchDataVB, SRVDesc);
                    pPatchPiece.m_pMyRegularPatchDataSRV.SetDebugName("SubDMesh PerPatch SRV");

                    // Create SRV for extraordinary per-patch data
                    SRVDesc = new D3D11ShaderResourceViewDesc(pPatchPiece.m_pMyExtraordinaryPatchData, DxgiFormat.R8G8B8A8UInt, 0U, (uint)vExtraordinaryPatchData.Count * 2);
                    pPatchPiece.m_pMyExtraordinaryPatchDataSRV = this.d3dDevice.CreateShaderResourceView(pPatchPiece.m_pPerPatchDataVB, SRVDesc);
                    pPatchPiece.m_pMyExtraordinaryPatchDataSRV.SetDebugName("SubDMesh Xord PerPatch SRV");

                    pPatchPiece.m_vCenter = pMesh.BoundingBoxCenter;
                    pPatchPiece.m_vExtents = pMesh.BoundingBoxExtents;

                    // Find frame that corresponds to this mesh
                    pPatchPiece.m_iFrameIndex = -1;

                    for (int j = 0; j < FrameCount; j++)
                    {
                        SdkMeshFrame pFrame = this.m_pMeshFile.Frames[j];

                        if (pFrame.MeshIndex == pPatchPiece.m_MeshIndex)
                        {
                            pPatchPiece.m_iFrameIndex = j;
                        }
                    }

                    this.m_PatchPieces.Add(pPatchPiece);
                }
            }

            this.CreateDefaultTextures();

            // Setup constant buffers
            var PerSubsetCBDesc = new D3D11BufferDesc(PerSubsetConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.m_pPerSubsetCB = this.d3dDevice.CreateBuffer(PerSubsetCBDesc);
            this.m_pPerSubsetCB.SetDebugName("SubDMesh PerSubsetConstantBufferData");

            // Create bind pose
            this.m_pMeshFile.TransformBindPose(XMMatrix.Identity);

            this.Update(XMMatrix.Identity, 0.0);
        }

        /// <summary>
        /// This only renders the regular patches of the mesh piece
        /// </summary>
        /// <param name="PieceIndex"></param>
        public void RenderPatchPiece_OnlyRegular(int PieceIndex)
        {
            PatchPiece pPiece = this.m_PatchPieces[PieceIndex];

            // Set the input assembler
            this.d3dDeviceContext.InputAssemblerSetIndexBuffer(pPiece.m_pMyRegularPatchIB, DxgiFormat.R32UInt, 0);
            uint Stride = SubDControlPoint.Size;
            uint Offset = 0;
            this.d3dDeviceContext.InputAssemblerSetVertexBuffers(0, new[] { pPiece.m_pControlPointVB }, new[] { Stride }, new[] { Offset });

            this.d3dDeviceContext.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.PatchList32ControlPoint);

            // Bind the per-patch data
            this.d3dDeviceContext.HullShaderSetShaderResources(0, new[] { pPiece.m_pMyRegularPatchDataSRV });

            // Loop through the mesh subsets
            int SubsetCount = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets.Count;

            for (int i = 0; i < SubsetCount; i++)
            {
                SdkMeshSubset pSubset = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets[i];

                if (pSubset.PrimitiveTopology != D3D11PrimitiveTopology.TriangleList)
                {
                    continue;
                }

                // Set per-subset constant buffer, so the hull shader references the proper index in the per-patch data
                PerSubsetConstantBufferData data = new()
                {
                    m_iPatchStartIndex = pPiece.RegularPatchStart[i]
                };

                this.d3dDeviceContext.UpdateSubresource(this.m_pPerSubsetCB, 0, null, data, 0, 0);

                this.d3dDeviceContext.HullShaderSetConstantBuffers(g_iBindPerSubset, new[] { this.m_pPerSubsetCB });

                // Set up the material for this subset
                SetupMaterial(pSubset.MaterialIndex);

                // Draw      
                uint NumIndices = (uint)pPiece.RegularPatchCount[i] * Constants.MaxExtraordinaryPoints;
                uint StartIndex = (uint)pPiece.RegularPatchStart[i] * Constants.MaxExtraordinaryPoints;
                this.d3dDeviceContext.DrawIndexed(NumIndices, StartIndex, 0);
            }
        }

        /// <summary>
        /// This only renders the extraordinary patches of the mesh piece
        /// </summary>
        /// <param name="PieceIndex"></param>
        public void RenderPatchPiece_OnlyExtraordinary(int PieceIndex)
        {
            PatchPiece pPiece = this.m_PatchPieces[PieceIndex];

            // Set the input assembler
            this.d3dDeviceContext.InputAssemblerSetIndexBuffer(pPiece.m_pMyExtraordinaryPatchIB, DxgiFormat.R32UInt, 0);
            uint Stride = SubDControlPoint.Size;
            uint Offset = 0;
            this.d3dDeviceContext.InputAssemblerSetVertexBuffers(0, new[] { pPiece.m_pControlPointVB }, new[] { Stride }, new[] { Offset });

            this.d3dDeviceContext.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.PatchList32ControlPoint);

            // Bind the per-patch data
            this.d3dDeviceContext.HullShaderSetShaderResources(0, new[] { pPiece.m_pMyExtraordinaryPatchDataSRV });

            // Loop through the mesh subsets
            int SubsetCount = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets.Count;

            for (int i = 0; i < SubsetCount; i++)
            {
                SdkMeshSubset pSubset = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets[i];

                if (pSubset.PrimitiveTopology != D3D11PrimitiveTopology.TriangleList)
                {
                    continue;
                }

                // Set per-subset constant buffer, so the hull shader references the proper index in the per-patch data
                PerSubsetConstantBufferData data = new()
                {
                    m_iPatchStartIndex = pPiece.ExtraordinaryPatchStart[i]
                };

                this.d3dDeviceContext.UpdateSubresource(this.m_pPerSubsetCB, 0, null, data, 0, 0);
                this.d3dDeviceContext.HullShaderSetConstantBuffers(g_iBindPerSubset, new[] { m_pPerSubsetCB });

                // Set up the material for this subset
                SetupMaterial(pSubset.MaterialIndex);

                // Draw       
                uint NumIndices = (uint)pPiece.ExtraordinaryPatchCount[i] * Constants.MaxExtraordinaryPoints;
                uint StartIndex = (uint)pPiece.ExtraordinaryPatchStart[i] * Constants.MaxExtraordinaryPoints;
                this.d3dDeviceContext.DrawIndexed(NumIndices, StartIndex, 0);
            }
        }

        /// <summary>
        /// Renders a single mesh from the SDKMESH data.  Each mesh "piece" is a separate mesh within the file, with its own VB and IB buffers.
        /// </summary>
        /// <param name="PieceIndex"></param>
        public void RenderPolyMeshPiece(int PieceIndex)
        {
            PolyMeshPiece pPiece = this.m_PolyMeshPieces[PieceIndex];

            this.d3dDeviceContext.InputAssemblerSetIndexBuffer(pPiece.m_pIndexBuffer, DxgiFormat.R16UInt, 0);
            uint Stride = SubDControlPoint.Size;
            uint Offset = 0;
            this.d3dDeviceContext.InputAssemblerSetVertexBuffers(0, new[] { pPiece.m_pVertexBuffer }, new[] { Stride }, new[] { Offset });
            this.d3dDeviceContext.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            int SubsetCount = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets.Count;

            for (int i = 0; i < SubsetCount; ++i)
            {
                SdkMeshSubset pSubset = this.m_pMeshFile.Meshes[pPiece.m_MeshIndex].Subsets[i];

                if (pSubset.PrimitiveTopology != D3D11PrimitiveTopology.TriangleList)
                {
                    continue;
                }

                this.SetupMaterial(pSubset.MaterialIndex);

                this.d3dDeviceContext.DrawIndexed((uint)pSubset.IndexCount, (uint)pSubset.IndexStart, 0);
            }
        }
    }
}
