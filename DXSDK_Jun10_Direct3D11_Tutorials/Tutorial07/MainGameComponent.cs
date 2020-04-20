using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using System.IO;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.Dds;

namespace Tutorial07
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11Buffer constantBufferNeverChanges;

        private D3D11Buffer constantBufferChangesOnResize;

        private D3D11Buffer constantBufferChangesEveryFrame;

        private D3D11ShaderResourceView textureView;

        private D3D11SamplerState sampler;

        private XMMatrix worldMatrix;

        private XMMatrix viewMatrix;

        private XMMatrix projectionMatrix;

        private XMVector meshColor;

        private static readonly SimpleVertex[] Vertices = new SimpleVertex[]
        {
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, -1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, -1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, 1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, 1.0f ), new XMFloat2( 0.0f, 1.0f ) ),

            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, -1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, -1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, 1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, 1.0f ), new XMFloat2( 0.0f, 1.0f ) ),

            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, 1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, -1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, -1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, 1.0f ), new XMFloat2( 0.0f, 1.0f ) ),

            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, 1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, -1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, -1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, 1.0f ), new XMFloat2( 0.0f, 1.0f ) ),

            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, -1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, -1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, -1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, -1.0f ), new XMFloat2( 0.0f, 1.0f ) ),

            new SimpleVertex( new XMFloat3( -1.0f, -1.0f, 1.0f ), new XMFloat2( 0.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, -1.0f, 1.0f ), new XMFloat2( 1.0f, 0.0f ) ),
            new SimpleVertex( new XMFloat3( 1.0f, 1.0f, 1.0f ), new XMFloat2( 1.0f, 1.0f ) ),
            new SimpleVertex( new XMFloat3( -1.0f, 1.0f, 1.0f ), new XMFloat2( 0.0f, 1.0f ) ),
        };

        private static readonly ushort[] Indices = new ushort[]
        {
            3,1,0,
            2,1,3,

            6,4,5,
            7,4,6,

            11,9,8,
            10,9,11,

            14,12,13,
            15,12,14,

            19,17,16,
            18,17,19,

            22,20,21,
            23,20,22
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
                    SemanticName = "TEXCOORD",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32Float,
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

            this.constantBufferNeverChanges = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferNeverChangesData.Size, D3D11BindOptions.ConstantBuffer));

            this.constantBufferChangesOnResize = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferChangesOnResizeData.Size, D3D11BindOptions.ConstantBuffer));

            this.constantBufferChangesEveryFrame = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferChangesEveryFrameData.Size, D3D11BindOptions.ConstantBuffer));

            DdsDirectX.CreateTexture(
                "seafloor.dds",
                this.deviceResources.D3DDevice,
                this.deviceResources.D3DContext,
                out this.textureView);

            D3D11SamplerDesc samplerDesc = new D3D11SamplerDesc(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                0,
                D3D11ComparisonFunction.Never,
                new float[] { 0.0f, 0.0f, 0.0f, 0.0f },
                0.0f,
                float.MaxValue);

            this.sampler = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            this.worldMatrix = XMMatrix.Identity;

            XMVector eye = new XMVector(0.0f, 3.0f, -6.0f, 0.0f);
            XMVector at = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            XMVector up = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            this.viewMatrix = XMMatrix.LookAtLH(eye, at, up);

            ConstantBufferNeverChangesData cbNeverChanges;
            cbNeverChanges.View = this.viewMatrix.Transpose();
            this.deviceResources.D3DContext.UpdateSubresource(this.constantBufferNeverChanges, 0, null, cbNeverChanges, 0, 0);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBufferNeverChanges);
            D3D11Utils.DisposeAndNull(ref this.constantBufferChangesOnResize);
            D3D11Utils.DisposeAndNull(ref this.constantBufferChangesEveryFrame);
            D3D11Utils.DisposeAndNull(ref this.textureView);
            D3D11Utils.DisposeAndNull(ref this.sampler);
        }

        public void CreateWindowSizeDependentResources()
        {
            this.projectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight, 0.01f, 100.0f);

            ConstantBufferChangesOnResizeData cbChangesOnResize;
            cbChangesOnResize.Projection = this.projectionMatrix.Transpose();
            this.deviceResources.D3DContext.UpdateSubresource(this.constantBufferChangesOnResize, 0, null, cbChangesOnResize, 0, 0);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            float t = timer == null ? 0.0f : (float)timer.TotalSeconds;

            this.worldMatrix = XMMatrix.RotationY(t);

            this.meshColor = new XMVector(
                (XMScalar.Sin(t * 1.0f) + 1.0f) * 0.5f,
                (XMScalar.Cos(t * 3.0f) + 1.0f) * 0.5f,
                (XMScalar.Sin(t * 5.0f) + 1.0f) * 0.5f,
                1.0f);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            ConstantBufferChangesEveryFrameData cb;
            cb.World = this.worldMatrix.Transpose();
            cb.MeshColor = this.meshColor;
            context.UpdateSubresource(this.constantBufferChangesEveryFrame, 0, null, cb, 0, 0);

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            context.InputAssemblerSetInputLayout(this.inputLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { SimpleVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShader(this.vertexShader, null);
            context.VertexShaderSetConstantBuffers(0, new[] {
                this.constantBufferNeverChanges,
                this.constantBufferChangesOnResize,
                this.constantBufferChangesEveryFrame });
            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetConstantBuffers(2, new[] { this.constantBufferChangesEveryFrame });
            context.PixelShaderSetShaderResources(0, new[] { this.textureView });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });
            context.DrawIndexed((uint)MainGameComponent.Indices.Length, 0, 0);
        }
    }
}
