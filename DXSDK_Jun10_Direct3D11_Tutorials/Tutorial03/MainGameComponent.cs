using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace Tutorial03
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private static readonly SimpleVertex[] Vertices = new SimpleVertex[]
        {
            new SimpleVertex(new XMFloat3( 0.0f, 0.5f, 0.5f )),
            new SimpleVertex(new XMFloat3( 0.5f, -0.5f, 0.5f )),
            new SimpleVertex(new XMFloat3( -0.5f, -0.5f, 0.5f )),
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
                }
            };

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes("PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = D3D11BufferDesc.From(MainGameComponent.Vertices, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.deviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, MainGameComponent.Vertices, 0, 0);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
        }

        public void CreateWindowSizeDependentResources()
        {
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(StepTimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, null);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });

            context.InputAssemblerSetInputLayout(this.inputLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { SimpleVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            // Render a triangle
            context.VertexShaderSetShader(this.vertexShader, null);
            context.PixelShaderSetShader(this.pixelShader, null);

            context.Draw((uint)MainGameComponent.Vertices.Length, 0);
        }
    }
}
