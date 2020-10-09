// A simple inclusive prefix sum(scan) implemented in CS4.0
// 
// Note, to maintain the simplicity of the sample, this scan has these limitations:
//      - At maximum 16384 elements can be scanned.
//      - The element to be scanned is of type uint2, see comments in Scan.hlsl 
//        and below for how to change this type

using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AdaptiveTessellationCS40
{
    class ScanCS
    {
        private D3D11ComputeShader m_pScanCS;
        private D3D11ComputeShader m_pScan2CS;
        private D3D11ComputeShader m_pScan3CS;

        private D3D11Buffer m_pAuxBuf;
        private D3D11ShaderResourceView m_pAuxBufRV;
        private D3D11UnorderedAccessView m_pAuxBufUAV;

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            var d3dDevice = resources.D3DDevice;

            this.m_pScanCS = d3dDevice.CreateComputeShader(File.ReadAllBytes("ScanInBucketComputeShader.cso"), null);
            this.m_pScan2CS = d3dDevice.CreateComputeShader(File.ReadAllBytes("ScanBucketResultComputeShader.cso"), null);
            this.m_pScan3CS = d3dDevice.CreateComputeShader(File.ReadAllBytes("ScanAddBucketResultComputeShader.cso"), null);

            this.m_pAuxBuf = d3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.UnorderedAccess,
                StructureByteStride = 4 * 2, // If scan types other than uint2, remember change here
                ByteWidth = 4 * 2 * 128,
                MiscOptions = D3D11ResourceMiscOptions.BufferStructured,
                Usage = D3D11Usage.Default
            });

            this.m_pAuxBufRV = d3dDevice.CreateShaderResourceView(this.m_pAuxBuf, new D3D11ShaderResourceViewDesc
            {
                ViewDimension = D3D11SrvDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferSrv
                {
                    FirstElement = 0,
                    NumElements = 128
                }
            });

            this.m_pAuxBufUAV = d3dDevice.CreateUnorderedAccessView(this.m_pAuxBuf, new D3D11UnorderedAccessViewDesc
            {
                ViewDimension = D3D11UavDimension.Buffer,
                Format = DxgiFormat.Unknown,
                Buffer = new D3D11BufferUav
                {
                    FirstElement = 0,
                    NumElements = 128
                }
            });
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.m_pAuxBuf);
            D3D11Utils.DisposeAndNull(ref this.m_pAuxBufRV);
            D3D11Utils.DisposeAndNull(ref this.m_pAuxBufUAV);

            D3D11Utils.DisposeAndNull(ref this.m_pScanCS);
            D3D11Utils.DisposeAndNull(ref this.m_pScan2CS);
            D3D11Utils.DisposeAndNull(ref this.m_pScan3CS);
        }

        // Both scan input and scanned output are in the buffer resource referred by p0SRV and p0UAV.
        // The buffer resource referred by p1SRV and p1UAV is used as intermediate result, 
        // and should be as large as the input/output buffer
        public void Scan(
            D3D11DeviceContext d3dImmediateContext,
            // How many elements in the input buffer are to be scanned?
            uint numToScan,
            // SRV and UAV of the buffer which contains the input data,
            // and the scanned result when the function returns
            D3D11ShaderResourceView p0SRV,
            D3D11UnorderedAccessView p0UAV,
            // SRV and UAV of an aux buffer, which must be the same size as the input/output buffer
            D3D11ShaderResourceView p1SRV,
            D3D11UnorderedAccessView p1UAV)
        {
            // first pass, scan in each bucket
            {
                d3dImmediateContext.ComputeShaderSetShader(this.m_pScanCS, null);
                d3dImmediateContext.ComputeShaderSetShaderResources(0, new[] { p0SRV });
                d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new[] { p1UAV }, new[] { 0u });
                d3dImmediateContext.Dispatch((uint)Math.Ceiling(numToScan / 128.0f), 1, 1);
                d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0u });
            }

            // second pass, record and scan the sum of each bucket
            {
                d3dImmediateContext.ComputeShaderSetShader(this.m_pScan2CS, null);
                d3dImmediateContext.ComputeShaderSetShaderResources(0, new[] { p1SRV });
                d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new[] { this.m_pAuxBufUAV }, new[] { 0u });
                d3dImmediateContext.Dispatch(1, 1, 1);
                d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0u });
            }

            // last pass, add the buckets scanned result to each bucket to get the final result
            {
                d3dImmediateContext.ComputeShaderSetShader(this.m_pScan3CS, null);
                d3dImmediateContext.ComputeShaderSetShaderResources(0, new[] { p1SRV, this.m_pAuxBufRV });
                d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new[] { p0UAV }, new[] { 0u });
                d3dImmediateContext.Dispatch((uint)Math.Ceiling(numToScan / 128.0f), 1, 1);
            }

            // Unbind resources for CS
            d3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0u });
            d3dImmediateContext.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null });
            d3dImmediateContext.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { });
        }
    }
}
