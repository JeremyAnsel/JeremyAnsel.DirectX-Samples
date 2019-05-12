using BasicMaths;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace Lesson4.Textures
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

#if DEBUG
        private const string ShadersDirectory = "../../../Lesson4.Textures.Shaders/Data/Debug/";
#else
        private const string ShadersDirectory = "../../../Lesson4.Textures.Shaders/Data/Release/";
#endif

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11Buffer constantBuffer;

        private ConstantBufferData constantBufferData;

        private D3D11ShaderResourceView textureView;

        private D3D11SamplerState sampler;

        private float degree;

        private BasicVertex[] cubeVertices = new BasicVertex[]
        {
            new BasicVertex( new Float3(-0.5f, 0.5f, -0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 0.0f) ), // +Y (top face)
            new BasicVertex( new Float3( 0.5f, 0.5f, -0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3( 0.5f, 0.5f,  0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3(-0.5f, 0.5f,  0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 1.0f) ),

            new BasicVertex( new Float3(-0.5f, -0.5f,  0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(0.0f, 0.0f) ), // -Y (bottom face)
            new BasicVertex( new Float3( 0.5f, -0.5f,  0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3( 0.5f, -0.5f, -0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(0.0f, 1.0f) ),

            new BasicVertex( new Float3(0.5f,  0.5f,  0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f) ), // +X (right face)
            new BasicVertex( new Float3(0.5f,  0.5f, -0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3(0.5f, -0.5f, -0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3(0.5f, -0.5f,  0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.0f) ),

            new BasicVertex( new Float3(-0.5f,  0.5f, -0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f) ), // -X (left face)
            new BasicVertex( new Float3(-0.5f,  0.5f,  0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3(-0.5f, -0.5f,  0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.0f) ),

            new BasicVertex( new Float3(-0.5f,  0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 0.0f) ), // +Z (front face)
            new BasicVertex( new Float3( 0.5f,  0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3( 0.5f, -0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3(-0.5f, -0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 1.0f) ),

            new BasicVertex( new Float3( 0.5f,  0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 0.0f) ), // -Z (back face)
            new BasicVertex( new Float3(-0.5f,  0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(1.0f, 0.0f) ),
            new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(1.0f, 1.0f) ),
            new BasicVertex( new Float3( 0.5f, -0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 1.0f) ),
        };

        private ushort[] cubeIndices = new ushort[]
        {
            0, 1, 2,
            0, 2, 3,

            4, 5, 6,
            4, 6, 7,

            8, 9, 10,
            8, 10, 11,

            12, 13, 14,
            12, 14, 15,

            16, 17, 18,
            16, 18, 19,

            20, 21, 22,
            20, 22, 23
        };

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

            byte[] vertexShaderBytecode = File.ReadAllBytes(MainGameComponent.ShadersDirectory + "Textures.VertexShader.cso");
            this.vertexShader = this.deviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            D3D11InputElementDesc[] basicVertexLayoutDesc = new D3D11InputElementDesc[]
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

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(basicVertexLayoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes(MainGameComponent.ShadersDirectory + "Textures.PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = D3D11BufferDesc.From(cubeVertices, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.deviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, cubeVertices, 0, 0);

            var indexBufferDesc = D3D11BufferDesc.From(cubeIndices, D3D11BindOptions.IndexBuffer);
            this.indexBuffer = this.deviceResources.D3DDevice.CreateBuffer(indexBufferDesc, cubeIndices, 0, 0);

            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.constantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

            this.constantBufferData.View = new Float4X4(
                -1.00000000f, 0.00000000f, 0.00000000f, 0.00000000f,
                 0.00000000f, 0.89442718f, 0.44721359f, 0.00000000f,
                 0.00000000f, 0.44721359f, -0.89442718f, -2.23606800f,
                 0.00000000f, 0.00000000f, 0.00000000f, 1.00000000f
                 );

            byte[] textureData = File.ReadAllBytes("../../texturedata.bin");

            D3D11Texture2DDesc textureDesc = new D3D11Texture2DDesc(DxgiFormat.R8G8B8A8UNorm, 256, 256, 1, 1);

            D3D11SubResourceData[] textureSubResData = new[]
                {
                    new D3D11SubResourceData(textureData, 1024)
                };

            using (var texture = this.deviceResources.D3DDevice.CreateTexture2D(textureDesc, textureSubResData))
            {
                D3D11ShaderResourceViewDesc textureViewDesc = new D3D11ShaderResourceViewDesc
                {
                    Format = textureDesc.Format,
                    ViewDimension = D3D11SrvDimension.Texture2D,
                    Texture2D = new D3D11Texture2DSrv
                    {
                        MipLevels = textureDesc.MipLevels,
                        MostDetailedMip = 0
                    }
                };

                this.textureView = this.deviceResources.D3DDevice.CreateShaderResourceView(texture, textureViewDesc);
            }

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
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBuffer);
            D3D11Utils.DisposeAndNull(ref this.textureView);
            D3D11Utils.DisposeAndNull(ref this.sampler);
        }

        public void CreateWindowSizeDependentResources()
        {
            var viewport = this.deviceResources.ScreenViewport;

            float xScale = 1.42814801f;
            float yScale = 1.42814801f;

            if (viewport.Width > viewport.Height)
            {
                xScale = yScale * viewport.Height / viewport.Width;
            }
            else
            {
                yScale = xScale * viewport.Width / viewport.Height;
            }

            this.constantBufferData.Projection = new Float4X4(
                xScale, 0.0f, 0.0f, 0.0f,
                0.0f, yScale, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, -0.01f,
                0.0f, 0.0f, -1.0f, 0.0f
                );
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(StepTimer timer)
        {
            this.constantBufferData.Model = Float4X4.RotationY(-this.degree);
            this.degree += 1.0f;
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.UpdateSubresource(this.constantBuffer, 0, null, this.constantBufferData, 0, 0);

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

            context.DrawIndexed((uint)this.cubeIndices.Length, 0, 0);
        }
    }
}
