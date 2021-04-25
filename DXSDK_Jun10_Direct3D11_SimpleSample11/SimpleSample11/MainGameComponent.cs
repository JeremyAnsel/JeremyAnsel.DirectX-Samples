using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace SimpleSample11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11SamplerState samplerLinear;

        private D3D11Buffer perObjectConstantBuffer;

        private D3D11Buffer perFrameConstantBuffer;

        private double totalSeconds;

        public MainGameComponent()
        {
            XMVector eye = new(0.0f, 0.0f, -5.0f, 0.0f);
            XMVector at = new(0.0f, 0.0f, -0.0f, 0.0f);
            XMVector up = new(0.0f, 1.0f, 0.0f, 0.0f);
            this.ViewMatrix = XMMatrix.LookAtLH(eye, at, up);

            this.WorldMatrix = XMMatrix.Identity;
        }

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel91;

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            // Create the shaders
            byte[] vertexShaderBytecode = File.ReadAllBytes("VertexShader.cso");
            this.vertexShader = this.deviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            byte[] pixelShaderBytecode = File.ReadAllBytes("PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            // Create a layout for the object data
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

            // Create state objects
            D3D11SamplerDesc samplerDesc = new(
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

            this.samplerLinear = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            // Create constant buffers
            this.perObjectConstantBuffer = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(PerObjectConstantBuffer.Size, D3D11BindOptions.ConstantBuffer));

            this.perFrameConstantBuffer = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(PerFrameConstantBuffer.Size, D3D11BindOptions.ConstantBuffer));

            // Create other render resources here
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.samplerLinear);
            D3D11Utils.DisposeAndNull(ref this.perObjectConstantBuffer);
            D3D11Utils.DisposeAndNull(ref this.perFrameConstantBuffer);

            // Delete additional render resources here...
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
            this.totalSeconds = timer?.TotalSeconds ?? 0.0;
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the render target and the depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.176f, 0.196f, 0.667f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Get the projection & view matrix from the camera class
            XMMatrix mWorld = this.WorldMatrix;
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;

            // Set the constant buffers
            PerFrameConstantBuffer pVSPerFrame;
            pVSPerFrame.LightDir = new XMFloat3(0.0f, 0.707f, -0.707f);
            pVSPerFrame.Time = (float)this.totalSeconds;
            pVSPerFrame.LightDiffuse = new XMFloat4(1.0f, 1.0f, 1.0f, 1.0f);
            context.UpdateSubresource(this.perFrameConstantBuffer, 0, null, pVSPerFrame, 0, 0);

            PerObjectConstantBuffer pVSPerObject;
            pVSPerObject.WorldViewProjection = mWorldViewProjection.Transpose();
            pVSPerObject.World = mWorld.Transpose();
            pVSPerObject.MaterialAmbientColor = new XMFloat4(0.3f, 0.3f, 0.3f, 1.0f);
            pVSPerObject.MaterialDiffuseColor = new XMFloat4(0.7f, 0.7f, 0.7f, 1.0f);
            context.UpdateSubresource(this.perObjectConstantBuffer, 0, null, pVSPerObject, 0, 0);

            context.VertexShaderSetConstantBuffers(0, new[] { this.perObjectConstantBuffer, this.perFrameConstantBuffer });

            // Set render resources
            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.VertexShaderSetShader(this.vertexShader, null);
            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetSamplers(0, new[] { this.samplerLinear });

            // Render objects here...
        }
    }
}
