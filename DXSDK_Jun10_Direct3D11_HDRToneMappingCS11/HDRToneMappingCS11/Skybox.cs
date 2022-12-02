using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Encapsulation of skybox geometry and textures
    /// </summary>
    class Skybox
    {
        private DeviceResources deviceResources;

        private D3D11Resource m_pEnvironmentMap;
        private D3D11ShaderResourceView m_pEnvironmentRV;
        private D3D11VertexShader m_pVertexShader;
        private D3D11PixelShader m_pPixelShader;
        private D3D11SamplerState m_pSam;
        private D3D11InputLayout m_pVertexLayout;
        private D3D11Buffer m_pcbVSPerObject;
        private D3D11Buffer m_pVB;
        private D3D11DepthStencilState m_pDepthStencilState;

        private float m_fSize = 1.0f;

        public void CreateDeviceDependentResources(
            DeviceResources resources,
            float fSize,
            D3D11Resource pCubeTexture,
            D3D11ShaderResourceView pCubeRV)
        {
            this.deviceResources = resources;

            var device = deviceResources.D3DDevice;

            this.m_fSize = fSize;
            this.m_pEnvironmentMap = pCubeTexture;
            this.m_pEnvironmentRV = pCubeRV;

            // Create the shaders
            this.m_pVertexShader = device.CreateVertexShader(File.ReadAllBytes("SkyboxVS.cso"), null);
            this.m_pVertexShader.SetDebugName("SkyboxVS");
            this.m_pPixelShader = device.CreatePixelShader(File.ReadAllBytes("SkyboxPS.cso"), null);
            this.m_pPixelShader.SetDebugName("SkyboxPS");

            // Create an input layout

            D3D11InputElementDesc[] vertexLayoutDesc = new[]
            {
                new D3D11InputElementDesc("POSITION",  0, DxgiFormat.R32G32B32A32Float, 0, 0, D3D11InputClassification.PerVertexData, 0 ),
            };

            this.m_pVertexLayout = device.CreateInputLayout(vertexLayoutDesc, File.ReadAllBytes("SkyboxVS.cso"));
            this.m_pVertexLayout.SetDebugName("Primary");

            // Setup linear or point sampler according to the format Query result
            D3D11SamplerDesc SamDesc = new(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                1,
                D3D11ComparisonFunction.Always,
                null,
                0.0f,
                float.MaxValue);

            this.m_pSam = device.CreateSamplerState(SamDesc);
            this.m_pSam.SetDebugName("Primary");

            // Setup constant buffer
            D3D11BufferDesc Desc = new(SkyboxPerObjectConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.m_pcbVSPerObject = device.CreateBuffer(Desc);
            this.m_pcbVSPerObject.SetDebugName("CB_VS_PER_OBJECT");

            // Depth stencil state
            D3D11DepthStencilDesc DSDesc = new()
            {
                IsDepthEnabled = false,
                DepthWriteMask = D3D11DepthWriteMask.All,
                DepthFunction = D3D11ComparisonFunction.Less,
                IsStencilEnabled = false
            };

            this.m_pDepthStencilState = device.CreateDepthStencilState(DSDesc);
            this.m_pDepthStencilState.SetDebugName("DepthStencil");
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.m_pEnvironmentMap);
            D3D11Utils.DisposeAndNull(ref this.m_pEnvironmentRV);
            D3D11Utils.DisposeAndNull(ref this.m_pSam);
            D3D11Utils.DisposeAndNull(ref this.m_pVertexShader);
            D3D11Utils.DisposeAndNull(ref this.m_pPixelShader);
            D3D11Utils.DisposeAndNull(ref this.m_pVertexLayout);
            D3D11Utils.DisposeAndNull(ref this.m_pcbVSPerObject);
            D3D11Utils.DisposeAndNull(ref this.m_pDepthStencilState);
        }

        public void CreateWindowSizeDependentResources()
        {
            var device = this.deviceResources.D3DDevice;

            // Fill the vertex buffer
            SkyboxVertex[] pVertex = new SkyboxVertex[4];

            // Map texels to pixels 
            float fHighW = -1.0f - (1.0f / this.deviceResources.BackBufferWidth);
            float fHighH = -1.0f - (1.0f / this.deviceResources.BackBufferHeight);
            float fLowW = 1.0f + (1.0f / this.deviceResources.BackBufferWidth);
            float fLowH = 1.0f + (1.0f / this.deviceResources.BackBufferHeight);

            pVertex[0].pos = new XMVector(fLowW, fLowH, 1.0f, 1.0f);
            pVertex[1].pos = new XMVector(fLowW, fHighH, 1.0f, 1.0f);
            pVertex[2].pos = new XMVector(fHighW, fLowH, 1.0f, 1.0f);
            pVertex[3].pos = new XMVector(fHighW, fHighH, 1.0f, 1.0f);

            uint uiVertBufSize = 4 * SkyboxVertex.Size;

            //Vertex Buffer
            D3D11BufferDesc vbdesc = new(uiVertBufSize, D3D11BindOptions.VertexBuffer, D3D11Usage.Immutable);
            D3D11SubResourceData InitData = new(pVertex, 0);

            this.m_pVB = device.CreateBuffer(vbdesc, InitData);
            this.m_pVB.SetDebugName("SkyBox");
        }

        public void ReleaseWindowSizeDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.m_pVB);
        }

        public void Render(XMMatrix pmWorldViewProj)
        {
            var context = this.deviceResources.D3DContext;

            context.InputAssemblerSetInputLayout(this.m_pVertexLayout);

            uint uStrides = SkyboxVertex.Size;
            uint uOffsets = 0;
            context.InputAssemblerSetVertexBuffers(0, new[] { this.m_pVB }, new[] { uStrides }, new[] { uOffsets });
            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.R32UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);

            context.VertexShaderSetShader(this.m_pVertexShader, null);
            context.PixelShaderSetShader(this.m_pPixelShader, null);

            SkyboxPerObjectConstantBufferData pVSPerObject = new()
            {
                m_WorldViewProj = pmWorldViewProj
            };

            context.UpdateSubresource(m_pcbVSPerObject, 0, null, pVSPerObject, 0, 0);
            context.VertexShaderSetConstantBuffers(0, new[] { this.m_pcbVSPerObject });

            context.PixelShaderSetSamplers(0, new[] { this.m_pSam });
            context.PixelShaderSetShaderResources(0, new[] { this.m_pEnvironmentRV });

            context.OutputMergerGetDepthStencilState(out D3D11DepthStencilState pDepthStencilStateStored, out uint StencilRef);
            context.OutputMergerSetDepthStencilState(this.m_pDepthStencilState, 0);

            context.Draw(4, 0);

            context.OutputMergerSetDepthStencilState(pDepthStencilStateStored, StencilRef);
        }
    }
}
