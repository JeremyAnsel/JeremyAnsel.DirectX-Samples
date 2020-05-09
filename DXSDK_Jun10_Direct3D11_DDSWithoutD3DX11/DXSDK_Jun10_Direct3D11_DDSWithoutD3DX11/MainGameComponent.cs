using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dds;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DXSDK_Jun10_Direct3D11_DDSWithoutD3DX11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer constantBufferPerObject;

        private D3D11Buffer constantBufferPerFrame;

        private D3D11SamplerState sampler;

        private D3D11ShaderResourceView textureView;

        private SdkMeshFile mesh;

        public MainGameComponent()
        {
        }

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel92;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            byte[] vertexShaderBytecode = File.ReadAllBytes("VertexShader.cso");
            this.vertexShader = this.deviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc
                {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                },
                new D3D11InputElementDesc
                {
                    SemanticName = "NORMAL",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                },
                new D3D11InputElementDesc
                {
                    SemanticName = "TEXCOORD",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 24,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes("PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            this.constantBufferPerObject = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferPerObject.Size, D3D11BindOptions.ConstantBuffer));

            this.constantBufferPerFrame = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferPerFrame.Size, D3D11BindOptions.ConstantBuffer));

            D3D11SamplerDesc samplerDesc = new D3D11SamplerDesc(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                1,
                D3D11ComparisonFunction.Always,
                new float[] { 0.0f, 0.0f, 0.0f, 0.0f },
                0.0f,
                float.MaxValue);

            this.sampler = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            DdsDirectX.CreateTexture(
                "seafloor.dds",
                this.deviceResources.D3DDevice,
                this.deviceResources.D3DContext,
                out this.textureView);

            this.mesh = SdkMeshFile.FromFile(
                this.deviceResources.D3DDevice,
                this.deviceResources.D3DContext,
                "ball.sdkmesh");

            XMVector eye = new XMVector(0.0f, 0.0f, -5.0f, 0.0f);
            XMVector at = new XMVector(0.0f, 0.0f, -0.0f, 0.0f);
            XMVector up = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            this.ViewMatrix = XMMatrix.LookAtLH(eye, at, up);
            this.WorldMatrix = XMMatrix.Identity;
        }

        public void ReleaseDeviceDependentResources()
        {
            this.mesh?.Release();
            this.mesh = null;

            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.constantBufferPerObject);
            D3D11Utils.DisposeAndNull(ref this.constantBufferPerFrame);
            D3D11Utils.DisposeAndNull(ref this.sampler);
            D3D11Utils.DisposeAndNull(ref this.textureView);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 1000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);

            // Clear the depth stencil
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.627f, 0.627f, 0.980f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Get the projection & view matrix from the camera class
            XMMatrix mWorld = this.WorldMatrix;
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;

            // Set the constant buffers
            ConstantBufferPerObject cbPerObject;
            cbPerObject.m_mWorldViewProjection = mWorldViewProjection.Transpose();
            cbPerObject.m_mWorld = mWorld;
            context.UpdateSubresource(this.constantBufferPerObject, 0, null, cbPerObject, 0, 0);

            ConstantBufferPerFrame cbPerFrame;
            cbPerFrame.m_vLightDir = new XMFloat4(0.0f, 0.707f, -0.707f, 0.0f);
            context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cbPerFrame, 0, 0);

            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBufferPerObject, this.constantBufferPerFrame });

            // Set render resources
            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.VertexShaderSetShader(this.vertexShader, null);
            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetShaderResources(0, new[] { this.textureView });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });

            this.mesh.Render(-1, -1, -1);
        }
    }
}
