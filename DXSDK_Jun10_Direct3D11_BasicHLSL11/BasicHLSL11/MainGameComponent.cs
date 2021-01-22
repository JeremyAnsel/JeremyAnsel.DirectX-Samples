using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System.IO;

namespace BasicHLSL11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private XMMatrix centerMesh;

        private SdkMeshFile mesh;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11SamplerState sampler;

        private D3D11Buffer constantBufferVSPerObject;

        private D3D11Buffer constantBufferPSPerObject;

        private D3D11Buffer constantBufferPSPerFrame;

        private const uint ConstantBufferVSPerObjectBind = 0;

        private const uint ConstantBufferPSPerObjectBind = 0;

        private const uint ConstantBufferPSPerFrameBind = 1;

        public MainGameComponent()
        {
            var vLightDir = new XMFloat3(-1, 1, -1);
            this.LightDirection = XMVector3.Normalize(vLightDir);

            XMVector eye = new XMVector(0.0f, 0.0f, -100.0f, 0.0f);
            XMVector at = new XMVector(0.0f, 0.0f, -0.0f, 0.0f);
            XMVector up = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            this.ViewMatrix = XMMatrix.LookAtLH(eye, at, up);
            this.WorldMatrix = XMMatrix.Identity;
        }

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public XMFloat3 LightDirection { get; set; }

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

            XMFloat3 vCenter = new XMFloat3(0.25767413f, -28.503521f, 111.00689f);
            XMMatrix m = XMMatrix.Translation(-vCenter.X, -vCenter.Y, -vCenter.Z);
            m *= XMMatrix.RotationY(XMMath.PI);
            m *= XMMatrix.RotationX(XMMath.PIDivTwo);
            this.centerMesh = m;

            // Load the mesh
            this.mesh = SdkMeshFile.FromFile(
                this.deviceResources.D3DDevice,
                this.deviceResources.D3DContext,
                "Tiny\\Tiny.sdkmesh");

            // Create the shaders
            byte[] vertexShaderBytecode = File.ReadAllBytes("BasicHLSL11_VS.cso");
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

            byte[] pixelShaderBytecode = File.ReadAllBytes("BasicHLSL11_PS.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            // Create a sampler state
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

            // Setup constant buffers
            this.constantBufferVSPerObject = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferVSPerObject.Size, D3D11BindOptions.ConstantBuffer));

            this.constantBufferPSPerObject = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferPSPerObject.Size, D3D11BindOptions.ConstantBuffer));

            this.constantBufferPSPerFrame = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferPSPerFrame.Size, D3D11BindOptions.ConstantBuffer));
        }

        public void ReleaseDeviceDependentResources()
        {
            this.mesh?.Release();
            this.mesh = null;

            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.sampler);
            D3D11Utils.DisposeAndNull(ref this.constantBufferVSPerObject);
            D3D11Utils.DisposeAndNull(ref this.constantBufferPSPerObject);
            D3D11Utils.DisposeAndNull(ref this.constantBufferPSPerFrame);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 2.0f, 4000.0f);
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

            // Clear the render target and depth stencil
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.25f, 0.25f, 0.55f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Get the projection & view matrix from the camera class
            XMMatrix mWorld = this.centerMesh * this.WorldMatrix;
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;
            XMFloat3 vLightDir = this.LightDirection;

            // Per frame cb update
            float fAmbient = 0.1f;
            ConstantBufferPSPerFrame cbPSPerFrame;
            cbPSPerFrame.m_vLightDirAmbient = new XMFloat4(vLightDir.X, vLightDir.Y, vLightDir.Z, fAmbient);
            context.UpdateSubresource(this.constantBufferPSPerFrame, 0, null, cbPSPerFrame, 0, 0);
            context.PixelShaderSetConstantBuffers(ConstantBufferPSPerFrameBind, new[] { this.constantBufferPSPerFrame });

            // IA setup
            context.InputAssemblerSetInputLayout(this.inputLayout);

            // Set the shaders
            context.VertexShaderSetShader(this.vertexShader, null);
            context.PixelShaderSetShader(this.pixelShader, null);

            // Set the per object constant data
            // VS Per object
            ConstantBufferVSPerObject cbVSPerObject;
            cbVSPerObject.m_WorldViewProj = mWorldViewProjection.Transpose();
            cbVSPerObject.m_World = mWorld.Transpose();
            context.UpdateSubresource(this.constantBufferVSPerObject, 0, null, cbVSPerObject, 0, 0);
            context.VertexShaderSetConstantBuffers(ConstantBufferVSPerObjectBind, new[] { this.constantBufferVSPerObject });

            // PS Per object
            ConstantBufferPSPerObject cbPSPerObject;
            cbPSPerObject.m_vObjectColor = new XMFloat4(1, 1, 1, 1);
            context.UpdateSubresource(this.constantBufferPSPerObject, 0, null, cbPSPerObject, 0, 0);
            context.PixelShaderSetConstantBuffers(ConstantBufferPSPerObjectBind, new[] { this.constantBufferPSPerObject });

            // Set render resources
            context.PixelShaderSetSamplers(0, new[] { this.sampler });

            // Render

            //// Get the mesh
            //context.InputAssemblerSetVertexBuffers(
            //    0,
            //    new[] { this.mesh.Meshes[0].VertexBuffers[0].Buffer },
            //    new[] { this.mesh.Meshes[0].VertexBuffers[0].StrideBytes },
            //    new[] { 0U });

            //context.InputAssemblerSetIndexBuffer(
            //    this.mesh.Meshes[0].IndexBuffer.Buffer,
            //    this.mesh.Meshes[0].IndexBuffer.IndexFormat,
            //    0);

            //for (int subsetIndex = 0; subsetIndex < this.mesh.Meshes.Count; subsetIndex++)
            //{
            //    // Get the subset
            //    SdkMeshSubset subset = this.mesh.Meshes[0].Subsets[subsetIndex];

            //    context.InputAssemblerSetPrimitiveTopology(subset.PrimitiveTopology);

            //    D3D11ShaderResourceView pDiffuseRV = this.mesh.Materials[subset.MaterialIndex].DiffuseTextureView;
            //    context.PixelShaderSetShaderResources(0, new[] { pDiffuseRV });

            //    context.DrawIndexed((uint)subset.IndexCount, 0, subset.VertexStart);
            //}

            this.mesh.Render(0, -1, -1);
        }
    }
}
