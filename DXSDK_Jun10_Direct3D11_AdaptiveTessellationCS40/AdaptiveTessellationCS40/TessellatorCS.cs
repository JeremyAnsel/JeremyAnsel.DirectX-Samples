// Demos how to use Compute Shader 4.0 to do one simple adaptive tessellation scheme

using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;

namespace AdaptiveTessellationCS40
{
    class TessellatorCS
    {
        private const int MaxFactor = 16;

        private static readonly bool DebugEnabled = false;

        // finalPointPositionTable[i] < insideNumHalfTessFactorPoints, scan of [0], scatter, inverse scatter
        private static readonly int[,,] insidePointIndex = new int[MaxFactor / 2 + 1, MaxFactor / 2 + 2, 4];

        // finalPointPositionTable[i] < outsideNumHalfTessFactorPoints, scan of [0], scatter, inverse scatter
        private static readonly int[,,] outsidePointIndex = new int[MaxFactor / 2 + 1, MaxFactor / 2 + 2, 4];

        private DeviceResources deviceResources;

        private readonly ScanCS s_ScanCS = new ScanCS();

        private D3D11ComputeShader s_pEdgeFactorCS;
        private D3D11ComputeShader s_pScatterVertexTriIDIndexIDCS;
        private D3D11ComputeShader s_pScatterIndexTriIDIndexIDCS;
        private readonly D3D11ComputeShader[] s_pNumVerticesIndicesCSs = new D3D11ComputeShader[4];
        private readonly D3D11ComputeShader[] s_pTessVerticesCSs = new D3D11ComputeShader[4];
        private readonly D3D11ComputeShader[] s_pTessIndicesCSs = new D3D11ComputeShader[4];
        private D3D11Buffer s_pEdgeFactorCSCB;
        private D3D11Buffer s_pLookupTableCSCB;
        private D3D11Buffer s_pCSCB;
        private D3D11Buffer s_pCSReadBackBuf;

        private D3D11Buffer m_pEdgeFactorBuf;
        private D3D11ShaderResourceView m_pEdgeFactorBufSRV;
        private D3D11UnorderedAccessView m_pEdgeFactorBufUAV;
        private D3D11Buffer m_pScanBuf0;
        private D3D11Buffer m_pScanBuf1;
        private D3D11ShaderResourceView m_pScanBuf0SRV;
        private D3D11ShaderResourceView m_pScanBuf1SRV;
        private D3D11UnorderedAccessView m_pScanBuf0UAV;
        private D3D11UnorderedAccessView m_pScanBuf1UAV;
        private D3D11Buffer m_pScatterVertexBuf;
        private D3D11Buffer m_pScatterIndexBuf;
        private D3D11ShaderResourceView m_pScatterVertexBufSRV;
        private D3D11ShaderResourceView m_pScatterIndexBufSRV;
        private D3D11UnorderedAccessView m_pScatterVertexBufUAV;
        private D3D11UnorderedAccessView m_pScatterIndexBufUAV;
        private D3D11ShaderResourceView m_pTessedVerticesBufSRV;
        private D3D11UnorderedAccessView m_pTessedVerticesBufUAV;
        private D3D11UnorderedAccessView m_pTessedIndicesBufUAV;
        private D3D11ShaderResourceView m_pBaseVBSRV;

        private XMFloat2 m_tess_edge_len_scale;

        private uint m_nCachedTessedVertices;
        private uint m_nCachedTessedIndices;

        public uint m_nVertices;

        static TessellatorCS()
        {
            InitLookupTables();
        }

        public D3D11ShaderResourceView TessedVerticesBufSRV => this.m_pTessedVerticesBufSRV;

        public D3D11ShaderResourceView BaseVBSRV => this.m_pBaseVBSRV;

        public PartitioningMode PartitioningMode { get; set; } = PartitioningMode.FractionalEven;

