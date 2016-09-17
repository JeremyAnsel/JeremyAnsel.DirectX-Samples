using System.IO;
using BasicMaths;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;

namespace Lesson3.Cubes
{
    class Lesson3Game : GameWindowBase
    {
#if DEBUG
        private const string ShadersDirectory = "../../../Lesson3.Cubes.Shaders/Data/Debug/";
#else
        private const string ShadersDirectory = "../../../Lesson3.Cubes.Shaders/Data/Release/";
#endif

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11Buffer constantBuffer;

        private ConstantBufferData constantBufferData;

        private float degree;

        private BasicVertex[] cubeVertices = new BasicVertex[]
        {
            new BasicVertex(new Float3(-0.5f, 0.5f, -0.5f), new Float3(0.0f, 1.0f, 0.0f)), // +Y (top face)
            new BasicVertex(new Float3( 0.5f, 0.5f, -0.5f), new Float3(1.0f, 1.0f, 0.0f)),
            new BasicVertex(new Float3( 0.5f, 0.5f,  0.5f), new Float3(1.0f, 1.0f, 1.0f)),
            new BasicVertex(new Float3(-0.5f, 0.5f,  0.5f), new Float3(0.0f, 1.0f, 1.0f)),

            new BasicVertex(new Float3(-0.5f, -0.5f,  0.5f), new Float3(0.0f, 0.0f, 1.0f)), // -Y (bottom face)
            new BasicVertex(new Float3( 0.5f, -0.5f,  0.5f), new Float3(1.0f, 0.0f, 1.0f)),
            new BasicVertex(new Float3( 0.5f, -0.5f, -0.5f), new Float3(1.0f, 0.0f, 0.0f)),
            new BasicVertex(new Float3(-0.5f, -0.5f, -0.5f), new Float3(0.0f, 0.0f, 0.0f)),
        };

        private ushort[] cubeIndices = new ushort[]
        {
            0, 1, 2,
            0, 2, 3,

            4, 5, 6,
            4, 6, 7,

            3, 2, 5,
            3, 5, 4,

            2, 1, 6,
            2, 6, 5,

            1, 7, 6,
            1, 0, 7,

            0, 3, 4,
            0, 4, 7
        };

        public Lesson3Game()
        {
        }

        protected override void Init()
        {
            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            byte[] vertexShaderBytecode = File.ReadAllBytes(Lesson3Game.ShadersDirectory + "Cubes.VertexShader.cso");
            this.vertexShader = this.DeviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

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
                    SemanticName = "COLOR",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.inputLayout = this.DeviceResources.D3DDevice.CreateInputLayout(basicVertexLayoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes(Lesson3Game.ShadersDirectory + "Cubes.PixelShader.cso");
            this.pixelShader = this.DeviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = D3D11BufferDesc.From(cubeVertices, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.DeviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, cubeVertices, 0, 0);

            var indexBufferDesc = D3D11BufferDesc.From(cubeIndices, D3D11BindOptions.IndexBuffer);
            this.indexBuffer = this.DeviceResources.D3DDevice.CreateBuffer(indexBufferDesc, cubeIndices, 0, 0);

            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.constantBuffer = this.DeviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

            this.constantBufferData.View = new Float4X4(
                -1.00000000f, 0.00000000f, 0.00000000f, 0.00000000f,
                 0.00000000f, 0.89442718f, 0.44721359f, 0.00000000f,
                 0.00000000f, 0.44721359f, -0.89442718f, -2.23606800f,
                 0.00000000f, 0.00000000f, 0.00000000f, 1.00000000f
                 );
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();

            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBuffer);
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            var viewport = this.DeviceResources.ScreenViewport;

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

        protected override void Update()
        {
            base.Update();

            this.constantBufferData.Model = Float4X4.RotationY(-degree);
            degree += 1.0f;
        }

        protected override void Render()
        {
            var context = DeviceResources.D3DContext;

            context.UpdateSubresource(this.constantBuffer, 0, null, this.constantBufferData, 0, 0);

            context.OutputMergerSetRenderTargets(new[] { this.DeviceResources.D3DRenderTargetView }, this.DeviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.DeviceResources.D3DRenderTargetView, new float[] { 0.071f, 0.04f, 0.561f, 1.0f });
            context.ClearDepthStencilView(this.DeviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

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

            context.DrawIndexed((uint)this.cubeIndices.Length, 0, 0);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown && key == VirtualKey.F12)
            {
                this.FpsTextRenderer.IsEnabled = !this.FpsTextRenderer.IsEnabled;
            }
        }
    }
}
