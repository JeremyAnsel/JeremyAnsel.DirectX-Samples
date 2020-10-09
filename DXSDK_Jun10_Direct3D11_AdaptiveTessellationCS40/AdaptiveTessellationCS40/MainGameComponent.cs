using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.Media.WavefrontObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AdaptiveTessellationCS40
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private readonly TessellatorCS tessellator = new TessellatorCS();

        // Vertex buffer of the input base mesh
        private D3D11Buffer g_pBaseVB;

        // Vertex buffer of the tessellated mesh
        private D3D11Buffer g_pTessedVB;

        // Index buffer of the tessellated mesh
        private D3D11Buffer g_pTessedIB;

        // Vertex layout for the input base mesh
        private D3D11InputLayout g_pBaseVBLayout;

        // Constant buffer to transfer world view projection matrix to VS
        private D3D11Buffer g_pVSCB;

        // VS for rendering the tessellated mesh
        private D3D11VertexShader g_pVS;

        // VS for rendering the base mesh
        private D3D11VertexShader g_pBaseVS;

        // PS for rendering both the tessellated mesh and base mesh
        private D3D11PixelShader g_pPS;

        // Wireframe rasterizer mode
        D3D11RasterizerState g_pRasWireFrame;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public bool ShowTessellated { get; set; } = true;

        public PartitioningMode PartitioningMode { get; set; } = PartitioningMode.FractionalEven;

        public XMMatrix WorldViewProjectionMatrix { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var d3dDevice = this.deviceResources.D3DDevice;

            this.tessellator.CreateDeviceDependentResources(this.deviceResources);

            XMFloat4[] initData;

            // Parse the .obj file. Both triangle faces and quad faces are supported.
            // Only v and f tags are processed, other tags like vn, vt etc are ignored.
            {
                var initFile = ObjFile.FromFile("BaseMesh.obj");
                var data = new List<XMFloat4>();
                var v = new List<XMFloat4>();

                for (int i = 0; i < initFile.Vertices.Count; i++)
                {
                    ObjVector4 objPosition = initFile.Vertices[i].Position;
                    XMFloat4 pos = new XMFloat4(
                        objPosition.X,
                        objPosition.Y,
                        objPosition.Z,
                        1.0f);

                    v.Add(pos);
                }

                foreach (ObjFace face in initFile.Faces)
                {
                    if (face.Vertices.Count < 3)
                    {
                        continue;
                    }

                    data.Add(v[face.Vertices[0].Vertex - 1]);
                    data.Add(v[face.Vertices[1].Vertex - 1]);
                    data.Add(v[face.Vertices[2].Vertex - 1]);

                    if (face.Vertices.Count >= 4)
                    {
                        data.Add(v[face.Vertices[2].Vertex - 1]);
                        data.Add(v[face.Vertices[3].Vertex - 1]);
                        data.Add(v[face.Vertices[0].Vertex - 1]);
                    }
                }

                initData = data.ToArray();
            }

            this.g_pBaseVB = d3dDevice.CreateBuffer(
                D3D11BufferDesc.From(initData, D3D11BindOptions.ShaderResource | D3D11BindOptions.VertexBuffer),
                initData,
                0,
                0);

            this.tessellator.SetBaseMesh((uint)initData.Length, this.g_pBaseVB);

            this.g_pVS = d3dDevice.CreateVertexShader(File.ReadAllBytes("RenderVertexShader.cso"), null);

            {
                byte[] shaderBytecode = File.ReadAllBytes("RenderBaseVertexShader.cso");
                this.g_pBaseVS = d3dDevice.CreateVertexShader(shaderBytecode, null);

                D3D11InputElementDesc[] layoutDesc = new[]
                {
                    new D3D11InputElementDesc(
                        "POSITION",
                        0,
                        DxgiFormat.R32G32B32A32Float,
                        0,
                        0,
                        D3D11InputClassification.PerVertexData,
                        0)
                };

                this.g_pBaseVBLayout = d3dDevice.CreateInputLayout(layoutDesc, shaderBytecode);
            }

            this.g_pPS = d3dDevice.CreatePixelShader(File.ReadAllBytes("RenderPixelShader.cso"), null);

            // Setup constant buffer
            this.g_pVSCB = d3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ConstantBuffer,
                ByteWidth = 4 * 16
            });

            // Rasterizer state
            this.g_pRasWireFrame = d3dDevice.CreateRasterizerState(new D3D11RasterizerDesc
            {
                CullMode = D3D11CullMode.None,
                FillMode = D3D11FillMode.WireFrame
            });
        }

        public void ReleaseDeviceDependentResources()
        {
            this.tessellator.ReleaseDeviceDependentResources();

            D3D11Utils.DisposeAndNull(ref this.g_pBaseVB);
            D3D11Utils.DisposeAndNull(ref this.g_pTessedVB);
            D3D11Utils.DisposeAndNull(ref this.g_pTessedIB);
            D3D11Utils.DisposeAndNull(ref this.g_pBaseVBLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pVSCB);
            D3D11Utils.DisposeAndNull(ref this.g_pVS);
            D3D11Utils.DisposeAndNull(ref this.g_pBaseVS);
            D3D11Utils.DisposeAndNull(ref this.g_pPS);
            D3D11Utils.DisposeAndNull(ref this.g_pRasWireFrame);
        }

        public void CreateWindowSizeDependentResources()
        {
            this.tessellator.CreateWindowSizeDependentResources();

            XMMatrix mWorld = XMMatrix.Identity;
            XMFloat3 vecEye = new XMFloat3(0.0f, 0.0f, -300.0f);
            XMFloat3 vecAt = new XMFloat3(10.0f, 20.0f, 0.0f);
            XMFloat3 vecUp = new XMFloat3(0.0f, 1.0f, 0.0f);
            XMMatrix mView = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            XMMatrix mProj = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 1.0f, 500000.0f);
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;
            this.WorldViewProjectionMatrix = mWorldViewProjection;
        }

        public void ReleaseWindowSizeDependentResources()
        {
            this.tessellator.ReleaseWindowSizeDependentResources();
        }

        public void Update(ITimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the render target and depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            this.tessellator.PartitioningMode = this.PartitioningMode;

            // Get the projection & view matrix from the camera class
            XMMatrix mWorldViewProjection = this.WorldViewProjectionMatrix;

            if (this.ShowTessellated)
            {
                uint num_tessed_vertices;
                uint num_tessed_indices;

                this.tessellator.PerEdgeTessellation(mWorldViewProjection, ref this.g_pTessedVB, ref this.g_pTessedIB, out num_tessed_vertices, out num_tessed_indices);

                // render tessellated mesh
                if (num_tessed_vertices > 0 && num_tessed_indices > 0)
                {
                    context.RasterizerStageSetState(this.g_pRasWireFrame);

                    context.VertexShaderSetShader(this.g_pVS, null);
                    context.PixelShaderSetShader(this.g_pPS, null);

                    context.UpdateSubresource(this.g_pVSCB, 0, null, mWorldViewProjection, 0, 0);
                    context.VertexShaderSetConstantBuffers(0, new[] { this.g_pVSCB });

                    context.VertexShaderSetShaderResources(0, new[] { this.tessellator.BaseVBSRV, this.tessellator.TessedVerticesBufSRV });

                    context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);
                    context.InputAssemblerSetIndexBuffer(this.g_pTessedIB, DxgiFormat.R32UInt, 0);
                    context.DrawIndexed(num_tessed_indices, 0, 0);

                    context.VertexShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null });
                    context.InputAssemblerSetIndexBuffer(null, DxgiFormat.R32UInt, 0);
                }
            }
            else
            {
                // render original mesh
                context.RasterizerStageSetState(this.g_pRasWireFrame);

                context.VertexShaderSetShader(this.g_pBaseVS, null);
                context.PixelShaderSetShader(this.g_pPS, null);

                context.UpdateSubresource(this.g_pVSCB, 0, null, mWorldViewProjection, 0, 0);
                context.VertexShaderSetConstantBuffers(0, new[] { this.g_pVSCB });

                context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);
                context.InputAssemblerSetInputLayout(this.g_pBaseVBLayout);
                context.InputAssemblerSetVertexBuffers(0, new[] { this.g_pBaseVB }, new[] { 4U * 4 }, new[] { 0U });

                context.Draw(this.tessellator.m_nVertices, 0);

                context.InputAssemblerSetVertexBuffers(0, new D3D11Buffer[] { null }, new[] { 4U * 4 }, new[] { 0U });
            }
        }
    }
}
