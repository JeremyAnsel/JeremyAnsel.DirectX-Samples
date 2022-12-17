using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System.IO;

namespace ShadowVolume10
{
    class MainGameComponent : IGameComponent
    {
        public const int MaxNumLights = 6;

        public const int MaxNumBackgrounds = 2;

        public const float ExtrudeEpsilon = 0.1f;

        private static readonly XMFloat4[] g_vShadowColor = new XMFloat4[MaxNumLights]
        {
            new XMFloat4( 0.0f, 1.0f, 0.0f, 0.2f ),
            new XMFloat4( 1.0f, 1.0f, 0.0f, 0.2f ),
            new XMFloat4( 1.0f, 0.0f, 0.0f, 0.2f ),
            new XMFloat4(0.0f, 0.0f, 1.0f, 0.2f),
            new XMFloat4(1.0f, 0.0f, 1.0f, 0.2f),
            new XMFloat4(0.0f, 1.0f, 1.0f, 0.2f)
        };

        private static readonly LightInitData[] g_LightInit = new LightInitData[MaxNumLights]
        {
            new LightInitData( new XMFloat3( -2.0f, 3.0f, -3.0f ), new XMFloat4( 10.0f, 10.0f, 10.0f, 1.0f ) ),
            new LightInitData( new XMFloat3( 2.0f, 3.0f, -3.0f ), new XMFloat4( 10.0f, 10.0f, 10.0f, 1.0f ) ),
            new LightInitData( new XMFloat3( -2.0f, 3.0f, 3.0f ), new XMFloat4( 10.0f, 10.0f, 10.0f, 1.0f ) ),
            new LightInitData( new XMFloat3( 2.0f, 3.0f, 3.0f ), new XMFloat4( 10.0f, 10.0f, 10.0f, 1.0f ) ),
            new LightInitData( new XMFloat3( -2.0f, 3.0f, 0.0f ), new XMFloat4( 10.0f, 0.0f, 0.0f, 1.0f ) ),
            new LightInitData( new XMFloat3( 2.0f, 3.0f, 0.0f ), new XMFloat4( 0.0f, 0.0f, 10.0f, 1.0f ) )
        };

        private DeviceResources deviceResources;

        // Scaling to apply to mesh
        private XMMatrix worldScaling;

        // Background matrix
        private readonly XMMatrix[] worldBackground = new XMMatrix[MaxNumBackgrounds];

        // Light objects
        private readonly LightData[] lights = new LightData[MaxNumLights];

        private readonly XMFloat4 ambient = new(0.1f, 0.1f, 0.1f, 1.0f);

        // Background mesh
        private readonly SdkMeshFile[] backgrounds = new SdkMeshFile[MaxNumBackgrounds];

        // The mesh object
        private SdkMeshFile mesh;

        private D3D11Buffer constantBuffer;

        private D3D11GeometryShader GSShadowmain;

        private D3D11PixelShader PSAmbientmain;

        private D3D11PixelShader PSScenemain;

        private D3D11PixelShader PSShadowmain;

        private D3D11VertexShader VSScenemain;

        private D3D11VertexShader VSShadowmain;

        private D3D11InputLayout inputLayout;

        private D3D11SamplerState sampler;

        private D3D11BlendState DisableFrameBufferBlendState;

        private D3D11BlendState NoBlendingBlendState;

        private D3D11BlendState AdditiveBlendingBlendState;

        private D3D11BlendState SrcAlphaBlendingBlendState;

        private D3D11DepthStencilState EnableDepthDepthStencilState;

        private D3D11DepthStencilState TwoSidedStencilDepthStencilState;

        private D3D11DepthStencilState RenderNonShadowsDepthStencilState;

        private D3D11RasterizerState DisableCullingRasterizerState;

        private D3D11RasterizerState EnableCullingRasterizerState;

        public MainGameComponent()
        {
            // Initialize the lights
            for (int L = 0; L < MaxNumLights; L++)
            {
                this.lights[L].Position = g_LightInit[L].Position;
                this.lights[L].Color = g_LightInit[L].Color;
            }

            // Initialize the scaling and translation for the background meshes
            // Hardcode the matrices since we only have two.
            this.worldBackground[0] = XMMatrix.Translation(0.0f, 2.0f, 0.0f);
            this.worldBackground[1] = XMMatrix.Scaling(0.3f, 0.3f, 0.3f) * XMMatrix.Translation(0.0f, 1.5f, 0.0f);
            this.worldScaling = XMMatrix.Identity;

            // Setup the camera's view parameters
            this.LightTransform = XMMatrix.Identity;
            this.WorldTransform = XMMatrix.Identity;
            XMVector vecEye = new(0.0f, 1.0f, -5.0f, 0.0f);
            XMVector vecAt = new(0.0f, 0.0f, 0.0f, 0.0f);
            XMVector vecUp = new(0.0f, 1.0f, 0.0f, 0.0f);
            this.ViewTransform = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        private int currentBackground = 0;

        public int CurrentBackground
        {
            get
            {
                return this.currentBackground;
            }

            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value >= MaxNumBackgrounds)
                {
                    value = MaxNumBackgrounds - 1;
                }

                this.currentBackground = value;
            }
        }

