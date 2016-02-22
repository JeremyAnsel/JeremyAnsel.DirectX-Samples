﻿using System.IO;
using System.Runtime.InteropServices;
using BasicMaths;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;

namespace Lesson2.Triangles
{
    class Lesson2Game : GameWindowBase
    {
#if DEBUG
        private const string ShadersDirectory = "../../../Lesson2.Triangles.Shaders/Data/Debug/";
#else
        private const string ShadersDirectory = "../../../Lesson2.Triangles.Shaders/Data/Release/";
#endif

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private Float2[] triangleVertices = new Float2[]
        {
            new Float2(-0.5f, -0.5f),
            new Float2( 0.0f,  0.5f),
            new Float2( 0.5f, -0.5f),
        };

        private ushort[] triangleIndices = new ushort[]
        {
            0, 1, 2,
        };

        public Lesson2Game()
        {
        }

        protected override void Init()
        {
            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            byte[] vertexShaderBytecode = File.ReadAllBytes(Lesson2Game.ShadersDirectory + "Triangles.VertexShader.cso");
            this.vertexShader = this.DeviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            D3D11InputElementDesc[] basicVertexLayoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc
                {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.inputLayout = this.DeviceResources.D3DDevice.CreateInputLayout(basicVertexLayoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes(Lesson2Game.ShadersDirectory + "Triangles.PixelShader.cso");
            this.pixelShader = this.DeviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = D3D11BufferDesc.From(triangleVertices, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.DeviceResources.D3DDevice.CreateBuffer(vertexBufferDesc, triangleVertices, 0, 0);

            var indexBufferDesc = D3D11BufferDesc.From(triangleIndices, D3D11BindOptions.IndexBuffer);
            this.indexBuffer = this.DeviceResources.D3DDevice.CreateBuffer(indexBufferDesc, triangleIndices, 0, 0);
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();

            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Render()
        {
            var context = DeviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.DeviceResources.D3DRenderTargetView }, null);
            context.ClearRenderTargetView(this.DeviceResources.D3DRenderTargetView, new float[] { 0.071f, 0.04f, 0.561f, 1.0f });

            context.InputAssemblerSetInputLayout(this.inputLayout);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { (uint)Marshal.SizeOf(typeof(Float2)) },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShader(this.vertexShader, null);
            context.PixelShaderSetShader(this.pixelShader, null);

            context.DrawIndexed((uint)this.triangleIndices.Length, 0, 0);
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