        private static void InitLookupTables()
        {
            int[] finalPointPositionTable = new int[MaxFactor / 2 + 1];
            finalPointPositionTable[0] = 0;
            finalPointPositionTable[1] = MaxFactor / 2;

            for (int i = 2; i < MaxFactor / 2 + 1; i++)
            {
                int level = 0;

                while (true)
                {
                    if ((((i - 2) - ((1 << level) - 1)) & ((1 << (level + 1)) - 1)) == 0)
                    {
                        break;
                    }

                    level++;
                }

                finalPointPositionTable[i] = ((MaxFactor >> 1) + ((i - 2) - ((1 << level) - 1))) >> (level + 1);
            }

            for (int h = 0; h <= MaxFactor / 2; h++)
            {
                for (int i = 0; i <= MaxFactor / 2; i++)
                {
                    if (i == 0)
                    {
                        insidePointIndex[h, i, 0] = 0;
                    }
                    else
                    {
                        insidePointIndex[h, i, 0] = finalPointPositionTable[i] < h ? 1 : 0;
                    }
                }

                insidePointIndex[h, MaxFactor / 2 + 1, 0] = 0;

                for (int i = 0; i <= MaxFactor / 2 + 1; i++)
                {
                    if (i == 0)
                    {
                        insidePointIndex[h, i, 1] = 0;
                    }
                    else
                    {
                        insidePointIndex[h, i, 1] = insidePointIndex[h, i - 1, 0] + insidePointIndex[h, i - 1, 1];
                    }

                    if (insidePointIndex[h, i, 0] != 0)
                    {
                        insidePointIndex[h, insidePointIndex[h, i, 1], 2] = i;
                    }
                }

                for (int i = MaxFactor / 2; i >= 0; i--)
                {
                    if (insidePointIndex[h, i, 0] != 0)
                    {
                        insidePointIndex[h, insidePointIndex[h, MaxFactor / 2 + 1, 1] - insidePointIndex[h, i + 1, 1], 3] = i;
                    }
                }
            }

            for (int h = 0; h <= MaxFactor / 2; h++)
            {
                for (int i = 0; i <= MaxFactor / 2; i++)
                {
                    outsidePointIndex[h, i, 0] = finalPointPositionTable[i] < h ? 1 : 0;
                }

                outsidePointIndex[h, MaxFactor / 2 + 1, 0] = 0;

                for (int i = 0; i <= MaxFactor / 2 + 1; i++)
                {
                    if (i == 0)
                    {
                        outsidePointIndex[h, i, 1] = 0;
                    }
                    else
                    {
                        outsidePointIndex[h, i, 1] = outsidePointIndex[h, i - 1, 0] + outsidePointIndex[h, i - 1, 1];
                    }

                    if (outsidePointIndex[h, i, 0] != 0)
                    {
                        outsidePointIndex[h, outsidePointIndex[h, i, 1], 2] = i;
                    }
                }

                for (int i = MaxFactor / 2; i >= 0; i--)
                {
                    if (outsidePointIndex[h, i, 0] != 0)
                    {
                        outsidePointIndex[h, outsidePointIndex[h, MaxFactor / 2 + 1, 1] - outsidePointIndex[h, i + 1, 1], 3] = i;
                    }
                }
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            this.s_ScanCS.CreateDeviceDependentResources(resources);

            this.PartitioningMode = PartitioningMode.FractionalEven;
            this.CreateCSs();
            this.CreateCSForPartitioningModes();
            this.CreateCSBuffers();
        }

        private void CreateCSs()
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            this.s_pEdgeFactorCS = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_EdgeFactorCS.cso"), null);
            this.s_pScatterVertexTriIDIndexIDCS = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_ScatterVertexTriIDCS.cso"), null);
            this.s_pScatterIndexTriIDIndexIDCS = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_ScatterIndexTriIDCS.cso"), null);
        }

        private void CreateCSForPartitioningModes()
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            this.s_pNumVerticesIndicesCSs[(int)PartitioningMode.Integer] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_NumVerticesIndicesCSInteger.cso"), null);
            this.s_pTessVerticesCSs[(int)PartitioningMode.Integer] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateVerticesCSInteger.cso"), null);
            this.s_pTessIndicesCSs[(int)PartitioningMode.Integer] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateIndicesCSInteger.cso"), null);