        public RenderType RenderType { get; set; } = RenderType.Scene;

        private int numLights = 2;

        public int NumLights
        {
            get
            {
                return this.numLights;
            }

            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                else if (value > MaxNumLights)
                {
                    value = MaxNumLights;
                }

                this.numLights = value;
            }
        }

        public XMMatrix LightTransform { get; set; }

        public XMMatrix WorldTransform { get; set; }

        public XMMatrix ViewTransform { get; set; }

        public XMMatrix ProjectionTransform { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            this.backgrounds[0] = SdkMeshFile.FromFile(device, context, "Cell\\cell.sdkmesh");
            this.backgrounds[1] = SdkMeshFile.FromFile(device, context, "Seafloor\\seafloor.sdkmesh");
            this.mesh = SdkMeshFile.FromFile(device, context, "Dwarf\\dwarf.sdkmesh");

            ComputeMeshScaling(this.mesh, out this.worldScaling);

            this.constantBuffer = device.CreateBuffer(new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));

            this.GSShadowmain = device.CreateGeometryShader(File.ReadAllBytes("GSShadowmain.cso"), null);
            this.PSAmbientmain = device.CreatePixelShader(File.ReadAllBytes("PSAmbientmain.cso"), null);
            this.PSScenemain = device.CreatePixelShader(File.ReadAllBytes("PSScenemain.cso"), null);
            this.PSShadowmain = device.CreatePixelShader(File.ReadAllBytes("PSShadowmain.cso"), null);
            this.VSScenemain = device.CreateVertexShader(File.ReadAllBytes("VSScenemain.cso"), null);
            this.VSShadowmain = device.CreateVertexShader(File.ReadAllBytes("VSShadowmain.cso"), null);

            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
            };

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, File.ReadAllBytes("VSScenemain.cso"));

            D3D11SamplerDesc samplerDesc = new(
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

            this.sampler = device.CreateSamplerState(samplerDesc);

            var DisableFrameBufferBlendDesc = D3D11BlendDesc.Default;
            var DisableFrameBufferBlendDescRenderTargets = DisableFrameBufferBlendDesc.GetRenderTargets();
            DisableFrameBufferBlendDescRenderTargets[0].IsBlendEnabled = false;
            DisableFrameBufferBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.None;
            DisableFrameBufferBlendDesc.SetRenderTargets(DisableFrameBufferBlendDescRenderTargets);
            this.DisableFrameBufferBlendState = device.CreateBlendState(DisableFrameBufferBlendDesc);

            var NoBlendingBlendDesc = D3D11BlendDesc.Default;
            NoBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var NoBlendingBlendDescRenderTargets = NoBlendingBlendDesc.GetRenderTargets();
            NoBlendingBlendDescRenderTargets[0].IsBlendEnabled = false;
            NoBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            NoBlendingBlendDesc.SetRenderTargets(NoBlendingBlendDescRenderTargets);
            this.NoBlendingBlendState = device.CreateBlendState(NoBlendingBlendDesc);

            var AdditiveBlendingBlendDesc = D3D11BlendDesc.Default;
            AdditiveBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var AdditiveBlendingBlendDescRenderTargets = AdditiveBlendingBlendDesc.GetRenderTargets();
            AdditiveBlendingBlendDescRenderTargets[0].IsBlendEnabled = true;
            AdditiveBlendingBlendDescRenderTargets[0].SourceBlend = D3D11BlendValue.One;
            AdditiveBlendingBlendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.One;
            AdditiveBlendingBlendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            AdditiveBlendingBlendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            AdditiveBlendingBlendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            AdditiveBlendingBlendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            AdditiveBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            AdditiveBlendingBlendDesc.SetRenderTargets(AdditiveBlendingBlendDescRenderTargets);
            this.AdditiveBlendingBlendState = device.CreateBlendState(AdditiveBlendingBlendDesc);

            var SrcAlphaBlendingBlendDesc = D3D11BlendDesc.Default;
            SrcAlphaBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var SrcAlphaBlendingBlendDescRenderTargets = SrcAlphaBlendingBlendDesc.GetRenderTargets();
            SrcAlphaBlendingBlendDescRenderTargets[0].IsBlendEnabled = true;
            SrcAlphaBlendingBlendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            SrcAlphaBlendingBlendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            SrcAlphaBlendingBlendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            SrcAlphaBlendingBlendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            SrcAlphaBlendingBlendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            SrcAlphaBlendingBlendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            SrcAlphaBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            SrcAlphaBlendingBlendDesc.SetRenderTargets(SrcAlphaBlendingBlendDescRenderTargets);
            this.SrcAlphaBlendingBlendState = device.CreateBlendState(SrcAlphaBlendingBlendDesc);

            var EnableDepthDepthStencilDesc = D3D11DepthStencilDesc.Default;
            EnableDepthDepthStencilDesc.IsDepthEnabled = true;
            EnableDepthDepthStencilDesc.DepthWriteMask = D3D11DepthWriteMask.All;
            this.EnableDepthDepthStencilState = device.CreateDepthStencilState(EnableDepthDepthStencilDesc);

            this.TwoSidedStencilDepthStencilState = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                true,
                D3D11DepthWriteMask.Zero,
                D3D11ComparisonFunction.Less,
                true,
                0xff,
                0xff,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Decrement,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Always,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Increment,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Always));

            this.RenderNonShadowsDepthStencilState = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                true,
                D3D11DepthWriteMask.Zero,
                D3D11ComparisonFunction.LessEqual,
                true,
                0xff,
                0,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Equal,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Zero,
                D3D11ComparisonFunction.Never));

            var DisableCullingRasterizerDesc = D3D11RasterizerDesc.Default;
            DisableCullingRasterizerDesc.CullMode = D3D11CullMode.None;
            this.DisableCullingRasterizerState = device.CreateRasterizerState(DisableCullingRasterizerDesc);

            var EnableCullingRasterizerDesc = D3D11RasterizerDesc.Default;
            EnableCullingRasterizerDesc.CullMode = D3D11CullMode.Back;
            this.EnableCullingRasterizerState = device.CreateRasterizerState(EnableCullingRasterizerDesc);
        }

        public void ReleaseDeviceDependentResources()
        {
            for (int i = 0; i < this.backgrounds.Length; i++)
            {
                this.backgrounds[i]?.Release();
                this.backgrounds[i] = null;
            }

            this.mesh?.Release();
            this.mesh = null;

            D3D11Utils.DisposeAndNull(ref this.constantBuffer);

            D3D11Utils.DisposeAndNull(ref this.GSShadowmain);
            D3D11Utils.DisposeAndNull(ref this.PSAmbientmain);
            D3D11Utils.DisposeAndNull(ref this.PSScenemain);
            D3D11Utils.DisposeAndNull(ref this.PSShadowmain);
            D3D11Utils.DisposeAndNull(ref this.VSScenemain);
            D3D11Utils.DisposeAndNull(ref this.VSShadowmain);

            D3D11Utils.DisposeAndNull(ref this.inputLayout);

            D3D11Utils.DisposeAndNull(ref this.sampler);

            D3D11Utils.DisposeAndNull(ref this.DisableFrameBufferBlendState);
            D3D11Utils.DisposeAndNull(ref this.NoBlendingBlendState);
            D3D11Utils.DisposeAndNull(ref this.AdditiveBlendingBlendState);
            D3D11Utils.DisposeAndNull(ref this.SrcAlphaBlendingBlendState);

            D3D11Utils.DisposeAndNull(ref this.EnableDepthDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.TwoSidedStencilDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.RenderNonShadowsDepthStencilState);

            D3D11Utils.DisposeAndNull(ref this.DisableCullingRasterizerState);
            D3D11Utils.DisposeAndNull(ref this.EnableCullingRasterizerState);
        }

        /// <summary>
        /// Compute a matrix that scales Mesh to a specified size and centers around origin.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="scalingCenter"></param>
        private static void ComputeMeshScaling(SdkMeshFile mesh, out XMMatrix scalingCenter)
        {
            XMVector max = XMVector.Replicate(float.MinValue);
            XMVector min = XMVector.Replicate(float.MaxValue);

            for (int i = 0; i < mesh.Meshes.Count; i++)
            {
                XMVector newMax = mesh.Meshes[i].BoundingBoxCenter.ToVector() + mesh.Meshes[i].BoundingBoxExtents.ToVector();
                XMVector newMin = mesh.Meshes[i].BoundingBoxCenter.ToVector() - mesh.Meshes[i].BoundingBoxExtents.ToVector();

                max = XMVector.Max(max, newMax);
                min = XMVector.Min(min, newMin);
            }

            XMVector vCtr = (max + min) / 2.0f;
            XMVector vExtents = max - vCtr;
            float reciprocalRadius = XMVector3.ReciprocalLength(vExtents).X;

            scalingCenter = XMMatrix.Translation(-vCtr.X, -vCtr.Y, -vCtr.Z) * XMMatrix.Scaling(reciprocalRadius, reciprocalRadius, reciprocalRadius);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionTransform = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 5000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
        }

        private void UpdateLights(ref ConstantBufferData cb, int iLight)
        {
            XMVector vLight = new(this.lights[iLight].Position.X, this.lights[iLight].Position.Y, this.lights[iLight].Position.Z, 1.0f);
            vLight = XMVector4.Transform(vLight, this.LightTransform);

            cb.vLightPosition = vLight;
            cb.vLightColor = this.lights[iLight].Color;
        }

        private void RenderScene(
            ref ConstantBufferData cb,
            D3D11VertexShader vs,
            D3D11GeometryShader gs,
            D3D11PixelShader ps,
            D3D11BlendState blendState,
            D3D11DepthStencilState depthStencilState,
            uint stencilReference,
            D3D11RasterizerState rasterizerState,
            bool renderBackground)
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.GeometryShaderSetShader(gs, null);
            context.GeometryShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetShader(ps, null);
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            cb.mViewProjection = (this.ViewTransform * this.ProjectionTransform).Transpose();

            // Render background
            if (renderBackground)
            {
                cb.mWorld = this.worldBackground[this.currentBackground].Transpose();
                cb.mWorldViewProjection = (this.worldBackground[this.currentBackground] * this.ViewTransform * this.ProjectionTransform).Transpose();

                context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);
                this.backgrounds[this.currentBackground].Render(0, -1, -1);
            }

            // Render object
            cb.mWorld = (this.worldScaling * this.WorldTransform).Transpose();
            cb.mWorldViewProjection = (this.worldScaling * this.WorldTransform * this.ViewTransform * this.ProjectionTransform).Transpose();

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);
            this.mesh.Render(0, -1, -1);

            this.worldScaling.Decompose(out XMVector scale, out _, out _);
            XMMatrix world;

            world = XMMatrix.Multiply(this.worldScaling, XMMatrix.Translation(scale.X * -2, 0, 0));
            cb.mWorld = (world * this.WorldTransform).Transpose();
            cb.mWorldViewProjection = (world * this.WorldTransform * this.ViewTransform * this.ProjectionTransform).Transpose();
            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);
            this.mesh.Render(0, -1, -1);

            world = XMMatrix.Multiply(this.worldScaling, XMMatrix.Translation(scale.X * 2, 0, 0));
            cb.mWorld = (world * this.WorldTransform).Transpose();
            cb.mWorldViewProjection = (world * this.WorldTransform * this.ViewTransform * this.ProjectionTransform).Transpose();
            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);
            this.mesh.Render(0, -1, -1);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Set the Vertex Layout
            context.InputAssemblerSetInputLayout(this.inputLayout);

            ConstantBufferData cb = default;

            // Render the scene ambient - renders the scene with ambient lighting
            cb.vAmbient = this.ambient;

            this.RenderScene(
                ref cb,
                this.VSScenemain,
                null,
                this.PSAmbientmain,
                this.NoBlendingBlendState,
                this.EnableDepthDepthStencilState,
                0U,
                this.EnableCullingRasterizerState,
                true);

            cb.fExtrudeAmt = 120.0f - ExtrudeEpsilon;
            cb.fExtrudeBias = ExtrudeEpsilon;

            for (int l = 0; l < this.NumLights; l++)
            {
                this.UpdateLights(ref cb, l);

                // Clear the stencil
                context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Stencil, 1.0f, 0);

                // Render the shadow volume - extrudes shadows from geometry
                if (this.RenderType == RenderType.ShadowVolume)
                {
                    cb.vShadowColor = g_vShadowColor[l];

                    this.RenderScene(
                        ref cb,
                        this.VSShadowmain,
                        this.GSShadowmain,
                        this.PSShadowmain,
                        this.SrcAlphaBlendingBlendState,
                        this.TwoSidedStencilDepthStencilState,
                        1U,
                        this.DisableCullingRasterizerState,
                        false);
                }
                else
                {
                    this.RenderScene(
                        ref cb,
                        this.VSShadowmain,
                        this.GSShadowmain,
                        this.PSShadowmain,
                        this.DisableFrameBufferBlendState,
                        this.TwoSidedStencilDepthStencilState,
                        1U,
                        this.DisableCullingRasterizerState,
                        false);
                }

                // Render the lit scene - renders textured primitives
                this.RenderScene(
                    ref cb,
                    this.VSScenemain,
                    null,
                    this.PSScenemain,
                    this.AdditiveBlendingBlendState,
                    this.RenderNonShadowsDepthStencilState,
                    0U,
                    this.EnableCullingRasterizerState,
                    true);
            }
        }
    }
}
