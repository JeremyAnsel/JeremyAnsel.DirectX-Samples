using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleBezier11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11VertexShader g_pVertexShader;

        private D3D11HullShader g_pHullShaderInteger;

        private D3D11HullShader g_pHullShaderFracEven;

        private D3D11HullShader g_pHullShaderFracOdd;

        private D3D11DomainShader g_pDomainShader;

        private D3D11PixelShader g_pPixelShader;

        private D3D11PixelShader g_pSolidColorPS;

        private D3D11InputLayout g_pPatchLayout;

        private D3D11Buffer g_pcbPerFrame;

        private const uint g_iBindPerFrame = 0;

        private D3D11RasterizerState g_pRasterizerStateSolid;

        private D3D11RasterizerState g_pRasterizerStateWireframe;

        // Control points for mesh
        private D3D11Buffer g_pControlPointVB;

        public MainGameComponent()
        {
        }

        public float Subdivs { get; set; } = 8.0f;

        public bool DrawWires { get; set; } = false;

        public PartitionMode PartitionMode { get; set; } = PartitionMode.Integer;

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public XMVector EyePt { get; set; }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel110;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var d3dDevice = this.deviceResources.D3DDevice;

            // Create the shaders
            byte[] vertexShaderBytecode = File.ReadAllBytes("VertexShader.cso");
            this.g_pVertexShader = d3dDevice.CreateVertexShader(vertexShaderBytecode, null);
            this.g_pHullShaderInteger = d3dDevice.CreateHullShader(File.ReadAllBytes("HullShaderInteger.cso"), null);
            this.g_pHullShaderFracEven = d3dDevice.CreateHullShader(File.ReadAllBytes("HullShaderFracEven.cso"), null);
            this.g_pHullShaderFracOdd = d3dDevice.CreateHullShader(File.ReadAllBytes("HullShaderFracOdd.cso"), null);
            this.g_pDomainShader = d3dDevice.CreateDomainShader(File.ReadAllBytes("DomainShader.cso"), null);
            this.g_pPixelShader = d3dDevice.CreatePixelShader(File.ReadAllBytes("PixelShader.cso"), null);
            this.g_pSolidColorPS = d3dDevice.CreatePixelShader(File.ReadAllBytes("SolidColorPS.cso"), null);

            // Create our vertex input layout - this matches the BEZIER_CONTROL_POINT structure
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

            this.g_pPatchLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            // Create constant buffers
            this.g_pcbPerFrame = d3dDevice.CreateBuffer(new D3D11BufferDesc(ConstantBufferConstants.Size, D3D11BindOptions.ConstantBuffer));

            // Create solid and wireframe rasterizer state objects
            D3D11RasterizerDesc rasterDesc = D3D11RasterizerDesc.Default;
            rasterDesc.CullMode = D3D11CullMode.None;
            rasterDesc.IsDepthClipEnabled = true;

            rasterDesc.FillMode = D3D11FillMode.Solid;
            this.g_pRasterizerStateSolid = d3dDevice.CreateRasterizerState(rasterDesc);

            rasterDesc.FillMode = D3D11FillMode.WireFrame;
            this.g_pRasterizerStateWireframe = d3dDevice.CreateRasterizerState(rasterDesc);

            D3D11BufferDesc vbdesc = D3D11BufferDesc.From(MobiusStrip.Points, D3D11BindOptions.VertexBuffer);
            this.g_pControlPointVB = d3dDevice.CreateBuffer(vbdesc, MobiusStrip.Points, 0, 0);

            XMFloat3 vecEye = new XMFloat3(1.0f, 1.5f, -3.5f);
            XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);
            XMFloat3 vecUp = new XMFloat3(0.0f, 1.0f, 0.0f);

            this.ViewMatrix = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
            this.EyePt = vecEye;
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pVertexShader);
            D3D11Utils.DisposeAndNull(ref this.g_pHullShaderInteger);
            D3D11Utils.DisposeAndNull(ref this.g_pHullShaderFracEven);
            D3D11Utils.DisposeAndNull(ref this.g_pHullShaderFracOdd);
            D3D11Utils.DisposeAndNull(ref this.g_pDomainShader);
            D3D11Utils.DisposeAndNull(ref this.g_pPixelShader);
            D3D11Utils.DisposeAndNull(ref this.g_pSolidColorPS);
            D3D11Utils.DisposeAndNull(ref this.g_pPatchLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pcbPerFrame);
            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateSolid);
            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateWireframe);
            D3D11Utils.DisposeAndNull(ref this.g_pControlPointVB);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 20.0f);
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

            // WVP
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mViewProjection = mView * mProj;

            // Update per-frame variables
            ConstantBufferConstants cb;
            cb.ViewProjection = mViewProjection.Transpose();
            cb.CameraPosWorld = this.EyePt;
            cb.TessellationFactor = this.Subdivs;
            context.UpdateSubresource(this.g_pcbPerFrame, 0, null, cb, 0, 0);

            // Clear the render target and depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.05f, 0.05f, 0.05f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Set state for solid rendering
            context.RasterizerStageSetState(this.g_pRasterizerStateSolid);

            // Render the meshes
            // Bind all of the CBs
            context.VertexShaderSetConstantBuffers(g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.HullShaderSetConstantBuffers(g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.DomainShaderSetConstantBuffers(g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.PixelShaderSetConstantBuffers(g_iBindPerFrame, new[] { this.g_pcbPerFrame });

            // Set the shaders
            context.VertexShaderSetShader(this.g_pVertexShader, null);

            // For this sample, choose either the "integer", "fractional_even",
            // or "fractional_odd" hull shader
            switch (this.PartitionMode)
            {
                case PartitionMode.Integer:
                default:
                    context.HullShaderSetShader(this.g_pHullShaderInteger, null);
                    break;

                case PartitionMode.FractionalEven:
                    context.HullShaderSetShader(this.g_pHullShaderFracEven, null);
                    break;

                case PartitionMode.FractionalOdd:
                    context.HullShaderSetShader(this.g_pHullShaderFracOdd, null);
                    break;
            }

            context.DomainShaderSetShader(this.g_pDomainShader, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(this.g_pPixelShader, null);

            // Optionally draw the wireframe
            if (this.DrawWires)
            {
                context.PixelShaderSetShader(this.g_pSolidColorPS, null);
                context.RasterizerStageSetState(this.g_pRasterizerStateWireframe);
            }

            // Set the input assembler
            // This sample uses patches with 16 control points each
            // Although the Mobius strip only needs to use a vertex buffer,
            // you can use an index buffer as well by calling IASetIndexBuffer().
            context.InputAssemblerSetInputLayout(this.g_pPatchLayout);
            context.InputAssemblerSetVertexBuffers(0, new[] { this.g_pControlPointVB }, new[] { BezierControlPoint.Size }, new[] { 0U });
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.PatchList16ControlPoint);

            // Draw the mesh
            context.Draw((uint)MobiusStrip.Points.Length, 0);

            context.RasterizerStageSetState(this.g_pRasterizerStateSolid);
        }
    }
}