            this.s_pNumVerticesIndicesCSs[(int)PartitioningMode.Pow2] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_NumVerticesIndicesCSPow2.cso"), null);
            this.s_pTessVerticesCSs[(int)PartitioningMode.Pow2] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateVerticesCSPow2.cso"), null);
            this.s_pTessIndicesCSs[(int)PartitioningMode.Pow2] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateIndicesCSPow2.cso"), null);

            this.s_pNumVerticesIndicesCSs[(int)PartitioningMode.FractionalOdd] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_NumVerticesIndicesCSFracOdd.cso"), null);
            this.s_pTessVerticesCSs[(int)PartitioningMode.FractionalOdd] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateVerticesCSFracOdd.cso"), null);
            this.s_pTessIndicesCSs[(int)PartitioningMode.FractionalOdd] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateIndicesCSFracOdd.cso"), null);

            this.s_pNumVerticesIndicesCSs[(int)PartitioningMode.FractionalEven] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_NumVerticesIndicesCSFracEven.cso"), null);
            this.s_pTessVerticesCSs[(int)PartitioningMode.FractionalEven] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateVerticesCSFracEven.cso"), null);
            this.s_pTessIndicesCSs[(int)PartitioningMode.FractionalEven] = d3dDevice.CreateComputeShader(File.ReadAllBytes("TessellatorCS40_TessellateIndicesCSFracEven.cso"), null);
        }

        private void CreateCSBuffers()
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            // constant buffers used to pass parameters to CS
            int[] lut = new int[insidePointIndex.Length + outsidePointIndex.Length];
            Buffer.BlockCopy(insidePointIndex, 0, lut, 0, insidePointIndex.Length * sizeof(int));
            Buffer.BlockCopy(outsidePointIndex, 0, lut, insidePointIndex.Length * sizeof(int), outsidePointIndex.Length * sizeof(int));

            this.s_pLookupTableCSCB = d3dDevice.CreateBuffer(
                D3D11BufferDesc.From(lut, D3D11BindOptions.ConstantBuffer, D3D11Usage.Immutable),
                lut,
                (uint)lut.Length * sizeof(int),
                0);

            this.s_pEdgeFactorCSCB = d3dDevice.CreateBuffer(
                new D3D11BufferDesc(EdgeFactorConstantBuffer.Size, D3D11BindOptions.ConstantBuffer, D3D11Usage.Default));

            this.s_pCSCB = d3dDevice.CreateBuffer(
                new D3D11BufferDesc(sizeof(int) * 4, D3D11BindOptions.ConstantBuffer, D3D11Usage.Default));

