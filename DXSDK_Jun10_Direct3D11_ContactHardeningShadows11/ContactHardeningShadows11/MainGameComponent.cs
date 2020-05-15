using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ContactHardeningShadows11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private const float g_fShadowMapWidth = 1024.0f;

        private const float g_fShadowMapHeight = 1024.0f;

        private const uint g_iConstantsConstantBufferBind = 0;

        private D3D11VertexShader g_pSceneVS;

        private D3D11InputLayout g_pSceneVertexLayout;

        private D3D11PixelShader g_pScenePS;

        private D3D11VertexShader g_pShadowMapVS;

        private SdkMeshFile g_SceneMesh;

        private SdkMeshFile g_Poles;

        private D3D11Buffer g_pcbConstants;

        private D3D11SamplerState g_pSamplePoint;

        private D3D11SamplerState g_pSampleLinear;

        private D3D11SamplerState g_pSamplePointCmp;

        private D3D11BlendState g_pBlendStateNoBlend;

        private D3D11BlendState g_pBlendStateColorWritesOff;

        // SM depth stencil texture data
        private D3D11Texture2D g_pShadowMapDepthStencilTexture;

        // SRV of the SM
        private D3D11ShaderResourceView g_pDepthTextureSRV;

        // depth stencil view of the SM
        private D3D11DepthStencilView g_pDepthStencilTextureDSV;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel110;
            }
        }

        public float SunWidth { get; set; } = 2.0f;

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public XMMatrix LightWorldMatrix { get; set; }

        public XMMatrix LightViewMatrix { get; set; }

        public XMMatrix LightProjectionMatrix { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            byte[] renderSceneVertexShaderBytecode = File.ReadAllBytes("RenderSceneVertexShader.cso");
            this.g_pSceneVS = this.deviceResources.D3DDevice.CreateVertexShader(renderSceneVertexShaderBytecode, null);

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
                    SemanticName = "TEXTURE",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 24,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.g_pSceneVertexLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, renderSceneVertexShaderBytecode);

            byte[] renderScenePixelShaderBytecode = File.ReadAllBytes("RenderScenePixelShader.cso");
            this.g_pScenePS = this.deviceResources.D3DDevice.CreatePixelShader(renderScenePixelShaderBytecode, null);

            byte[] renderSceneShadowMapVertexShaderBytecode = File.ReadAllBytes("RenderSceneShadowMapVertexShader.cso");
            this.g_pShadowMapVS = this.deviceResources.D3DDevice.CreateVertexShader(renderSceneShadowMapVertexShaderBytecode, null);

            this.g_SceneMesh = SdkMeshFile.FromFile(this.deviceResources.D3DDevice, this.deviceResources.D3DContext, @"ColumnScene\scene.sdkmesh");
            this.g_Poles = SdkMeshFile.FromFile(this.deviceResources.D3DDevice, this.deviceResources.D3DContext, @"ColumnScene\poles.sdkmesh");

            this.g_pcbConstants = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(ConstantBufferConstants.Size, D3D11BindOptions.ConstantBuffer));

            D3D11SamplerDesc samplerDesc = new D3D11SamplerDesc(
                D3D11Filter.ComparisonMinMagMipPoint,
                D3D11TextureAddressMode.Border,
                D3D11TextureAddressMode.Border,
                D3D11TextureAddressMode.Border,
                0.0f,
                1,
                D3D11ComparisonFunction.LessEqual,
                new float[] { 1.0f, 1.0f, 1.0f, 1.0f },
                0.0f,
                float.MaxValue);

            // PointCmp
            this.g_pSamplePointCmp = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            // Point
            samplerDesc.Filter = D3D11Filter.MinMagMipPoint;
            samplerDesc.ComparisonFunction = D3D11ComparisonFunction.Always;
            this.g_pSamplePoint = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            // Linear
            samplerDesc.Filter = D3D11Filter.MinMagMipLinear;
            samplerDesc.AddressU = D3D11TextureAddressMode.Wrap;
            samplerDesc.AddressV = D3D11TextureAddressMode.Wrap;
            samplerDesc.AddressW = D3D11TextureAddressMode.Wrap;
            this.g_pSampleLinear = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

            // Create a blend state to disable alpha blending
            D3D11BlendDesc blendDesc = D3D11BlendDesc.Default;
            blendDesc.IsIndependentBlendEnabled = false;
            D3D11RenderTargetBlendDesc[] blendDescRenderTargets = blendDesc.GetRenderTargets();
            blendDescRenderTargets[0].IsBlendEnabled = false;

            blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            this.g_pBlendStateNoBlend = this.deviceResources.D3DDevice.CreateBlendState(blendDesc);

            blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.None;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            this.g_pBlendStateColorWritesOff = this.deviceResources.D3DDevice.CreateBlendState(blendDesc);

            // textures / rts
            D3D11Texture2DDesc TDesc = new D3D11Texture2DDesc(
                DxgiFormat.R16Typeless,
                (uint)g_fShadowMapWidth,
                (uint)g_fShadowMapHeight,
                1,
                1,
                D3D11BindOptions.DepthStencil | D3D11BindOptions.ShaderResource);

            this.g_pShadowMapDepthStencilTexture = this.deviceResources.D3DDevice.CreateTexture2D(TDesc);

            D3D11ShaderResourceViewDesc SRVDesc = new D3D11ShaderResourceViewDesc
            {
                Format = DxgiFormat.R16UNorm,
                ViewDimension = D3D11SrvDimension.Texture2D,
                Texture2D = new D3D11Texture2DSrv
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };

            this.g_pDepthTextureSRV = this.deviceResources.D3DDevice.CreateShaderResourceView(this.g_pShadowMapDepthStencilTexture, SRVDesc);

            D3D11DepthStencilViewDesc DSVDesc = new D3D11DepthStencilViewDesc
            {
                Format = DxgiFormat.D16UNorm,
                ViewDimension = D3D11DsvDimension.Texture2D,
                Options = D3D11DepthStencilViewOptions.None,
                Texture2D = new D3D11Texture2DDsv
                {
                    MipSlice = 0
                }
            };

            this.g_pDepthStencilTextureDSV = this.deviceResources.D3DDevice.CreateDepthStencilView(this.g_pShadowMapDepthStencilTexture, DSVDesc);

            XMFloat3 vecEye = new XMFloat3(0.95f, 5.83f, -14.48f);
            XMFloat3 vecAt = new XMFloat3(0.90f, 5.44f, -13.56f);
            XMFloat3 vecUp = new XMFloat3(0.0f, 1.0f, 0.0f);
            this.ViewMatrix = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
            this.WorldMatrix = XMMatrix.Identity;

            XMFloat3 vecEyeL = new XMFloat3(0, 0, 0);
            XMFloat3 vecAtL = new XMFloat3(0, -0.5f, 1);
            XMFloat3 vecUUpL = new XMFloat3(0.0f, 1.0f, 0.0f);
            this.LightViewMatrix = XMMatrix.LookAtLH(vecEyeL, vecAtL, vecUUpL);
            this.LightWorldMatrix = XMMatrix.Identity;
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pShadowMapDepthStencilTexture);
            D3D11Utils.DisposeAndNull(ref this.g_pDepthTextureSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pDepthStencilTextureDSV);

            D3D11Utils.DisposeAndNull(ref this.g_pSamplePoint);
            D3D11Utils.DisposeAndNull(ref this.g_pSampleLinear);
            D3D11Utils.DisposeAndNull(ref this.g_pSamplePointCmp);
            D3D11Utils.DisposeAndNull(ref this.g_pBlendStateNoBlend);
            D3D11Utils.DisposeAndNull(ref this.g_pBlendStateColorWritesOff);

            D3D11Utils.DisposeAndNull(ref this.g_pcbConstants);

            this.g_SceneMesh?.Release();
            this.g_SceneMesh = null;

            this.g_Poles?.Release();
            this.g_Poles = null;

            D3D11Utils.DisposeAndNull(ref this.g_pSceneVS);
            D3D11Utils.DisposeAndNull(ref this.g_pSceneVertexLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pScenePS);
            D3D11Utils.DisposeAndNull(ref this.g_pShadowMapVS);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.5f, 100.0f);
            this.LightProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 10.0f, 100.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
        }

        private void RenderShadowMap(out XMFloat4X4 mViewProjLight, out XMFloat3 vLightDir)
        {
            var context = this.deviceResources.D3DContext;

            D3D11Rect[] oldRects = context.RasterizerStageGetScissorRects();
            D3D11Viewport[] oldVp = context.RasterizerStageGetViewports();

            D3D11Rect[] rects = new D3D11Rect[1] { new D3D11Rect(0, (int)g_fShadowMapWidth, 0, (int)g_fShadowMapHeight) };
            context.RasterizerStageSetScissorRects(rects);

            D3D11Viewport[] vp = new D3D11Viewport[1] { new D3D11Viewport(0, 0, g_fShadowMapWidth, g_fShadowMapHeight, 0.0f, 1.0f) };
            context.RasterizerStageSetViewports(vp);

            // Set our scene render target & keep original depth buffer
            D3D11RenderTargetView[] pRTVs = new D3D11RenderTargetView[2];
            context.OutputMergerSetRenderTargets(pRTVs, this.g_pDepthStencilTextureDSV);

            // Clear the render target
            context.ClearDepthStencilView(this.g_pDepthStencilTextureDSV, D3D11ClearOptions.Depth | D3D11ClearOptions.Stencil, 1.0f, 0);

            // Get the projection & view matrix from the camera class
            XMFloat3 up = new XMFloat3(0, 1, 0);
            XMFloat4 vLight = new XMFloat4(0.0f, 0.0f, 0.0f, 1.0f);
            XMFloat4 vLightLookAt = new XMFloat4(0.0f, -0.5f, 1.0f, 0.0f);
            vLightLookAt = vLight.ToVector() + vLightLookAt.ToVector();
            vLight = XMVector4.Transform(vLight, this.LightWorldMatrix);
            vLightLookAt = XMVector4.Transform(vLightLookAt, this.LightWorldMatrix);

            vLightDir = XMVector.Subtract(vLightLookAt, vLight);

            XMMatrix mProj = XMMatrix.OrthographicOffCenterLH(-8.5f, 9, -15, 11, -20, 20);
            XMMatrix mView = XMMatrix.LookAtLH(vLight, vLightLookAt, up);

            mViewProjLight = mView * mProj;

            // Setup the constant buffer for the scene vertex shader
            ConstantBufferConstants pConstants = new ConstantBufferConstants
            {
                WorldViewProjection = mViewProjLight.ToMatrix().Transpose(),
                WorldViewProjLight = mViewProjLight.ToMatrix().Transpose(),
                ShadowMapDimensions = new XMFloat4(
                    g_fShadowMapWidth,
                    g_fShadowMapHeight,
                    1.0f / g_fShadowMapWidth,
                    1.0f / g_fShadowMapHeight),
                LightDir = new XMFloat4(vLightDir.X, vLightDir.Y, vLightDir.Z, 0.0f),
                SunWidth = this.SunWidth
            };

            context.UpdateSubresource(this.g_pcbConstants, 0, null, pConstants, 0, 0);
            context.VertexShaderSetConstantBuffers(g_iConstantsConstantBufferBind, new[] { this.g_pcbConstants });
            context.PixelShaderSetConstantBuffers(g_iConstantsConstantBufferBind, new[] { this.g_pcbConstants });

            // Set the shaders
            context.VertexShaderSetShader(this.g_pShadowMapVS, null);
            context.PixelShaderSetShader(null, null);

            // Set the vertex buffer format
            context.InputAssemblerSetInputLayout(this.g_pSceneVertexLayout);

            // Render the scene
            this.g_Poles.Render(0, -1, -1);

            // reset the old viewport etc.
            context.RasterizerStageSetScissorRects(oldRects);
            context.RasterizerStageSetViewports(oldVp);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            D3D11RenderTargetView[] pRTV = new D3D11RenderTargetView[2];
            D3D11ShaderResourceView[] pSRV = new D3D11ShaderResourceView[8];

            // Array of our samplers
            D3D11SamplerState[] ppSamplerStates = new D3D11SamplerState[3] { this.g_pSamplePoint, this.g_pSampleLinear, this.g_pSamplePointCmp };
            context.PixelShaderSetSamplers(0, ppSamplerStates);

            // Store off original render target, this is the back buffer of the swap chain
            D3D11RenderTargetView pOrigRTV = this.deviceResources.D3DRenderTargetView;
            D3D11DepthStencilView pOrigDSV = this.deviceResources.D3DDepthStencilView;

            // Clear the render target
            float[] ClearColor = { 0.0f, 0.25f, 0.25f, 1.0f };
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, ClearColor);
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth | D3D11ClearOptions.Stencil, 1.0f, 0);

            // disable color writes
            context.OutputMergerSetBlendState(this.g_pBlendStateColorWritesOff, null, 0xffffffff);

            this.RenderShadowMap(out XMFloat4X4 mViewProjLight, out XMFloat3 vLightDir);

            // enable color writes
            context.OutputMergerSetBlendState(this.g_pBlendStateNoBlend, null, 0xffffffff);

            // Get the projection & view matrix from the camera class
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mWorldViewProjection = mView * mProj;

            // Setup the constant buffer for the scene vertex shader
            ConstantBufferConstants pConstants = new ConstantBufferConstants
            {
                WorldViewProjection = mWorldViewProjection.Transpose(),
                WorldViewProjLight = mViewProjLight.ToMatrix().Transpose(),
                ShadowMapDimensions = new XMFloat4(
                    g_fShadowMapWidth,
                    g_fShadowMapHeight,
                    1.0f / g_fShadowMapWidth,
                    1.0f / g_fShadowMapHeight),
                LightDir = new XMFloat4(vLightDir.X, vLightDir.Y, vLightDir.Z, 0.0f),
                SunWidth = this.SunWidth
            };

            context.UpdateSubresource(this.g_pcbConstants, 0, null, pConstants, 0, 0);
            context.VertexShaderSetConstantBuffers(g_iConstantsConstantBufferBind, new[] { this.g_pcbConstants });
            context.PixelShaderSetConstantBuffers(g_iConstantsConstantBufferBind, new[] { this.g_pcbConstants });

            // Set the shaders
            context.VertexShaderSetShader(this.g_pSceneVS, null);
            context.PixelShaderSetShader(this.g_pScenePS, null);

            // Set the vertex buffer format
            context.InputAssemblerSetInputLayout(this.g_pSceneVertexLayout);

            // Rebind to original back buffer and depth buffer
            pRTV[0] = pOrigRTV;
            context.OutputMergerSetRenderTargets(pRTV, pOrigDSV);

            // set the shadow map
            context.PixelShaderSetShaderResources(1, new[] { this.g_pDepthTextureSRV });

            // Render the scene
            this.g_SceneMesh.Render(0, -1, -1);
            this.g_Poles.Render(0, -1, -1);

            // restore resources
            context.PixelShaderSetShaderResources(0, pSRV);
        }
    }
}
