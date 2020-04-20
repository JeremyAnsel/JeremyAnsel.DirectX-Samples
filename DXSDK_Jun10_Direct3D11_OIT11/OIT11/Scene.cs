using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.Dxgi;
using System.IO;

namespace OIT11
{
    class Scene : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout vertexLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer vertexShaderConstantBuffer;

        private XMMatrix worldViewProj;

        public Scene()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel110;
            }
        }

        public void SetWorldViewProj(XMMatrix worldViewProj)
        {
            this.worldViewProj = worldViewProj;
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            byte[] vertexShaderBytecode = File.ReadAllBytes("SceneVS.cso");
            this.vertexShader = this.deviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc
                {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32A32Float,
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
                    AlignedByteOffset = 0xffffffff,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.vertexLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes("ScenePS.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            // Set up vertex buffer
            float fRight = -10.0f;
            float fTop = -10.0f;
            float fLeft = 10.0f;
            float fLowH = -5.0f;

            // Fill the vertex buffer
            var vertices = new SceneVertex[12];

            vertices[0].Pos = new XMFloat4(fLeft, fLowH, 50.0f, 1.0f);
            vertices[1].Pos = new XMFloat4(fLeft, fTop, 50.0f, 1.0f);
            vertices[2].Pos = new XMFloat4(fRight, fLowH, 50.0f, 1.0f);
            vertices[3].Pos = new XMFloat4(fRight, fTop, 50.0f, 1.0f);

            vertices[0].Color = new XMFloat4(1.0f, 0.0f, 0.0f, 0.5f);
            vertices[1].Color = new XMFloat4(1.0f, 0.0f, 0.0f, 0.5f);
            vertices[2].Color = new XMFloat4(1.0f, 0.0f, 0.0f, 0.5f);
            vertices[3].Color = new XMFloat4(1.0f, 0.0f, 0.0f, 0.5f);

            vertices[4].Pos = new XMFloat4(fLeft, fLowH, 60.0f, 1.0f);
            vertices[5].Pos = new XMFloat4(fLeft, fTop, 60.0f, 1.0f);
            vertices[6].Pos = new XMFloat4(fRight, fLowH, 40.0f, 1.0f);
            vertices[7].Pos = new XMFloat4(fRight, fTop, 40.0f, 1.0f);

            vertices[4].Color = new XMFloat4(0.0f, 1.0f, 0.0f, 0.5f);
            vertices[5].Color = new XMFloat4(0.0f, 1.0f, 0.0f, 0.5f);
            vertices[6].Color = new XMFloat4(0.0f, 1.0f, 0.0f, 0.5f);
            vertices[7].Color = new XMFloat4(0.0f, 1.0f, 0.0f, 0.5f);

            vertices[8].Pos = new XMFloat4(fLeft, fLowH, 40.0f, 1.0f);
            vertices[9].Pos = new XMFloat4(fLeft, fTop, 40.0f, 1.0f);
            vertices[10].Pos = new XMFloat4(fRight, fLowH, 60.0f, 1.0f);
            vertices[11].Pos = new XMFloat4(fRight, fTop, 60.0f, 1.0f);

            vertices[8].Color = new XMFloat4(0.0f, 0.0f, 1.0f, 0.5f);
            vertices[9].Color = new XMFloat4(0.0f, 0.0f, 1.0f, 0.5f);
            vertices[10].Color = new XMFloat4(0.0f, 0.0f, 1.0f, 0.5f);
            vertices[11].Color = new XMFloat4(0.0f, 0.0f, 1.0f, 0.5f);

            var vertexBufferDesc = D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer, D3D11Usage.Immutable);
            this.vertexBuffer = this.deviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, vertices, 0, 0);

            var constantBufferDesc = new D3D11BufferDesc(SceneVertexShaderConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.vertexShaderConstantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.vertexLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.vertexShaderConstantBuffer);
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

            context.InputAssemblerSetInputLayout(this.vertexLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { SceneVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.R32UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);

            context.VertexShaderSetShader(this.vertexShader, null);

            SceneVertexShaderConstantBufferData cb;
            cb.WorldViewProj = this.worldViewProj;

            context.UpdateSubresource(this.vertexShaderConstantBuffer, 0, null, cb, 0, 0);
            context.VertexShaderSetConstantBuffers(0, new[] { this.vertexShaderConstantBuffer });

            context.Draw(12, 0);
        }
    }
}