            // read back buffer
            this.s_pCSReadBackBuf = d3dDevice.CreateBuffer(
                new D3D11BufferDesc(sizeof(int) * 2, D3D11BindOptions.None, D3D11Usage.Staging, D3D11CpuAccessOptions.Read));
        }

        public void ReleaseDeviceDependentResources()
        {
            this.s_ScanCS.ReleaseDeviceDependentResources();

            D3D11Utils.DisposeAndNull(ref this.s_pEdgeFactorCS);
            D3D11Utils.DisposeAndNull(ref this.s_pScatterVertexTriIDIndexIDCS);
            D3D11Utils.DisposeAndNull(ref this.s_pScatterIndexTriIDIndexIDCS);
            D3D11Utils.DisposeAndNull(ref this.s_pNumVerticesIndicesCSs[0]);
            D3D11Utils.DisposeAndNull(ref this.s_pNumVerticesIndicesCSs[1]);
            D3D11Utils.DisposeAndNull(ref this.s_pNumVerticesIndicesCSs[2]);
            D3D11Utils.DisposeAndNull(ref this.s_pNumVerticesIndicesCSs[3]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessVerticesCSs[0]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessVerticesCSs[1]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessVerticesCSs[2]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessVerticesCSs[3]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessIndicesCSs[0]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessIndicesCSs[1]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessIndicesCSs[2]);
            D3D11Utils.DisposeAndNull(ref this.s_pTessIndicesCSs[3]);
            D3D11Utils.DisposeAndNull(ref this.s_pEdgeFactorCSCB);
            D3D11Utils.DisposeAndNull(ref this.s_pLookupTableCSCB);
            D3D11Utils.DisposeAndNull(ref this.s_pCSCB);
            D3D11Utils.DisposeAndNull(ref this.s_pCSReadBackBuf);

            D3D11Utils.DisposeAndNull(ref this.m_pEdgeFactorBuf);
            D3D11Utils.DisposeAndNull(ref this.m_pEdgeFactorBufSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pEdgeFactorBufUAV);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf0);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf1);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf0SRV);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf1SRV);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf0UAV);
            D3D11Utils.DisposeAndNull(ref this.m_pScanBuf1UAV);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBuf);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBuf);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBufSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBufSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBufUAV);
            D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBufUAV);
            D3D11Utils.DisposeAndNull(ref this.m_pTessedVerticesBufSRV);
            D3D11Utils.DisposeAndNull(ref this.m_pTessedVerticesBufUAV);
            D3D11Utils.DisposeAndNull(ref this.m_pTessedIndicesBufUAV);
            D3D11Utils.DisposeAndNull(ref this.m_pBaseVBSRV);
        }

        public void CreateWindowSizeDependentResources()
        {
            const float adaptive_scale_in_pixels = 15.0f;

            this.m_tess_edge_len_scale = new XMFloat2(
                this.deviceResources.BackBufferWidth * 0.5f / adaptive_scale_in_pixels,
                this.deviceResources.BackBufferHeight * 0.5f / adaptive_scale_in_pixels);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void SetBaseMesh(uint nVertices, D3D11Buffer pBaseVB)
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            this.m_nVertices = nVertices;

            // shader resource view of base mesh vertex data 
            this.m_pBaseVBSRV = d3dDevice.CreateShaderResourceView(pBaseVB, new D3D11ShaderResourceViewDesc
            {
                ViewDimension = D3D11SrvDimension.Buffer,
                Format = DxgiFormat.R32G32B32A32Float,
                Buffer = new D3D11BufferSrv
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices
                }
            });

            // Buffer for edge tessellation factor
            this.m_pEdgeFactorBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                ByteWidth = 4 * 4 * this.m_nVertices / 3,
                StructureByteStride = 4 * 4,
                MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                Usage = D3D11Usage.Default
            });

            // shader resource view of the buffer above
            this.m_pEdgeFactorBufSRV = d3dDevice.CreateShaderResourceView(this.m_pEdgeFactorBuf, new D3D11ShaderResourceViewDesc
            {
                ViewDimension = D3D11SrvDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferSrv
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });

            // UAV of the buffer above
            this.m_pEdgeFactorBufUAV = d3dDevice.CreateUnorderedAccessView(this.m_pEdgeFactorBuf, new D3D11UnorderedAccessViewDesc
            {
                ViewDimension = D3D11UavDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferUav
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });

            // Buffers for scan
            this.m_pScanBuf0 = d3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                ByteWidth = 4 * 2 * this.m_nVertices / 3,
                StructureByteStride = 4 * 2,
                MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                Usage = D3D11Usage.Default
            });

            this.m_pScanBuf1 = d3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                ByteWidth = 4 * 2 * this.m_nVertices / 3,
                StructureByteStride = 4 * 2,
                MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                Usage = D3D11Usage.Default
            });

            // shader resource views of the scan buffers
            this.m_pScanBuf0SRV = d3dDevice.CreateShaderResourceView(this.m_pScanBuf0, new D3D11ShaderResourceViewDesc
            {
                ViewDimension = D3D11SrvDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferSrv
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });

            this.m_pScanBuf1SRV = d3dDevice.CreateShaderResourceView(this.m_pScanBuf1, new D3D11ShaderResourceViewDesc
            {
                ViewDimension = D3D11SrvDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferSrv
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });

            // UAV of the scan buffers
            this.m_pScanBuf0UAV = d3dDevice.CreateUnorderedAccessView(this.m_pScanBuf0, new D3D11UnorderedAccessViewDesc
            {
                ViewDimension = D3D11UavDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferUav
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });

            this.m_pScanBuf1UAV = d3dDevice.CreateUnorderedAccessView(this.m_pScanBuf1, new D3D11UnorderedAccessViewDesc
            {
                ViewDimension = D3D11UavDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferUav
                {
                    FirstElement = 0,
                    NumElements = this.m_nVertices / 3
                }
            });
        }

        private T[] CreateAndCopyToDebugBuf<T>(D3D11Buffer buffer) where T : struct
        {
            var d3dDevice = this.deviceResources.D3DDevice;
            var d3dContext = this.deviceResources.D3DContext;

            var desc = buffer.Description;
            desc.CpuAccessOptions = D3D11CpuAccessOptions.Read;
            desc.Usage = D3D11Usage.Staging;
            desc.BindOptions = D3D11BindOptions.None;
            desc.MiscOptions = D3D11ResourceMiscOptions.None;

            //int sizeT = Marshal.SizeOf<T>();
            int sizeT = Unsafe.SizeOf<T>();
            int dataLength = (int)desc.ByteWidth / sizeT;
            var data = new T[dataLength];

            using (var debugbuf = d3dDevice.CreateBuffer(desc))
            {
                d3dContext.CopyResource(debugbuf, buffer);

                D3D11MappedSubResource mappedResource = d3dContext.Map(debugbuf, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                try
                {
                    //for (int i = 0; i < dataLength; i++)
                    //{
                    //    data[i] = Marshal.PtrToStructure<T>(mappedResource.Data + i * sizeT);
                    //}

                    var b = new byte[desc.ByteWidth];
                    Marshal.Copy(mappedResource.Data, b, 0, b.Length);

                    IntPtr d = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
                    Marshal.Copy(b, 0, d, b.Length);
                }
                finally
                {
                    d3dContext.Unmap(debugbuf, 0);
                }
            }

            return data;
        }

        private void RunComputeShader(
            D3D11ComputeShader pComputeShader,
            D3D11ShaderResourceView[] pShaderResourceViews,
            D3D11Buffer pNeverChangesCBCS,
            D3D11Buffer pCBCS,
            //T pCSData,
            //uint dwNumDataBytes,
            D3D11UnorderedAccessView pUnorderedAccessView,
            uint x,
            uint y,
            uint z)
        //where T : struct
        {
            var d3dContext = this.deviceResources.D3DContext;

            d3dContext.ComputeShaderSetShader(pComputeShader, null);
            d3dContext.ComputeShaderSetShaderResources(0, pShaderResourceViews);
            d3dContext.ComputeShaderSetUnorderedAccessViews(0, new[] { pUnorderedAccessView }, new[] { 0U });

            //if (pCBCS != null)
            //{
            //    d3dContext.UpdateSubresource(pCBCS, D3D11Utils.CalcSubresource(0, 0, 1), null, pCSData, dwNumDataBytes, dwNumDataBytes);
            //}

            if (pNeverChangesCBCS != null && pCBCS != null)
            {
                d3dContext.ComputeShaderSetConstantBuffers(0, new[] { pNeverChangesCBCS, pCBCS });
            }

            if (pNeverChangesCBCS != null && pCBCS == null)
            {
                d3dContext.ComputeShaderSetConstantBuffers(0, new[] { pNeverChangesCBCS });
            }

            if (pNeverChangesCBCS == null && pCBCS != null)
            {
                d3dContext.ComputeShaderSetConstantBuffers(0, new[] { pCBCS });
            }

            d3dContext.Dispatch(x, y, z);

            d3dContext.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0U });
            d3dContext.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null, null });
            d3dContext.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { });
        }

        public void PerEdgeTessellation(
            XMMatrix matWVP,
            ref D3D11Buffer ppTessedVerticesBuf,
            ref D3D11Buffer ppTessedIndicesBuf,
            out uint num_tessed_vertices,
            out uint num_tessed_indices)
        {
            var d3dDevice = this.deviceResources.D3DDevice;
            var d3dContext = this.deviceResources.D3DContext;

            // Update per-edge tessellation factors
            {
                EdgeFactorConstantBuffer cbCS = default;
                cbCS.MatWVP = matWVP;
                cbCS.TessEdgeLengthScale = this.m_tess_edge_len_scale;
                cbCS.NumTriangles = this.m_nVertices / 3;

                d3dContext.UpdateSubresource(
                    this.s_pEdgeFactorCSCB,
                    D3D11Utils.CalcSubresource(0, 0, 1),
                    null,
                    cbCS,
                    EdgeFactorConstantBuffer.Size,
                    EdgeFactorConstantBuffer.Size);

                this.RunComputeShader(
                    this.s_pEdgeFactorCS,
                    new[] { this.m_pBaseVBSRV },
                    null,
                    this.s_pEdgeFactorCSCB,
                    this.m_pEdgeFactorBufUAV,
                    (uint)Math.Ceiling(this.m_nVertices / 3 / 128.0f),
                    1U,
                    1U);
            }

            // How many vertices/indices are needed for the tessellated mesh?
            {
                uint[] cbCS = new uint[] { this.m_nVertices / 3, 0, 0, 0 };

                d3dContext.UpdateSubresource(
                    this.s_pCSCB,
                    D3D11Utils.CalcSubresource(0, 0, 1),
                    null,
                    cbCS,
                    4 * 4,
                    4 * 4);

                this.RunComputeShader(
                    this.s_pNumVerticesIndicesCSs[(int)this.PartitioningMode],
                    new[] { this.m_pEdgeFactorBufSRV },
                    this.s_pLookupTableCSCB,
                    this.s_pCSCB,
                    this.m_pScanBuf0UAV,
                    (uint)Math.Ceiling(this.m_nVertices / 3 / 128.0f),
                    1U,
                    1U);

                this.s_ScanCS.Scan(d3dContext, this.m_nVertices / 3, this.m_pScanBuf0SRV, this.m_pScanBuf0UAV, this.m_pScanBuf1SRV, this.m_pScanBuf1UAV);

                // read back the number of vertices/indices for tessellation output
                D3D11Box box = default;
                box.Left = 4 * 2 * this.m_nVertices / 3 - 4 * 2;
                box.Right = 4 * 2 * this.m_nVertices / 3;
                box.Top = 0;
                box.Bottom = 1;
                box.Front = 0;
                box.Back = 1;

                d3dContext.CopySubresourceRegion(this.s_pCSReadBackBuf, 0, 0, 0, 0, this.m_pScanBuf0, 0, box);

                D3D11MappedSubResource mappedResource = d3dContext.Map(this.s_pCSReadBackBuf, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                try
                {
                    num_tessed_vertices = (uint)Marshal.ReadInt32(mappedResource.Data + 4 * 0);
                    num_tessed_indices = (uint)Marshal.ReadInt32(mappedResource.Data + 4 * 1);
                }
                finally
                {
                    d3dContext.Unmap(this.s_pCSReadBackBuf, 0);
                }
            }

            if (num_tessed_vertices == 0 || num_tessed_indices == 0)
            {
                return;
            }

            // Turn on this and set a breakpoint on the line beginning with "p = " and see what has been written to m_pScanBuf0
            if (DebugEnabled)
            {
#pragma warning disable IDE0059 // Assignation inutile d'une valeur
                var p = this.CreateAndCopyToDebugBuf<(uint v, uint t)>(this.m_pScanBuf0);
#pragma warning restore IDE0059 // Assignation inutile d'une valeur
            }

            // Generate buffers for scattering TriID and IndexID for both vertex data and index data,
            // also generate buffers for output tessellated vertex data and index data
            {
                if (this.m_pScatterVertexBuf == null || this.m_nCachedTessedVertices < num_tessed_vertices || this.m_nCachedTessedVertices > num_tessed_vertices * 2)
                {
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBuf);
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBufSRV);
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterVertexBufUAV);

                    D3D11Utils.DisposeAndNull(ref ppTessedVerticesBuf);
                    D3D11Utils.DisposeAndNull(ref this.m_pTessedVerticesBufUAV);
                    D3D11Utils.DisposeAndNull(ref this.m_pTessedVerticesBufSRV);

                    this.m_pScatterVertexBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
                    {
                        BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                        ByteWidth = 4 * 2 * num_tessed_vertices,
                        StructureByteStride = 4 * 2,
                        MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                        Usage = D3D11Usage.Default
                    });

                    this.m_pScatterVertexBufSRV = d3dDevice.CreateShaderResourceView(this.m_pScatterVertexBuf, new D3D11ShaderResourceViewDesc
                    {
                        ViewDimension = D3D11SrvDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferSrv
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_vertices
                        }
                    });

                    this.m_pScatterVertexBufUAV = d3dDevice.CreateUnorderedAccessView(this.m_pScatterVertexBuf, new D3D11UnorderedAccessViewDesc
                    {
                        ViewDimension = D3D11UavDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferUav
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_vertices
                        }
                    });

                    // generate the output tessellated vertices buffer
                    ppTessedVerticesBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
                    {
                        BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                        ByteWidth = 4 * 3 * num_tessed_vertices,
                        StructureByteStride = 4 * 3,
                        MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                        Usage = D3D11Usage.Default
                    });

                    this.m_pTessedVerticesBufUAV = d3dDevice.CreateUnorderedAccessView(ppTessedVerticesBuf, new D3D11UnorderedAccessViewDesc
                    {
                        ViewDimension = D3D11UavDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferUav
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_vertices
                        }
                    });

                    this.m_pTessedVerticesBufSRV = d3dDevice.CreateShaderResourceView(ppTessedVerticesBuf, new D3D11ShaderResourceViewDesc
                    {
                        ViewDimension = D3D11SrvDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferSrv
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_vertices
                        }
                    });

                    this.m_nCachedTessedVertices = num_tessed_vertices;
                }

                if (this.m_pScatterIndexBuf == null || this.m_nCachedTessedIndices < num_tessed_indices)
                {
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBuf);
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBufSRV);
                    D3D11Utils.DisposeAndNull(ref this.m_pScatterIndexBufUAV);

                    D3D11Utils.DisposeAndNull(ref ppTessedIndicesBuf);
                    D3D11Utils.DisposeAndNull(ref this.m_pTessedIndicesBufUAV);

                    this.m_pScatterIndexBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
                    {
                        BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                        ByteWidth = 4 * 2 * num_tessed_indices,
                        StructureByteStride = 4 * 2,
                        MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                        Usage = D3D11Usage.Default
                    });

                    this.m_pScatterIndexBufSRV = d3dDevice.CreateShaderResourceView(this.m_pScatterIndexBuf, new D3D11ShaderResourceViewDesc
                    {
                        ViewDimension = D3D11SrvDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferSrv
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_indices
                        }
                    });

                    this.m_pScatterIndexBufUAV = d3dDevice.CreateUnorderedAccessView(this.m_pScatterIndexBuf, new D3D11UnorderedAccessViewDesc
                    {
                        ViewDimension = D3D11UavDimension.Buffer,
                        Format = DxgiFormat.Unknown,
                        Buffer = new D3D11BufferUav
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_indices
                        }
                    });

                    // generate the output tessellated indices buffer
                    ppTessedIndicesBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
                    {
                        BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess | D3D11BindOptions.IndexBuffer,
                        ByteWidth = 4 * num_tessed_indices,
                        MiscOptions = D3D11ResourceMiscOptions.BufferAllowRawViews,
                        Usage = D3D11Usage.Default
                    });

                    this.m_pTessedIndicesBufUAV = d3dDevice.CreateUnorderedAccessView(ppTessedIndicesBuf, new D3D11UnorderedAccessViewDesc
                    {
                        ViewDimension = D3D11UavDimension.Buffer,
                        Format = DxgiFormat.R32Typeless,
                        Buffer = new D3D11BufferUav
                        {
                            FirstElement = 0,
                            NumElements = num_tessed_indices,
                            Options = D3D11BufferUavOptions.Raw
                        }
                    });

                    this.m_nCachedTessedIndices = num_tessed_indices;
                }
            }

            // Scatter TriID, IndexID
            {
                uint[] cbCS = new uint[] { this.m_nVertices / 3, 0, 0, 0 };
                D3D11ShaderResourceView[] aRViews = new[] { this.m_pScanBuf0SRV };

                d3dContext.UpdateSubresource(
                    this.s_pCSCB,
                    D3D11Utils.CalcSubresource(0, 0, 1),
                    null,
                    cbCS,
                    4 * 4,
                    4 * 4);

                // Scatter vertex TriID, IndexID
                this.RunComputeShader(
                    this.s_pScatterVertexTriIDIndexIDCS,
                    aRViews,
                    null,
                    this.s_pCSCB,
                    this.m_pScatterVertexBufUAV,
                    (uint)Math.Ceiling(this.m_nVertices / 3 / 128.0f),
                    1U,
                    1U);

                // Scatter index TriID, IndexID
                this.RunComputeShader(
                    this.s_pScatterIndexTriIDIndexIDCS,
                    aRViews,
                    null,
                    this.s_pCSCB,
                    this.m_pScatterIndexBufUAV,
                    (uint)Math.Ceiling(this.m_nVertices / 3 / 128.0f),
                    1U,
                    1U);
            }

            // Turn on this and set a breakpoint on the line beginning with "p = " and see what has been written to m_pScatterVertexBuf
            if (DebugEnabled)
            {
#pragma warning disable IDE0059 // Assignation inutile d'une valeur
                var p = this.CreateAndCopyToDebugBuf<(uint v, uint t)>(this.m_pScatterVertexBuf);
#pragma warning restore IDE0059 // Assignation inutile d'une valeur
            }

            // Tessellate vertex
            {
                uint[] cbCS = new uint[] { num_tessed_vertices, 0, 0, 0 };

                d3dContext.UpdateSubresource(
                    this.s_pCSCB,
                    D3D11Utils.CalcSubresource(0, 0, 1),
                    null,
                    cbCS,
                    4 * 4,
                    4 * 4);

                this.RunComputeShader(
                    this.s_pTessVerticesCSs[(int)this.PartitioningMode],
                    new[] { this.m_pScatterVertexBufSRV, this.m_pEdgeFactorBufSRV },
                    this.s_pLookupTableCSCB,
                    this.s_pCSCB,
                    this.m_pTessedVerticesBufUAV,
                    (uint)Math.Ceiling(num_tessed_vertices / 128.0f),
                    1U,
                    1U);
            }

            // Turn on this and set a breakpoint on the line beginning with "p = " and see what has been written to *ppTessedVerticesBuf
            if (DebugEnabled)
            {
#pragma warning disable IDE0059 // Assignation inutile d'une valeur
                var p = this.CreateAndCopyToDebugBuf<(uint id, float u, float v)>(ppTessedVerticesBuf);
#pragma warning restore IDE0059 // Assignation inutile d'une valeur
            }

            // Tessellate indices
            {
                uint[] cbCS = new uint[] { num_tessed_indices, 0, 0, 0 };

                d3dContext.UpdateSubresource(
                    this.s_pCSCB,
                    D3D11Utils.CalcSubresource(0, 0, 1),
                    null,
                    cbCS,
                    4 * 4,
                    4 * 4);

                this.RunComputeShader(
                    this.s_pTessIndicesCSs[(int)this.PartitioningMode],
                    new[] { this.m_pScatterIndexBufSRV, this.m_pEdgeFactorBufSRV, this.m_pScanBuf0SRV },
                    this.s_pLookupTableCSCB,
                    this.s_pCSCB,
                    this.m_pTessedIndicesBufUAV,
                    (uint)Math.Ceiling(num_tessed_indices / 128.0f),
                    1U,
                    1U);
            }

            // Turn on this and set a breakpoint on the line beginning with "p = " and see what has been written to *ppTessedIndicesBuf
            if (DebugEnabled)
            {
#pragma warning disable IDE0059 // Assignation inutile d'une valeur
                var p = this.CreateAndCopyToDebugBuf<int>(ppTessedIndicesBuf);
#pragma warning restore IDE0059 // Assignation inutile d'une valeur
            }
        }
    }
}
