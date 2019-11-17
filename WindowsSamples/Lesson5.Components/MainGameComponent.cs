using BasicMaths;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lesson5.Components
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11InputLayout inputLayout;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11VertexShader vertexShader;

        private D3D11PixelShader pixelShader;

        private D3D11Texture2D texture;

        private D3D11ShaderResourceView textureView;

        private D3D11SamplerState sampler;

        private D3D11Buffer constantBuffer;

        private int vertexCount;

        private int indexCount;

        private ConstantBufferData constantBufferData;

        private BasicCamera camera;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel91;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var loader = new BasicLoader(this.deviceResources.D3DDevice);

            loader.LoadShader("Components.VertexShader.cso", null, out this.vertexShader, out this.inputLayout);

            var shapes = new BasicShapes(this.deviceResources.D3DDevice);
            shapes.CreateReferenceAxis(out this.vertexBuffer, out this.indexBuffer, out this.vertexCount, out this.indexCount);

            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.constantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

            loader.LoadShader("Components.PixelShader.cso", out this.pixelShader);

            loader.LoadTexture("texturedata.bin", 256, 256, out this.texture, out this.textureView);

            D3D11SamplerDesc samplerDesc = new D3D11SamplerDesc(
                D3D11Filter.Anisotropic,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                this.deviceResources.D3DFeatureLevel > D3D11FeatureLevel.FeatureLevel91 ? D3D11Constants.DefaultMaxAnisotropy : D3D11Constants.FeatureLevel91DefaultMaxAnisotropy,
                D3D11ComparisonFunction.Never,
                new float[] { 0.0f, 0.0f, 0.0f, 0.0f },
                0.0f,
                float.MaxValue);

            this.sampler = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            this.camera = new BasicCamera();
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBuffer);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.textureView);
            D3D11Utils.DisposeAndNull(ref this.texture);
            D3D11Utils.DisposeAndNull(ref this.sampler);
        }

        public void CreateWindowSizeDependentResources()
        {
            this.camera.SetProjectionParameters(70.0f, this.deviceResources.ScreenViewport.Width / this.deviceResources.ScreenViewport.Height, 0.01f, 100.0f);
            this.constantBufferData.Projection = this.camera.GetProjectionMatrix();
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(StepTimer timer)
        {
            this.constantBufferData.Model = Float4X4.RotationY((float)(-(timer?.TotalSeconds ?? 0.0) * 60.0f));

            this.camera.SetViewParameters(new Float3(0, 1.0f, 2.0f), new Float3(0, 0, 0), new Float3(0, 1, 0));
            this.constantBufferData.View = this.camera.GetViewMatrix();

            this.deviceResources.D3DContext.UpdateSubresource(this.constantBuffer, 0, null, this.constantBufferData, 0, 0);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.071f, 0.04f, 0.561f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            context.InputAssemblerSetInputLayout(this.inputLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { BasicVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShader(this.vertexShader, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });

            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetShaderResources(0, new[] { this.textureView });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });

            context.DrawIndexed((uint)this.indexCount, 0, 0);
        }
    }
}
