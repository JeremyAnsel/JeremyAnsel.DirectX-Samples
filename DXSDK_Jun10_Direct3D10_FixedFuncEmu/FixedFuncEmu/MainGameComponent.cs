using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dds;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedFuncEmu
{
    class MainGameComponent : IGameComponent
    {
        private const int MaxBalls = 10;

        private DeviceResources deviceResources;

        private XMMatrix g_mBlackHole;
        private XMMatrix g_mLightView;
        private XMMatrix g_mLightProj;
        private readonly SceneLight[] g_lights = new SceneLight[8];
        private readonly XMVector[] g_clipplanes = new XMVector[3];
        private readonly Ball[] g_balls = new Ball[MaxBalls];
        private double g_fLaunchInterval = 0.3f;
        private float g_fRotateSpeed = 70.0f;
        private double g_dLastLaunch;

        private D3D11Buffer constantBufferPerView;
        private D3D11Buffer constantBufferPerFrame;
        private D3D11Buffer g_pScreenQuadVB;
        private D3D11InputLayout g_pVertexLayout;
        private D3D11ShaderResourceView g_pScreenTexRV;
        private D3D11ShaderResourceView g_pProjectedTexRV;

        private SdkMeshFile g_ballMesh;
        private SdkMeshFile g_roomMesh;
        private SdkMeshFile g_holeMesh;

        private D3D11VertexShader g_VSScenemain;
        private D3D11PixelShader g_PSScenemain;
        private D3D11GeometryShader g_GSFlatmain;
        private D3D11GeometryShader g_GSPointmain;
        private D3D11VertexShader g_VSScreenSpacemain;
        private D3D11PixelShader g_PSAlphaTestmain;

        private D3D11SamplerState g_samLinear;
        private D3D11DepthStencilState g_EnableDepth;
        private D3D11DepthStencilState g_DisableDepth;

        public MainGameComponent()
        {
            this.g_dLastLaunch = -this.g_fLaunchInterval - 1;

            XMVector vecEye = new(0.0f, 2.3f, -8.5f, 0.0f);
            XMVector vecAt = new(0.0f, 2.0f, 0.0f, 0.0f);
            XMVector vecUp = new(0.0f, 1.0f, 0.0f, 0.0f);
            this.ViewTransform = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public XMMatrix ViewTransform { get; set; }

        public XMMatrix ProjectionTransform { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            this.constantBufferPerView = device.CreateBuffer(new D3D11BufferDesc(ConstantBufferPerViewData.Size, D3D11BindOptions.ConstantBuffer));
            this.constantBufferPerFrame = device.CreateBuffer(new D3D11BufferDesc(ConstantBufferPerFrameData.Size, D3D11BindOptions.ConstantBuffer));

            // Create the screenspace quad VB
            this.g_pScreenQuadVB = device.CreateBuffer(new D3D11BufferDesc(SceneVertex.Size * 4, D3D11BindOptions.VertexBuffer));

            // Create our vertex input layout
            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXTURE", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
            };

            this.g_pVertexLayout = device.CreateInputLayout(layoutDesc, File.ReadAllBytes("VSScreenSpacemain.cso"));

            // Load the HUD and Cookie Textures
            DdsDirectX.CreateTexture("hud.dds", device, context, out this.g_pScreenTexRV);
            DdsDirectX.CreateTexture("cookie.dds", device, context, out this.g_pProjectedTexRV);

            // Load the meshes
            this.g_ballMesh = SdkMeshFile.FromFile(device, context, "Ball\\ball.sdkmesh");
            this.g_roomMesh = SdkMeshFile.FromFile(device, context, "BlackHoleRoom\\BlackHoleRoom.sdkmesh");
            this.g_holeMesh = SdkMeshFile.FromFile(device, context, "BlackHoleRoom\\BlackHole.sdkmesh");
            this.g_mBlackHole = XMMatrix.Identity;

            // Initialize the balls
            for (int i = 0; i < MaxBalls; i++)
            {
                this.g_balls[i].dStartTime = -1.0;
            }

            // Setup the Lights
            for (int i = 0; i < this.g_lights.Length; i++)
            {
                this.g_lights[i] = new SceneLight();
            }

            int iLight = 0;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (x != 1 || y != 1)
                    {
                        this.g_lights[iLight].Position = new XMFloat4(3.0f * (-1.0f + x), 5.65f, 5.0f * (-1.0f + y), 1);

                        if (0 == iLight % 2)
                        {
                            this.g_lights[iLight].Diffuse = new XMFloat4(0.20f, 0.20f, 0.20f, 1.0f);
                            this.g_lights[iLight].Specular = new XMFloat4(0.5f, 0.5f, 0.5f, 1.0f);
                            this.g_lights[iLight].Ambient = new XMFloat4(0.03f, 0.03f, 0.03f, 0.0f);
                        }
                        else
                        {
                            this.g_lights[iLight].Diffuse = new XMFloat4(0.0f, 0.15f, 0.20f, 1.0f);
                            this.g_lights[iLight].Specular = new XMFloat4(0.15f, 0.25f, 0.3f, 1.0f);
                            this.g_lights[iLight].Ambient = new XMFloat4(0.00f, 0.02f, 0.03f, 0.0f);
                        }

                        this.g_lights[iLight].Atten.X = 1.0f;

                        iLight++;
                    }
                }
            }

            this.g_mLightProj = XMMatrix.PerspectiveFovLH(XMMath.ConvertToRadians(90.0f), 1.0f, 0.1f, 100.0f);

            this.g_VSScenemain = device.CreateVertexShader(File.ReadAllBytes("VSScenemain.cso"), null);
            this.g_PSScenemain = device.CreatePixelShader(File.ReadAllBytes("PSScenemain.cso"), null);
            this.g_GSFlatmain = device.CreateGeometryShader(File.ReadAllBytes("GSFlatmain.cso"), null);
            this.g_GSPointmain = device.CreateGeometryShader(File.ReadAllBytes("GSPointmain.cso"), null);
            this.g_VSScreenSpacemain = device.CreateVertexShader(File.ReadAllBytes("VSScreenSpacemain.cso"), null);
            this.g_PSAlphaTestmain = device.CreatePixelShader(File.ReadAllBytes("PSAlphaTestmain.cso"), null);

            this.g_samLinear = device.CreateSamplerState(new D3D11SamplerDesc(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                0.0f,
                1,
                D3D11ComparisonFunction.Never,
                null,
                float.MinValue,
                float.MaxValue));

            this.g_EnableDepth = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                    true,
                    D3D11DepthWriteMask.All,
                    D3D11ComparisonFunction.Less,
                    false,
                    0xff,
                    0xff,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11ComparisonFunction.Always,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11ComparisonFunction.Always));

            this.g_DisableDepth = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                    false,
                    D3D11DepthWriteMask.Zero,
                    D3D11ComparisonFunction.Less,
                    false,
                    0xff,
                    0xff,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11ComparisonFunction.Always,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11StencilOperation.Keep,
                    D3D11ComparisonFunction.Always));
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.constantBufferPerView);
            D3D11Utils.DisposeAndNull(ref this.constantBufferPerFrame);
            D3D11Utils.DisposeAndNull(ref this.g_pScreenQuadVB);
            D3D11Utils.DisposeAndNull(ref this.g_pVertexLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pScreenTexRV);
            D3D11Utils.DisposeAndNull(ref this.g_pProjectedTexRV);

            this.g_ballMesh?.Release();
            this.g_ballMesh = null;
            this.g_roomMesh?.Release();
            this.g_roomMesh = null;
            this.g_holeMesh?.Release();
            this.g_holeMesh = null;

            D3D11Utils.DisposeAndNull(ref this.g_VSScenemain);
            D3D11Utils.DisposeAndNull(ref this.g_PSScenemain);
            D3D11Utils.DisposeAndNull(ref this.g_GSFlatmain);
            D3D11Utils.DisposeAndNull(ref this.g_GSPointmain);
            D3D11Utils.DisposeAndNull(ref this.g_VSScreenSpacemain);
            D3D11Utils.DisposeAndNull(ref this.g_PSAlphaTestmain);

            D3D11Utils.DisposeAndNull(ref this.g_samLinear);
            D3D11Utils.DisposeAndNull(ref this.g_EnableDepth);
            D3D11Utils.DisposeAndNull(ref this.g_DisableDepth);
        }

        public void CreateWindowSizeDependentResources()
        {
            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionTransform = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 100.0f);

            float fWidth = this.deviceResources.BackBufferWidth;
            float fHeight = this.deviceResources.BackBufferHeight;

            ConstantBufferPerViewData cb = new()
            {
                // Set the viewport width/height
                g_viewportWidth = fWidth,
                g_viewportHeight = fHeight,
                g_nearPlane = 0.1f
            };

            context.UpdateSubresource(this.constantBufferPerView, 0, null, cb, 0, 0);

            // Update our Screen-space quad
            SceneVertex[] aVerts = new SceneVertex[]
            {
                new SceneVertex(new XMFloat3( 0, 0, 0.5f ), new XMFloat3( 0, 0, 0 ), new XMFloat2( 0, 0 ) ),
                new SceneVertex(new XMFloat3( fWidth, 0, 0.5f ), new XMFloat3( 0, 0, 0 ), new XMFloat2( 1, 0 ) ),
                new SceneVertex(new XMFloat3( 0, fHeight, 0.5f ), new XMFloat3( 0, 0, 0 ), new XMFloat2( 0, 1 ) ),
                new SceneVertex(new XMFloat3( fWidth, fHeight, 0.5f ), new XMFloat3( 0, 0, 0 ), new XMFloat2( 1, 1 ) ),
            };

            context.UpdateSubresource(this.g_pScreenQuadVB, 0, null, aVerts, 0, 0);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            float fBlackHoleRads = (float)timer.TotalSeconds * XMMath.ConvertToRadians(this.g_fRotateSpeed);
            this.g_mBlackHole = XMMatrix.RotationY(fBlackHoleRads);

            // Rotate the clip planes to align with the black holes
            this.g_clipplanes[0] = new XMVector(0, 1.0f, 0, -0.8f);
            XMVector v3Plane1 = new(0.707f, 0.707f, 0, 0.0f);
            XMVector v3Plane2 = new(-0.707f, 0.707f, 0, 0.0f);
            this.g_clipplanes[1] = XMVector3.TransformNormal(v3Plane1, this.g_mBlackHole);
            this.g_clipplanes[2] = XMVector3.TransformNormal(v3Plane2, this.g_mBlackHole);
            this.g_clipplanes[1].W = 0.70f;
            this.g_clipplanes[2].W = 0.70f;

            XMVector ballLaunch = new(2.1f, 8.1f, 0, 0.0f);
            XMVector ballStart = new(0, 0.45f, 0, 0.0f);
            XMVector ballGravity = new(0, -9.8f, 0, 0.0f);
            XMVector ballNow;

            float fBall_Life = 3.05f / ballLaunch.X;

            // Move existing balls
            for (int i = 0; i < MaxBalls; i++)
            {
                float T = (float)(timer.TotalSeconds - this.g_balls[i].dStartTime);
                // Live 1/2 second longer to fully show off clipping
                if (T < fBall_Life + 0.5f)
                {
                    // Use the equation X = Xo + VoT + 1/2AT^2
                    ballNow = ballStart + this.g_balls[i].velStart.ToVector() * T + 0.5f * ballGravity * T * T;

                    // Create a world matrix
                    this.g_balls[i].mWorld = XMMatrix.Translation(ballNow.X, ballNow.Y, ballNow.Z);
                }
                else
                {
                    this.g_balls[i].dStartTime = -1.0;
                }
            }

            // Launch a ball if it's time
            XMMatrix wLaunchMatrix;
            bool bFound = false;

            if ((timer.TotalSeconds - this.g_dLastLaunch) > this.g_fLaunchInterval)
            {
                for (int i = 0; i < MaxBalls && !bFound; i++)
                {
                    if (this.g_balls[i].dStartTime < 0.0)
                    {
                        // Found a free ball
                        this.g_balls[i].dStartTime = timer.TotalSeconds;
                        wLaunchMatrix = XMMatrix.RotationY(
                            (i % 2) * XMMath.ConvertToRadians(180.0f)
                            + fBlackHoleRads
                            + XMMath.ConvertToRadians(fBall_Life * this.g_fRotateSpeed));

                        this.g_balls[i].velStart = XMVector3.TransformNormal(ballLaunch, wLaunchMatrix);
                        this.g_balls[i].mWorld = XMMatrix.Translation(ballStart.X, ballStart.Y, ballStart.Z);

                        bFound = true;
                    }
                }

                this.g_dLastLaunch = timer.TotalSeconds;
            }

            // Rotate the cookie matrix
            XMMatrix mLightRot = XMMatrix.RotationY(XMMath.ConvertToRadians(50.0f) * (float)timer.TotalSeconds);
            XMFloat3 vLightEye = new(0, 5.65f, 0);
            XMFloat3 vLightAt = new(0, 0, 0);
            XMFloat3 vUp = new(0, 0, 1);
            this.g_mLightView = mLightRot * XMMatrix.LookAtLH(vLightEye, vLightAt, vUp);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.0f, 1.0f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            ConstantBufferPerFrameData cb = new()
            {
                g_clipplanes = this.g_clipplanes,
                g_lights = this.g_lights,
                g_pointSize = 3.0f,
                g_fogMode = (int)FogMode.Linear,
                g_fogStart = 12.0f,
                g_fogEnd = 22.0f,
                g_fogDensity = 0.05f,
                g_fogColor = new XMVector(0.7f, 1.0f, 1.0f, 1.0f)
            };

            XMMatrix mView = this.ViewTransform;
            XMMatrix mProj = this.ProjectionTransform;
            XMMatrix mInvProj = mProj.Inverse();
            XMMatrix mLightViewProj = this.g_mLightView * this.g_mLightProj;

            cb.g_mWorld = XMMatrix.Identity;
            cb.g_mView = mView.Transpose();
            cb.g_mProj = mProj.Transpose();
            cb.g_mInvProj = mInvProj.Transpose();
            cb.g_mLightViewProj = mLightViewProj.Transpose();

            context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cb, 0, 0);

            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBufferPerView, this.constantBufferPerFrame });
            context.GeometryShaderSetConstantBuffers(0, new[] { this.constantBufferPerView, this.constantBufferPerFrame });
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBufferPerView, this.constantBufferPerFrame });

            context.PixelShaderSetShaderResources(0, new[] { null, this.g_pProjectedTexRV });
            context.InputAssemblerSetInputLayout(this.g_pVertexLayout);

            cb.g_bEnableClipping = false;
            cb.g_bEnableLighting = false;
            context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cb, 0, 0);
            this.RenderSceneGouraud();
            this.g_roomMesh.Render(0, -1, -1);
            cb.g_mWorld = this.g_mBlackHole.Transpose();
            context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cb, 0, 0);
            this.g_holeMesh.Render(0, -1, -1);

            cb.g_bEnableClipping = true;
            cb.g_bEnableLighting = true;

            for (int i = 0; i < MaxBalls; i++)
            {
                if (this.g_balls[i].dStartTime > -1.0)
                {
                    cb.g_mWorld = this.g_balls[i].mWorld.Transpose();
                    context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cb, 0, 0);

                    if (i % 3 == 0)
                    {
                        this.RenderSceneGouraud();
                        this.g_ballMesh.Render(0, -1, -1);
                    }
                    else if (i % 3 == 1)
                    {
                        this.RenderSceneFlat();
                        this.g_ballMesh.Render(0, -1, -1);
                    }
                    else
                    {
                        this.RenderScenePoint();
                        this.g_ballMesh.Render(0, -1, -1);
                    }
                }
            }

            cb.g_bEnableClipping = false;
            cb.g_bEnableLighting = false;
            context.UpdateSubresource(this.constantBufferPerFrame, 0, null, cb, 0, 0);
            this.RenderScreenQuad();
        }

        /// <summary>
        /// Render a screen quad
        /// </summary>
        private void RenderScreenQuad()
        {
            var context = this.deviceResources.D3DContext;

            this.RenderScreneSpace();

            context.PixelShaderSetShaderResources(0, new[] { this.g_pScreenTexRV, null });

            uint[] uStride = new[] { SceneVertex.Size };
            uint[] offsets = new[] { 0U };
            D3D11Buffer[] pBuffers = new[] { this.g_pScreenQuadVB };
            context.InputAssemblerSetVertexBuffers(0, pBuffers, uStride, offsets);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);

            context.Draw(4, 0);
        }

        /// <summary>
        /// RenderSceneGouraud - renders gouraud-shaded primitives
        /// </summary>
        private void RenderSceneGouraud()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(this.g_VSScenemain, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(this.g_PSScenemain, null);
            context.PixelShaderSetSamplers(0, new[] { this.g_samLinear });
            context.OutputMergerSetDepthStencilState(this.g_EnableDepth, 0);
        }

        /// <summary>
        /// RenderSceneFlat - renders flat-shaded primitives
        /// </summary>
        private void RenderSceneFlat()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(this.g_VSScenemain, null);
            context.GeometryShaderSetShader(this.g_GSFlatmain, null);
            context.PixelShaderSetShader(this.g_PSScenemain, null);
            context.PixelShaderSetSamplers(0, new[] { this.g_samLinear });
            context.OutputMergerSetDepthStencilState(this.g_EnableDepth, 0);
        }

        /// <summary>
        /// RenderScenePoint - replaces d3dfill_point
        /// </summary>
        private void RenderScenePoint()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(this.g_VSScenemain, null);
            context.GeometryShaderSetShader(this.g_GSPointmain, null);
            context.PixelShaderSetShader(this.g_PSScenemain, null);
            context.PixelShaderSetSamplers(0, new[] { this.g_samLinear });
            context.OutputMergerSetDepthStencilState(this.g_EnableDepth, 0);
        }

        /// <summary>
        /// RenderScreneSpace - shows how to render something in screenspace
        /// </summary>
        private void RenderScreneSpace()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(this.g_VSScreenSpacemain, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(this.g_PSAlphaTestmain, null);
            context.PixelShaderSetSamplers(0, new[] { this.g_samLinear });
            context.OutputMergerSetDepthStencilState(this.g_DisableDepth, 0);
        }
    }
}
