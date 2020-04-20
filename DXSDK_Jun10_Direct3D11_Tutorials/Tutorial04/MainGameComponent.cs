using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.D3D11;
using System.IO;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;

namespace Tutorial04
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11Buffer constantBuffer;

        private XMMatrix worldMatrix;

        private XMMatrix viewMatrix;

        private XMMatrix projectionMatrix;

        private static readonly SimpleVertex[] Vertices = new SimpleVertex[]
        {
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, -1.0f ), new XMFloat4( 0.0f, 0.0f, 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, -1.0f ), new XMFloat4( 0.0f, 1.0f, 0.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, 1.0f ), new XMFloat4( 0.0f, 1.0f, 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, 1.0f ), new XMFloat4( 1.0f, 0.0f, 0.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, -1.0f ), new XMFloat4( 1.0f, 0.0f, 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, -1.0f ), new XMFloat4( 1.0f, 1.0f, 0.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, 1.0f ), new XMFloat4( 1.0f, 1.0f, 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, 1.0f ), new XMFloat4( 0.0f, 0.0f, 0.0f, 1.0f ) ),
        };

        private static readonly ushort[] Indices = new ushort[]
        {
            3,1,0,
            2,1,3,

            0,5,4,
            1,5,0,

            3,4,7,
            0,4,3,

            1,6,5,
            2,6,1,

            2,7,6,
            3,7,2,

            6,4,5,
            7,4,6,
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
                },
                new D3D11InputElementDesc
                {
                    SemanticName = "COLOR",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32A32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes("PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = D3D11BufferDesc.From(MainGameComponent.Vertices, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.deviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, MainGameComponent.Vertices, 0, 0);

            var indexBufferDesc = D3D11BufferDesc.From(MainGameComponent.Indices, D3D11BindOptions.IndexBuffer);
            this.indexBuffer = this.deviceResources.D3DDevice.CreateBuffer(indexBufferDesc, MainGameComponent.Indices, 0, 0);

            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.constantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

            this.worldMatrix = XMMatrix.Identity;

            XMVector eye = new XMVector(0.0f, 1.0f, -5.0f, 0.0f);
            XMVector at = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            XMVector up = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            this.viewMatrix = XMMatrix.LookAtLH(eye, at, up);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBuffer);
        }

        public void CreateWindowSizeDependentResources()
        {
            this.projectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivTwo, (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight, 0.01f, 100.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            float t = timer == null ? 0.0f : (float)timer.TotalSeconds;
            this.worldMatrix = XMMatrix.RotationY(t);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Update variables
            ConstantBufferData cb;
            cb.World = this.worldMatrix.Transpose();
            cb.View = this.viewMatrix.Transpose();
            cb.Projection = this.projectionMatrix.Transpose();
            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetInputLayout(this.inputLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { SimpleVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShader(this.vertexShader, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });

            context.PixelShaderSetShader(this.pixelShader, null);

            context.DrawIndexed((uint)MainGameComponent.Indices.Length, 0, 0);
        }
    }
}
