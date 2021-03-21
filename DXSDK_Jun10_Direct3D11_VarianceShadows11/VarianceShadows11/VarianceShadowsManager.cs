using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.D3DCompiler;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Globalization;
using System.IO;

namespace VarianceShadows11
{
    // This is where the shadows are calculated and rendered.
    // Initialize the Manager.  The manager performs all the work of caculating the render 
    // paramters of the shadow, creating the D3D resources, rendering the shadow, and rendering
    // the actual scene.
    class VarianceShadowsManager
    {
        private static readonly XMVector HalfVector = new XMVector(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly XMVector MultiplySetzwToZero = new XMVector(1.0f, 1.0f, 0.0f, 0.0f);

        private const string VsModel = "vs_4_0";
        private const string PsModel = "ps_4_0";

        public int CascadePartitionsMax;
        // Values are  between near and far
        private readonly float[] m_fCascadePartitionsFrustum = new float[CascadeConfig.MaxCascades];
        // Values are 0 to 100 and represent a percent of the frstum
        public readonly int[] CascadePartitionsZeroToOne = new int[CascadeConfig.MaxCascades];
        public int ShadowBlurSize;
        public bool BlurBetweenCascades;
        public float BlurBetweenCascadesAmount;

        public bool MoveLightTexelSize;
        public CameraSelection SelectedCamera;
        public FitProjection SelectedProjectionFit;
        public FitNearFar SelectedNearFarFit;
        public CascadeSelection SelectedCascadeSelection;
        public ShadowFilter ShadowFilter;

        private XMVector m_vSceneAABBMin;
        private XMVector m_vSceneAABBMax;
        private readonly XMMatrix[] m_matShadowProj = new XMMatrix[CascadeConfig.MaxCascades];
        private XMMatrix m_matShadowView;
        private CascadeConfig m_CopyOfCascadeConfig; // This copy is used to determine when settings change. Some of these settings require new buffer allocations.
        private CascadeConfig m_pCascadeConfig; // Pointer to the most recent setting.

        // D3D11 variables
        private D3D11VertexShader m_pvsQuadBlur;

        private readonly D3D11PixelShader[] m_ppsQuadBlurX = new D3D11PixelShader[CascadeConfig.MaxBlurLevels];
        private readonly D3D11PixelShader[] m_ppsQuadBlurY = new D3D11PixelShader[CascadeConfig.MaxBlurLevels];

        private D3D11VertexShader m_pvsRenderVarianceShadow;
        private D3D11PixelShader m_ppsRenderVarianceShadow;

        private D3D11InputLayout m_pVertexLayoutMesh;
        private readonly D3D11VertexShader[] m_pvsRenderScene = new D3D11VertexShader[CascadeConfig.MaxCascades];
        private readonly D3D11PixelShader[,,] m_ppsRenderSceneAllShaders = new D3D11PixelShader[CascadeConfig.MaxCascades, 2, 2];

        private D3D11Texture2D m_pCascadedShadowMapVarianceTextureArray;
        private readonly D3D11RenderTargetView[] m_pCascadedShadowMapVarianceRTVArrayAll = new D3D11RenderTargetView[CascadeConfig.MaxCascades];
        private readonly D3D11ShaderResourceView[] m_pCascadedShadowMapVarianceSRVArrayAll = new D3D11ShaderResourceView[CascadeConfig.MaxCascades];
        private D3D11ShaderResourceView m_pCascadedShadowMapVarianceSRVArraySingle;

        private D3D11Texture2D m_pTemporaryShadowDepthBufferTexture;
        private D3D11DepthStencilView m_pTemporaryShadowDepthBufferDSV;

        private D3D11Texture2D m_pCascadedShadowMapTempBlurTexture;
        private D3D11RenderTargetView m_pCascadedShadowMapTempBlurRTV;
        private D3D11ShaderResourceView m_pCascadedShadowMapTempBlurSRV;

        // All VS and PS constants are in the same buffer.
        // An actual title would break this up into multiple 
        // buffers updated based on frequency of variable changes
        private D3D11Buffer m_pcbGlobalConstantBuffer;

        private D3D11RasterizerState m_prsScene;
        private D3D11RasterizerState m_prsShadow;

        private readonly D3D11Viewport[] m_RenderVP = new D3D11Viewport[CascadeConfig.MaxCascades];
        private D3D11Viewport m_RenderOneTileVP;

        private D3D11SamplerState m_pSamLinear;
        private D3D11SamplerState m_pSamShadowPoint;
        private D3D11SamplerState m_pSamShadowLinear;
        private D3D11SamplerState m_pSamShadowAnisotropic16;
        private D3D11SamplerState m_pSamShadowAnisotropic8;
        private D3D11SamplerState m_pSamShadowAnisotropic4;
        private D3D11SamplerState m_pSamShadowAnisotropic2;

        private readonly MainGameSettings _settings;

        public VarianceShadowsManager(MainGameSettings settings)
        {
            this._settings = settings;

            this.BlurBetweenCascades = false;
            this.BlurBetweenCascadesAmount = 0.005f;
            this.m_RenderOneTileVP = this.m_RenderVP[0];
            this.ShadowBlurSize = 3;
            this.ShadowFilter = ShadowFilter.Anisotropic16x;

            this.m_CopyOfCascadeConfig = new CascadeConfig();

            for (int index = 0; index < CascadeConfig.MaxCascades; index++)
            {
                this.m_RenderVP[index].Height = this.m_CopyOfCascadeConfig.BufferSize;
                this.m_RenderVP[index].Width = this.m_CopyOfCascadeConfig.BufferSize;
                this.m_RenderVP[index].MaxDepth = 1.0f;
                this.m_RenderVP[index].MinDepth = 0.0f;
                this.m_RenderVP[index].TopLeftX = 0;
                this.m_RenderVP[index].TopLeftY = 0;
            }
        }

        public XMVector SceneAABBMin => m_vSceneAABBMin;

        public XMVector SceneAABBMax => m_vSceneAABBMax;

        public void InitScene(
            SdkMeshFile pMesh,
            CascadeConfig pCascadeConfig)
        {
            this.m_CopyOfCascadeConfig = pCascadeConfig.ShallowCopy();
            // Initialize m_iBufferSize to 0 to trigger a reallocate on the first frame.   
            this.m_CopyOfCascadeConfig.BufferSize = 0;
            // Save a pointer to cascade config.  Each frame we check our copy against the pointer.
            this.m_pCascadeConfig = pCascadeConfig;

            this.m_vSceneAABBMin = XMVector.Replicate(float.MaxValue);
            this.m_vSceneAABBMax = XMVector.Replicate(float.MinValue);

            // Calcaulte the AABB for the scene by iterating through all the meshes in the SDKMesh file.
            for (int i = 0; i < pMesh.Meshes.Count; i++)
            {
                SdkMeshMesh msh = pMesh.Meshes[i];

                XMVector vMeshMin = XMVector.FromFloat(
                    msh.BoundingBoxCenter.X - msh.BoundingBoxExtents.X,
                    msh.BoundingBoxCenter.Y - msh.BoundingBoxExtents.Y,
                    msh.BoundingBoxCenter.Z - msh.BoundingBoxExtents.Z,
                    1.0f);

                XMVector vMeshMax = XMVector.FromFloat(
                    msh.BoundingBoxCenter.X + msh.BoundingBoxExtents.X,
                    msh.BoundingBoxCenter.Y + msh.BoundingBoxExtents.Y,
                    msh.BoundingBoxCenter.Z + msh.BoundingBoxExtents.Z,
                    1.0f);

                this.m_vSceneAABBMin = XMVector.Min(vMeshMin, this.m_vSceneAABBMin);
                this.m_vSceneAABBMax = XMVector.Max(vMeshMax, this.m_vSceneAABBMax);
            }
        }

        // This runs when the application is initialized.
        public void Init(
            D3D11Device pd3dDevice,
            D3D11DeviceContext pd3dImmediateContext)
        {
            this.m_pvsRenderVarianceShadow = pd3dDevice.CreateVertexShader(File.ReadAllBytes("RenderVarianceShadow_VS.cso"), null);
            this.m_pvsRenderVarianceShadow.SetDebugName("RenderVarianceShadow - VSMain");

            this.m_ppsRenderVarianceShadow = pd3dDevice.CreatePixelShader(File.ReadAllBytes("RenderVarianceShadow_PS.cso"), null);
            this.m_ppsRenderVarianceShadow.SetDebugName("RenderVarianceShadow - PSMain");

            this.m_pvsQuadBlur = pd3dDevice.CreateVertexShader(File.ReadAllBytes("QuadShaders_VS.cso"), null);
            this.m_pvsQuadBlur.SetDebugName("2DQuadShaders - VSMain");

            D3DShaderMacro[] kernelDefines = new D3DShaderMacro[]
            {
                new D3DShaderMacro("SEPERABLE_BLUR_KERNEL_SIZE", "1")
            };

            for (int iBlurKernelIndex = 0, iKernelSize = 3; iBlurKernelIndex < CascadeConfig.MaxBlurLevels; iBlurKernelIndex++, iKernelSize += 2)
            {
                kernelDefines[0].Definition = iKernelSize.ToString(CultureInfo.InvariantCulture);

                D3DCompile.Compile(
                    QuadShadersResources.Shader,
                    "QuadShaders",
                    kernelDefines,
                    "PSBlurX",
                    PsModel,
                    D3DCompileOptions.OptimizationLevel3,
                    out byte[] shaderBytecodeX,
                    out _);

                this.m_ppsQuadBlurX[iBlurKernelIndex] = pd3dDevice.CreatePixelShader(shaderBytecodeX, null);
                this.m_ppsQuadBlurX[iBlurKernelIndex].SetDebugName("2DQuadShaders - PSBlurX (" + iKernelSize.ToString(CultureInfo.InvariantCulture) + ")");

                D3DCompile.Compile(
                    QuadShadersResources.Shader,
                    "QuadShaders",
                    kernelDefines,
                    "PSBlurY",
                    PsModel,
                    D3DCompileOptions.OptimizationLevel3,
                    out byte[] shaderBytecodeY,
                    out _);

                this.m_ppsQuadBlurY[iBlurKernelIndex] = pd3dDevice.CreatePixelShader(shaderBytecodeY, null);
                this.m_ppsQuadBlurY[iBlurKernelIndex].SetDebugName("2DQuadShaders - PSBlurY (" + iKernelSize.ToString(CultureInfo.InvariantCulture) + ")");
            }

            // In order to compile optimal versions of each shaders,compile out 64 versions of the same file.  
            // The if statments are dependent upon these macros.  This enables the compiler to optimize out code that can never be reached.
            // D3D11 Dynamic shader linkage would have this same effect without the need to compile 64 versions of the shader.
            D3DShaderMacro[] renderSceneDefines = new D3DShaderMacro[]
            {
                new D3DShaderMacro("CASCADE_COUNT_FLAG", "1"),
                new D3DShaderMacro("BLEND_BETWEEN_CASCADE_LAYERS_FLAG", "0"),
                new D3DShaderMacro("SELECT_CASCADE_BY_INTERVAL_FLAG", "0")
            };

            byte[] renderSceneBytecode = null;

            for (int iCascadeIndex = 0; iCascadeIndex < CascadeConfig.MaxCascades; iCascadeIndex++)
            {
                // There is just one vertex shader for the scene.                
                renderSceneDefines[0].Definition = (iCascadeIndex + 1).ToString(CultureInfo.InvariantCulture);
                renderSceneDefines[1].Definition = "0";
                renderSceneDefines[2].Definition = "0";

                D3DCompile.Compile(
                    RenderCascadeSceneResources.Shader,
                    "RenderCascadeScene",
                    renderSceneDefines,
                    "VSMain",
                    VsModel,
                    D3DCompileOptions.OptimizationLevel3,
                    out byte[] vsShaderBytecode,
                    out _);

                this.m_pvsRenderScene[iCascadeIndex] = pd3dDevice.CreateVertexShader(vsShaderBytecode, null);
                this.m_pvsRenderScene[iCascadeIndex].SetDebugName("RenderVarianceScene - VSMain (" + (iCascadeIndex + 1).ToString(CultureInfo.InvariantCulture) + ")");

                if (renderSceneBytecode == null)
                {
                    renderSceneBytecode = vsShaderBytecode;
                }

                for (int iBlendIndex = 0; iBlendIndex < 2; iBlendIndex++)
                {
                    for (int iIntervalIndex = 0; iIntervalIndex < 2; iIntervalIndex++)
                    {
                        renderSceneDefines[0].Definition = (iCascadeIndex + 1).ToString(CultureInfo.InvariantCulture);
                        renderSceneDefines[1].Definition = iBlendIndex.ToString(CultureInfo.InvariantCulture);
                        renderSceneDefines[2].Definition = iIntervalIndex.ToString(CultureInfo.InvariantCulture);

                        D3DCompile.Compile(
                            RenderCascadeSceneResources.Shader,
                            "RenderCascadeScene",
                            renderSceneDefines,
                            "PSMain",
                            PsModel,
                            D3DCompileOptions.OptimizationLevel3,
                            out byte[] shaderBytecode,
                            out _);

                        this.m_ppsRenderSceneAllShaders[iCascadeIndex, iBlendIndex, iIntervalIndex] = pd3dDevice.CreatePixelShader(shaderBytecode, null);
                        this.m_ppsRenderSceneAllShaders[iCascadeIndex, iBlendIndex, iIntervalIndex].SetDebugName(string.Format(CultureInfo.InvariantCulture, "RenderVarianceScene - PSMain [{0},{1},{2}]", iCascadeIndex + 1, iBlendIndex, iIntervalIndex));
                    }
                }
            }

            D3D11InputElementDesc[] layoutMesh = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
            };

            this.m_pVertexLayoutMesh = pd3dDevice.CreateInputLayout(layoutMesh, renderSceneBytecode);
            this.m_pVertexLayoutMesh.SetDebugName("Vertices");

            D3D11RasterizerDesc drd = new D3D11RasterizerDesc
            {
                FillMode = D3D11FillMode.Solid,
                CullMode = D3D11CullMode.None,
                IsFrontCounterClockwise = false,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                SlopeScaledDepthBias = 0.0f,
                IsDepthClipEnabled = true,
                IsScissorEnabled = false,
                IsMultisampleEnabled = true,
                IsAntialiasedLineEnabled = false
            };

            this.m_prsScene = pd3dDevice.CreateRasterizerState(drd);
            this.m_prsScene.SetDebugName("VSM Scene");

            // Setting the slope scale depth biase greatly decreases surface acne and incorrect self shadowing.
            drd.SlopeScaledDepthBias = 1.0f;
            this.m_prsShadow = pd3dDevice.CreateRasterizerState(drd);
            this.m_prsShadow.SetDebugName("VSM Shadow");

            this.m_pcbGlobalConstantBuffer = pd3dDevice.CreateBuffer(new D3D11BufferDesc
            {
                BindOptions = D3D11BindOptions.ConstantBuffer,
                ByteWidth = AllShadowDataConstantBuffer.Size
            });

            this.m_pcbGlobalConstantBuffer.SetDebugName("VSM CB_ALL_SHADOW_DATA");
        }

        public void DestroyAndDeallocateShadowResources()
        {
            D3D11Utils.DisposeAndNull(ref this.m_pVertexLayoutMesh);
            D3D11Utils.DisposeAndNull(ref this.m_pSamLinear);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowPoint);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowLinear);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic2);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic4);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic8);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic16);

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceTextureArray);

            for (int index = 0; index < CascadeConfig.MaxCascades; index++)
            {
                D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceRTVArrayAll[index]);
                D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceSRVArrayAll[index]);
            }

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceSRVArraySingle);

            D3D11Utils.DisposeAndNull(ref this.m_pTemporaryShadowDepthBufferTexture);
            D3D11Utils.DisposeAndNull(ref this.m_pTemporaryShadowDepthBufferDSV);

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurTexture);
            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurRTV);
            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurSRV);

            D3D11Utils.DisposeAndNull(ref this.m_pcbGlobalConstantBuffer);

            D3D11Utils.DisposeAndNull(ref this.m_prsShadow);
            D3D11Utils.DisposeAndNull(ref this.m_prsScene);

            D3D11Utils.DisposeAndNull(ref this.m_pvsQuadBlur);

            for (int index = 0; index < CascadeConfig.MaxBlurLevels; index++)
            {
                D3D11Utils.DisposeAndNull(ref this.m_ppsQuadBlurX[index]);
                D3D11Utils.DisposeAndNull(ref this.m_ppsQuadBlurY[index]);
            }

            D3D11Utils.DisposeAndNull(ref this.m_pvsRenderVarianceShadow);
            D3D11Utils.DisposeAndNull(ref this.m_ppsRenderVarianceShadow);

            for (int iCascadeIndex = 0; iCascadeIndex < CascadeConfig.MaxCascades; iCascadeIndex++)
            {
                D3D11Utils.DisposeAndNull(ref this.m_pvsRenderScene[iCascadeIndex]);

                for (int iBlendIndex = 0; iBlendIndex < 2; iBlendIndex++)
                {
                    for (int iIntervalIndex = 0; iIntervalIndex < 2; iIntervalIndex++)
                    {
                        D3D11Utils.DisposeAndNull(ref this.m_ppsRenderSceneAllShaders[iCascadeIndex, iBlendIndex, iIntervalIndex]);
                    }
                }
            }
        }

        // This is called when cascade config changes.
        // For example: when the shadow buffer size changes.
        private void ReleaseAndAllocateNewShadowResources(D3D11Device pd3dDevice)
        {
            // If any of these 3 paramaters was changed, we must reallocate the D3D resources.
            if (this.m_CopyOfCascadeConfig.Equals(this.m_pCascadeConfig))
            {
                return;
            }

            this.m_CopyOfCascadeConfig = this.m_pCascadeConfig.ShallowCopy();

            D3D11Utils.DisposeAndNull(ref this.m_pSamLinear);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowPoint);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowLinear);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic16);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic8);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic4);
            D3D11Utils.DisposeAndNull(ref this.m_pSamShadowAnisotropic2);

            this.m_pSamLinear = pd3dDevice.CreateSamplerState(new D3D11SamplerDesc(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                0, // 1
                D3D11ComparisonFunction.Never, // D3D11ComparisonFunction.Always
                new float[] { 0, 0, 0, 0 },
                0.0f,
                D3D11Constants.Float32Max));
            this.m_pSamLinear.SetDebugName("VSM Linear");

            var samDescShad = new D3D11SamplerDesc(
                D3D11Filter.Anisotropic,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Border,
                0.0f,
                0,
                D3D11ComparisonFunction.Never,
                new float[] { 0, 0, 0, 0 },
                0.0f,
                0.0f);

            samDescShad.MaxAnisotropy = 16;
            this.m_pSamShadowAnisotropic16 = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowAnisotropic16.SetDebugName("VSM Shadow Ansio16");

            samDescShad.MaxAnisotropy = 8;
            this.m_pSamShadowAnisotropic8 = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowAnisotropic8.SetDebugName("VSM Shadow Ansio8");

            samDescShad.MaxAnisotropy = 4;
            this.m_pSamShadowAnisotropic4 = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowAnisotropic4.SetDebugName("VSM Shadow Ansio4");

            samDescShad.MaxAnisotropy = 2;
            this.m_pSamShadowAnisotropic2 = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowAnisotropic2.SetDebugName("VSM Shadow Ansio2");

            samDescShad.MaxAnisotropy = 0;

            samDescShad.Filter = D3D11Filter.MinMagMipLinear;
            this.m_pSamShadowLinear = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowLinear.SetDebugName("VSM Shadow Linear");

            samDescShad.Filter = D3D11Filter.MinMagMipPoint;
            this.m_pSamShadowPoint = pd3dDevice.CreateSamplerState(samDescShad);
            this.m_pSamShadowPoint.SetDebugName("VSM Shadow Point");

            for (int index = 0; index < this.m_CopyOfCascadeConfig.CascadeLevels; index++)
            {
                this.m_RenderVP[index].Height = this.m_CopyOfCascadeConfig.BufferSize;
                this.m_RenderVP[index].Width = this.m_CopyOfCascadeConfig.BufferSize;
                this.m_RenderVP[index].MaxDepth = 1.0f;
                this.m_RenderVP[index].MinDepth = 0.0f;
                this.m_RenderVP[index].TopLeftX = this.m_CopyOfCascadeConfig.BufferSize * index;
                this.m_RenderVP[index].TopLeftY = 0;
            }

            this.m_RenderOneTileVP.Height = this.m_CopyOfCascadeConfig.BufferSize;
            this.m_RenderOneTileVP.Width = this.m_CopyOfCascadeConfig.BufferSize;
            this.m_RenderOneTileVP.MaxDepth = 1.0f;
            this.m_RenderOneTileVP.MinDepth = 0.0f;
            this.m_RenderOneTileVP.TopLeftX = 0.0f;
            this.m_RenderOneTileVP.TopLeftY = 0.0f;

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceTextureArray);

            for (int index = 0; index < CascadeConfig.MaxCascades; index++)
            {
                D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceRTVArrayAll[index]);
                D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceSRVArrayAll[index]);
            }

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapVarianceSRVArraySingle);

            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurTexture);
            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurRTV);
            D3D11Utils.DisposeAndNull(ref this.m_pCascadedShadowMapTempBlurSRV);

            D3D11Utils.DisposeAndNull(ref this.m_pTemporaryShadowDepthBufferTexture);
            D3D11Utils.DisposeAndNull(ref this.m_pTemporaryShadowDepthBufferDSV);

            DxgiFormat shadowBufferFormat;
            switch (this.m_CopyOfCascadeConfig.ShadowBufferFormat)
            {
                case DepthBufferFormat.R32G32:
                    shadowBufferFormat = DxgiFormat.R32G32Float;
                    break;

                case DepthBufferFormat.R16G16:
                    shadowBufferFormat = DxgiFormat.R16G16Float;
                    break;

                default:
                    shadowBufferFormat = DxgiFormat.R32G32Float;
                    break;
            }

            var dtd = new D3D11Texture2DDesc(
                shadowBufferFormat,
                (uint)this.m_CopyOfCascadeConfig.BufferSize,
                (uint)this.m_CopyOfCascadeConfig.BufferSize,
                (uint)this.m_CopyOfCascadeConfig.CascadeLevels,
                1,
                D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                1,
                0,
                D3D11ResourceMiscOptions.None);

            this.m_pCascadedShadowMapVarianceTextureArray = pd3dDevice.CreateTexture2D(dtd);
            this.m_pCascadedShadowMapVarianceTextureArray.SetDebugName("VSM ShadowMap Var Array");

            dtd.ArraySize = 1;
            this.m_pCascadedShadowMapTempBlurTexture = pd3dDevice.CreateTexture2D(dtd);
            this.m_pCascadedShadowMapTempBlurTexture.SetDebugName("VSM ShadowMap Temp Blur");

            dtd.Format = DxgiFormat.R32Typeless;
            dtd.BindOptions = D3D11BindOptions.DepthStencil;
            this.m_pTemporaryShadowDepthBufferTexture = pd3dDevice.CreateTexture2D(dtd);
            this.m_pTemporaryShadowDepthBufferTexture.SetDebugName("VSM Temp ShadowDepth");

            this.m_pTemporaryShadowDepthBufferDSV = pd3dDevice.CreateDepthStencilView(
                this.m_pTemporaryShadowDepthBufferTexture,
                new D3D11DepthStencilViewDesc(
                    this.m_pTemporaryShadowDepthBufferTexture,
                    D3D11DsvDimension.Texture2D,
                    DxgiFormat.D32Float,
                    0,
                    0));
            this.m_pTemporaryShadowDepthBufferDSV.SetDebugName("VSM Temp ShadowDepth DSV");

            for (int index = 0; index < this.m_CopyOfCascadeConfig.CascadeLevels; index++)
            {
                this.m_pCascadedShadowMapVarianceRTVArrayAll[index] = pd3dDevice.CreateRenderTargetView(
                    this.m_pCascadedShadowMapVarianceTextureArray,
                    new D3D11RenderTargetViewDesc(
                        D3D11RtvDimension.Texture2DArray,
                        shadowBufferFormat,
                        0,
                        (uint)index,
                        1));
                this.m_pCascadedShadowMapVarianceRTVArrayAll[index].SetDebugName(string.Format(CultureInfo.InvariantCulture, "VSM Cascaded Var Array ({0}) RTV", index));
            }

            this.m_pCascadedShadowMapTempBlurRTV = pd3dDevice.CreateRenderTargetView(
                this.m_pCascadedShadowMapTempBlurTexture,
                new D3D11RenderTargetViewDesc(
                    D3D11RtvDimension.Texture2D,
                    shadowBufferFormat,
                    0,
                    0,
                    0));
            this.m_pCascadedShadowMapTempBlurRTV.SetDebugName("VSM Cascaded SM Temp Blur RTV");

            this.m_pCascadedShadowMapVarianceSRVArraySingle = pd3dDevice.CreateShaderResourceView(
                this.m_pCascadedShadowMapVarianceTextureArray,
                new D3D11ShaderResourceViewDesc(
                D3D11SrvDimension.Texture2DArray,
                shadowBufferFormat,
                0,
                1,
                0,
                (uint)this.m_CopyOfCascadeConfig.CascadeLevels));
            this.m_pCascadedShadowMapVarianceSRVArraySingle.SetDebugName("VSM Cascaded SM Var Array SRV");

            for (int index = 0; index < this.m_CopyOfCascadeConfig.CascadeLevels; index++)
            {
                this.m_pCascadedShadowMapVarianceSRVArrayAll[index] = pd3dDevice.CreateShaderResourceView(
                    this.m_pCascadedShadowMapVarianceTextureArray,
                    new D3D11ShaderResourceViewDesc(
                    D3D11SrvDimension.Texture2DArray,
                    shadowBufferFormat,
                    0,
                    1,
                    (uint)index,
                    1));
                this.m_pCascadedShadowMapVarianceSRVArrayAll[index].SetDebugName(string.Format(CultureInfo.InvariantCulture, "VSM Cascaded SM Var Array ({0}) SRV", index));
            }

            //dsrvd.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
            this.m_pCascadedShadowMapTempBlurSRV = pd3dDevice.CreateShaderResourceView(
                this.m_pCascadedShadowMapTempBlurTexture,
                new D3D11ShaderResourceViewDesc(
                D3D11SrvDimension.Texture2DArray,
                shadowBufferFormat,
                0,
                1,
                0,
                1));
            this.m_pCascadedShadowMapTempBlurSRV.SetDebugName("VSM Cascaded SM Temp Blur SRV");
        }

        private static void CreateFrustumPointsFromCascadeInterval(
            float fCascadeIntervalBegin,
            float fCascadeIntervalEnd,
            in XMMatrix vProjection,
            XMVector[] pvCornerPointsWorld)
        {
            BoundingFrustum vViewFrust = BoundingFrustum.CreateFromMatrix(vProjection);
            vViewFrust.Near = fCascadeIntervalBegin;
            vViewFrust.Far = fCascadeIntervalEnd;

            XMVector vGrabY = XMVector.FromInt(0x00000000, 0xFFFFFFFF, 0x00000000, 0x00000000);
            XMVector vGrabX = XMVector.FromInt(0xFFFFFFFF, 0x00000000, 0x00000000, 0x00000000);

            XMVector vRightTop = XMVector.FromFloat(vViewFrust.RightSlope, vViewFrust.TopSlope, 1.0f, 1.0f);
            XMVector vLeftBottom = XMVector.FromFloat(vViewFrust.LeftSlope, vViewFrust.BottomSlope, 1.0f, 1.0f);
            XMVector vNear = XMVector.FromFloat(vViewFrust.Near, vViewFrust.Near, vViewFrust.Near, 1.0f);
            XMVector vFar = XMVector.FromFloat(vViewFrust.Far, vViewFrust.Far, vViewFrust.Far, 1.0f);
            XMVector vRightTopNear = XMVector.Multiply(vRightTop, vNear);
            XMVector vRightTopFar = XMVector.Multiply(vRightTop, vFar);
            XMVector vLeftBottomNear = XMVector.Multiply(vLeftBottom, vNear);
            XMVector vLeftBottomFar = XMVector.Multiply(vLeftBottom, vFar);

            pvCornerPointsWorld[0] = vRightTopNear;
            pvCornerPointsWorld[1] = XMVector.Select(vRightTopNear, vLeftBottomNear, vGrabX);
            pvCornerPointsWorld[2] = vLeftBottomNear;
            pvCornerPointsWorld[3] = XMVector.Select(vRightTopNear, vLeftBottomNear, vGrabY);

            pvCornerPointsWorld[4] = vRightTopFar;
            pvCornerPointsWorld[5] = XMVector.Select(vRightTopFar, vLeftBottomFar, vGrabX);
            pvCornerPointsWorld[6] = vLeftBottomFar;
            pvCornerPointsWorld[7] = XMVector.Select(vRightTopFar, vLeftBottomFar, vGrabY);
        }

        // Compute the near and far plane by intersecting an Ortho Projection with the Scenes AABB.
        // Computing an accurate near and flar plane will decrease surface acne and Peter-panning.
        // Surface acne is the term for erroneous self shadowing.  Peter-panning is the effect where
        // shadows disappear near the base of an object.
        // As offsets are generally used with PCF filtering due self shadowing issues, computing the
        // correct near and far planes becomes even more important.
        // This concept is not complicated, but the intersection code is.
        private static void ComputeNearAndFar(
            out float fNearPlane,
            out float fFarPlane,
            in XMVector vLightCameraOrthographicMin,
            in XMVector vLightCameraOrthographicMax,
            XMVector[] pvPointsInCameraView)
        {
            // Initialize the near and far planes
            fNearPlane = float.MaxValue;
            fFarPlane = float.MinValue;

            int iTriangleCnt;
            Triangle[] triangleList = new Triangle[16];
            for (int i = 0; i < triangleList.Length; i++)
            {
                triangleList[i] = new Triangle();
            }

            //iTriangleCnt = 1;
            //triangleList[0].pt[0] = pvPointsInCameraView[0];
            //triangleList[0].pt[1] = pvPointsInCameraView[1];
            //triangleList[0].pt[2] = pvPointsInCameraView[2];
            //triangleList[0].culled = false;

            // These are the indices used to tesselate an AABB into a list of triangles.
            int[] iAABBTriIndexes = new int[]
            {
                0,1,2,  1,2,3,
                4,5,6,  5,6,7,
                0,2,4,  2,4,6,
                1,3,5,  3,5,7,
                0,1,4,  1,4,5,
                2,3,6,  3,6,7
            };

            int[] iPointPassesCollision = new int[3];

            // At a high level: 
            // 1. Iterate over all 12 triangles of the AABB.  
            // 2. Clip the triangles against each plane. Create new triangles as needed.
            // 3. Find the min and max z values as the near and far plane.

            //This is easier because the triangles are in camera spacing making the collisions tests simple comparisions.

            float fLightCameraOrthographicMinX = vLightCameraOrthographicMin.X;
            float fLightCameraOrthographicMaxX = vLightCameraOrthographicMax.X;
            float fLightCameraOrthographicMinY = vLightCameraOrthographicMin.Y;
            float fLightCameraOrthographicMaxY = vLightCameraOrthographicMax.Y;

            for (int AABBTriIter = 0; AABBTriIter < 12; AABBTriIter++)
            {
                triangleList[0].pt[0] = pvPointsInCameraView[iAABBTriIndexes[AABBTriIter * 3 + 0]];
                triangleList[0].pt[1] = pvPointsInCameraView[iAABBTriIndexes[AABBTriIter * 3 + 1]];
                triangleList[0].pt[2] = pvPointsInCameraView[iAABBTriIndexes[AABBTriIter * 3 + 2]];
                triangleList[0].culled = false;
                iTriangleCnt = 1;

                // Clip each invidual triangle against the 4 frustums.  When ever a triangle is clipped into new triangles, 
                //add them to the list.
                for (int frustumPlaneIter = 0; frustumPlaneIter < 4; frustumPlaneIter++)
                {
                    float fEdge;
                    int iComponent;

                    if (frustumPlaneIter == 0)
                    {
                        fEdge = fLightCameraOrthographicMinX; // todo make float temp
                        iComponent = 0;
                    }
                    else if (frustumPlaneIter == 1)
                    {
                        fEdge = fLightCameraOrthographicMaxX;
                        iComponent = 0;
                    }
                    else if (frustumPlaneIter == 2)
                    {
                        fEdge = fLightCameraOrthographicMinY;
                        iComponent = 1;
                    }
                    else
                    {
                        fEdge = fLightCameraOrthographicMaxY;
                        iComponent = 1;
                    }

                    for (int triIter = 0; triIter < iTriangleCnt; triIter++)
                    {
                        // We don't delete triangles, so we skip those that have been culled.
                        if (!triangleList[triIter].culled)
                        {
                            int iInsideVertCount = 0;
                            XMVector tempOrder;
                            // Test against the correct frustum plane.
                            // This could be written more compactly, but it would be harder to understand.

                            if (frustumPlaneIter == 0)
                            {
                                for (int triPtIter = 0; triPtIter < 3; triPtIter++)
                                {
                                    if (triangleList[triIter].pt[triPtIter].X > vLightCameraOrthographicMin.X)
                                    {
                                        iPointPassesCollision[triPtIter] = 1;
                                    }
                                    else
                                    {
                                        iPointPassesCollision[triPtIter] = 0;
                                    }

                                    iInsideVertCount += iPointPassesCollision[triPtIter];
                                }
                            }
                            else if (frustumPlaneIter == 1)
                            {
                                for (int triPtIter = 0; triPtIter < 3; triPtIter++)
                                {
                                    if (triangleList[triIter].pt[triPtIter].X < vLightCameraOrthographicMax.X)
                                    {
                                        iPointPassesCollision[triPtIter] = 1;
                                    }
                                    else
                                    {
                                        iPointPassesCollision[triPtIter] = 0;
                                    }

                                    iInsideVertCount += iPointPassesCollision[triPtIter];
                                }
                            }
                            else if (frustumPlaneIter == 2)
                            {
                                for (int triPtIter = 0; triPtIter < 3; triPtIter++)
                                {
                                    if (triangleList[triIter].pt[triPtIter].Y > vLightCameraOrthographicMin.Y)
                                    {
                                        iPointPassesCollision[triPtIter] = 1;
                                    }
                                    else
                                    {
                                        iPointPassesCollision[triPtIter] = 0;
                                    }

                                    iInsideVertCount += iPointPassesCollision[triPtIter];
                                }
                            }
                            else
                            {
                                for (int triPtIter = 0; triPtIter < 3; triPtIter++)
                                {
                                    if (triangleList[triIter].pt[triPtIter].Y < vLightCameraOrthographicMax.Y)
                                    {
                                        iPointPassesCollision[triPtIter] = 1;
                                    }
                                    else
                                    {
                                        iPointPassesCollision[triPtIter] = 0;
                                    }

                                    iInsideVertCount += iPointPassesCollision[triPtIter];
                                }
                            }

                            // Move the points that pass the frustum test to the begining of the array.
                            if (iPointPassesCollision[1] != 0 && iPointPassesCollision[0] == 0)
                            {
                                tempOrder = triangleList[triIter].pt[0];
                                triangleList[triIter].pt[0] = triangleList[triIter].pt[1];
                                triangleList[triIter].pt[1] = tempOrder;
                                iPointPassesCollision[0] = 1;
                                iPointPassesCollision[1] = 0;
                            }
                            if (iPointPassesCollision[2] != 0 && iPointPassesCollision[1] == 0)
                            {
                                tempOrder = triangleList[triIter].pt[1];
                                triangleList[triIter].pt[1] = triangleList[triIter].pt[2];
                                triangleList[triIter].pt[2] = tempOrder;
                                iPointPassesCollision[1] = 1;
                                iPointPassesCollision[2] = 0;
                            }
                            if (iPointPassesCollision[1] != 0 && iPointPassesCollision[0] == 0)
                            {
                                tempOrder = triangleList[triIter].pt[0];
                                triangleList[triIter].pt[0] = triangleList[triIter].pt[1];
                                triangleList[triIter].pt[1] = tempOrder;
                                iPointPassesCollision[0] = 1;
                                iPointPassesCollision[1] = 0;
                            }

                            if (iInsideVertCount == 0)
                            {
                                // All points failed. We're done,  
                                triangleList[triIter].culled = true;
                            }
                            else if (iInsideVertCount == 1)
                            {
                                // One point passed. Clip the triangle against the Frustum plane
                                triangleList[triIter].culled = false;

                                // 
                                XMVector vVert0ToVert1 = triangleList[triIter].pt[1] - triangleList[triIter].pt[0];
                                XMVector vVert0ToVert2 = triangleList[triIter].pt[2] - triangleList[triIter].pt[0];

                                // Find the collision ratio.
                                float fHitPointTimeRatio = fEdge - triangleList[triIter].pt[0].GetByIndex(iComponent);
                                // Calculate the distance along the vector as ratio of the hit ratio to the component.
                                float fDistanceAlongVector01 = fHitPointTimeRatio / vVert0ToVert1.GetByIndex(iComponent);
                                float fDistanceAlongVector02 = fHitPointTimeRatio / vVert0ToVert2.GetByIndex(iComponent);
                                // Add the point plus a percentage of the vector.
                                vVert0ToVert1 *= fDistanceAlongVector01;
                                vVert0ToVert1 += triangleList[triIter].pt[0];
                                vVert0ToVert2 *= fDistanceAlongVector02;
                                vVert0ToVert2 += triangleList[triIter].pt[0];

                                triangleList[triIter].pt[1] = vVert0ToVert2;
                                triangleList[triIter].pt[2] = vVert0ToVert1;

                            }
                            else if (iInsideVertCount == 2)
                            {
                                // 2 in  // tesselate into 2 triangles

                                // Copy the triangle\(if it exists) after the current triangle out of
                                // the way so we can override it with the new triangle we're inserting.
                                triangleList[iTriangleCnt] = triangleList[triIter + 1];

                                triangleList[triIter].culled = false;
                                triangleList[triIter + 1].culled = false;

                                // Get the vector from the outside point into the 2 inside points.
                                XMVector vVert2ToVert0 = triangleList[triIter].pt[0] - triangleList[triIter].pt[2];
                                XMVector vVert2ToVert1 = triangleList[triIter].pt[1] - triangleList[triIter].pt[2];

                                // Get the hit point ratio.
                                float fHitPointTime_2_0 = fEdge - triangleList[triIter].pt[2].GetByIndex(iComponent);
                                float fDistanceAlongVector_2_0 = fHitPointTime_2_0 / vVert2ToVert0.GetByIndex(iComponent);
                                // Calcaulte the new vert by adding the percentage of the vector plus point 2.
                                vVert2ToVert0 *= fDistanceAlongVector_2_0;
                                vVert2ToVert0 += triangleList[triIter].pt[2];

                                // Add a new triangle.
                                triangleList[triIter + 1].pt[0] = triangleList[triIter].pt[0];
                                triangleList[triIter + 1].pt[1] = triangleList[triIter].pt[1];
                                triangleList[triIter + 1].pt[2] = vVert2ToVert0;

                                //Get the hit point ratio.
                                float fHitPointTime_2_1 = fEdge - triangleList[triIter].pt[2].GetByIndex(iComponent);
                                float fDistanceAlongVector_2_1 = fHitPointTime_2_1 / vVert2ToVert1.GetByIndex(iComponent);
                                vVert2ToVert1 *= fDistanceAlongVector_2_1;
                                vVert2ToVert1 += triangleList[triIter].pt[2];
                                triangleList[triIter].pt[0] = triangleList[triIter + 1].pt[1];
                                triangleList[triIter].pt[1] = triangleList[triIter + 1].pt[2];
                                triangleList[triIter].pt[2] = vVert2ToVert1;
                                // Cncrement triangle count and skip the triangle we just inserted.
                                iTriangleCnt++;
                                triIter++;
                            }
                            else
                            {
                                // all in
                                triangleList[triIter].culled = false;

                            }
                            // end if !culled loop            
                        }
                    }
                }

                for (int index = 0; index < iTriangleCnt; index++)
                {
                    if (!triangleList[index].culled)
                    {
                        // Set the near and far plan and the min and max z values respectivly.
                        for (int vertind = 0; vertind < 3; ++vertind)
                        {
                            float fTriangleCoordZ = triangleList[index].pt[vertind].Z;

                            if (fNearPlane > fTriangleCoordZ)
                            {
                                fNearPlane = fTriangleCoordZ;
                            }

                            if (fFarPlane < fTriangleCoordZ)
                            {
                                fFarPlane = fTriangleCoordZ;
                            }
                        }
                    }
                }
            }
        }

        // This runs per frame.
        // This data could be cached when the cameras do not move.
        public void InitFrame(D3D11Device pd3dDevice, SdkMeshFile mesh)
        {
            this.ReleaseAndAllocateNewShadowResources(pd3dDevice);

            XMMatrix matViewCameraProjection = this._settings.ViewerCameraProjection;
            XMMatrix matViewCameraView = this._settings.ViewerCameraView;
            XMMatrix matLightCameraView = this._settings.LightCameraView;

            XMMatrix matInverseViewCamera = matViewCameraView.Inverse(out _);

            // This function simply converts the center and extents of an AABB into 8 points
            BoundingBox bb = BoundingBox.CreateFromPoints(this.m_vSceneAABBMin, this.m_vSceneAABBMax);
            XMFloat3[] bbCorners = bb.GetCorners();

            // Transform the scene AABB to Light space.
            XMVector[] vSceneAABBPointsLightSpace = new XMVector[8];
            for (int index = 0; index < 8; index++)
            {
                vSceneAABBPointsLightSpace[index] = XMVector3.Transform(bbCorners[index], matLightCameraView);
            }

            float fFrustumIntervalBegin, fFrustumIntervalEnd;
            XMVector vLightCameraOrthographicMin;  // light space frustrum aabb 
            XMVector vLightCameraOrthographicMax;
            float fCameraNearFarRange = this._settings.ViewerCameraFarClip - this._settings.ViewerCameraNearClip;

            XMVector vWorldUnitsPerTexel = XMVector.Zero;

            // We loop over the cascades to calculate the orthographic projection for each cascade.
            for (int iCascadeIndex = 0; iCascadeIndex < this.m_CopyOfCascadeConfig.CascadeLevels; iCascadeIndex++)
            {
                // Calculate the interval of the View Frustum that this cascade covers. We measure the interval 
                // the cascade covers as a Min and Max distance along the Z Axis.
                if (this.SelectedProjectionFit == FitProjection.ToCascades)
                {
                    // Because we want to fit the orthogrpahic projection tightly around the Cascade, we set the Mimiumum cascade 
                    // value to the previous Frustum end Interval
                    if (iCascadeIndex == 0)
                    {
                        fFrustumIntervalBegin = 0.0f;
                    }
                    else
                    {
                        fFrustumIntervalBegin = this.CascadePartitionsZeroToOne[iCascadeIndex - 1];
                    }
                }
                else
                {
                    // In the FIT_TO_SCENE technique the Cascades overlap eachother.  In other words, interval 1 is coverd by
                    // cascades 1 to 8, interval 2 is covered by cascades 2 to 8 and so forth.
                    fFrustumIntervalBegin = 0.0f;
                }

                // Scale the intervals between 0 and 1. They are now percentages that we can scale with.
                fFrustumIntervalEnd = this.CascadePartitionsZeroToOne[iCascadeIndex];
                fFrustumIntervalBegin /= this.CascadePartitionsMax;
                fFrustumIntervalEnd /= this.CascadePartitionsMax;
                fFrustumIntervalBegin *= fCameraNearFarRange;
                fFrustumIntervalEnd *= fCameraNearFarRange;
                XMVector[] vFrustumPoints = new XMVector[8];

                // This function takes the began and end intervals along with the projection matrix and returns the 8
                // points that repreresent the cascade Interval
                CreateFrustumPointsFromCascadeInterval(
                    fFrustumIntervalBegin,
                    fFrustumIntervalEnd,
                    matViewCameraProjection,
                    vFrustumPoints);

                vLightCameraOrthographicMin = XMVector.Replicate(float.MaxValue);
                vLightCameraOrthographicMax = XMVector.Replicate(float.MinValue);

                XMVector vTempTranslatedCornerPoint;

                // This next section of code calculates the min and max values for the orthographic projection.
                for (int icpIndex = 0; icpIndex < 8; icpIndex++)
                {
                    // Transform the frustum from camera view space to world space.
                    vFrustumPoints[icpIndex] = XMVector4.Transform(vFrustumPoints[icpIndex], matInverseViewCamera);
                    // Transform the point from world space to Light Camera Space.
                    vTempTranslatedCornerPoint = XMVector4.Transform(vFrustumPoints[icpIndex], matLightCameraView);
                    // Find the closest point.
                    vLightCameraOrthographicMin = XMVector.Min(vTempTranslatedCornerPoint, vLightCameraOrthographicMin);
                    vLightCameraOrthographicMax = XMVector.Max(vTempTranslatedCornerPoint, vLightCameraOrthographicMax);
                }


                // This code removes the shimmering effect along the edges of shadows due to
                // the light changing to fit the camera.
                if (this.SelectedProjectionFit == FitProjection.ToScene)
                {
                    // Fit the ortho projection to the cascades far plane and a near plane of zero. 
                    // Pad the projection to be the size of the diagonal of the Frustum partition. 
                    // 
                    // To do this, we pad the ortho transform so that it is always big enough to cover 
                    // the entire camera view frustum.
                    XMVector vDiagonal = vFrustumPoints[0] - vFrustumPoints[6];
                    vDiagonal = XMVector3.Length(vDiagonal);

                    // The bound is the length of the diagonal of the frustum interval.
                    float fCascadeBound = vDiagonal.X;

                    // The offset calculated will pad the ortho projection so that it is always the same size 
                    // and big enough to cover the entire cascade interval.
                    XMVector vBoarderOffset = (vDiagonal - (vLightCameraOrthographicMax - vLightCameraOrthographicMin)) * HalfVector;
                    // Set the Z and W components to zero.
                    vBoarderOffset *= MultiplySetzwToZero;

                    // Add the offsets to the projection.
                    vLightCameraOrthographicMax += vBoarderOffset;
                    vLightCameraOrthographicMin -= vBoarderOffset;

                    // The world units per texel are used to snap the shadow the orthographic projection
                    // to texel sized increments.  This keeps the edges of the shadows from shimmering.
                    float fWorldUnitsPerTexel = fCascadeBound / this.m_CopyOfCascadeConfig.BufferSize;
                    vWorldUnitsPerTexel = XMVector.FromFloat(fWorldUnitsPerTexel, fWorldUnitsPerTexel, 0.0f, 0.0f);
                }
                else if (this.SelectedProjectionFit == FitProjection.ToCascades)
                {
                    // We calculate a looser bound based on the size of the PCF blur.  This ensures us that we're 
                    // sampling within the correct map.
                    float fScaleDuetoBlureAMT = (this.ShadowBlurSize * 2 + 1) / (float)this.m_CopyOfCascadeConfig.BufferSize;
                    XMVector vScaleDuetoBlureAMT = XMVector.FromFloat(fScaleDuetoBlureAMT, fScaleDuetoBlureAMT, 0.0f, 0.0f);

                    float fNormalizeByBufferSize = 1.0f / this.m_CopyOfCascadeConfig.BufferSize;
                    XMVector vNormalizeByBufferSize = XMVector.FromFloat(fNormalizeByBufferSize, fNormalizeByBufferSize, 0.0f, 0.0f);

                    // We calculate the offsets as a percentage of the bound.
                    XMVector vBoarderOffset = vLightCameraOrthographicMax - vLightCameraOrthographicMin;
                    vBoarderOffset *= HalfVector;
                    vBoarderOffset *= vScaleDuetoBlureAMT;
                    vLightCameraOrthographicMax += vBoarderOffset;
                    vLightCameraOrthographicMin -= vBoarderOffset;

                    // The world units per texel are used to snap  the orthographic projection
                    // to texel sized increments.  
                    // Because we're fitting tighly to the cascades, the shimmering shadow edges will still be present when the 
                    // camera rotates.  However, when zooming in or strafing the shadow edge will not shimmer.
                    vWorldUnitsPerTexel = vLightCameraOrthographicMax - vLightCameraOrthographicMin;
                    vWorldUnitsPerTexel *= vNormalizeByBufferSize;

                }

                if (this.MoveLightTexelSize)
                {
                    // We snape the camera to 1 pixel increments so that moving the camera does not cause the shadows to jitter.
                    // This is a matter of integer dividing by the world space size of a texel
                    vLightCameraOrthographicMin /= vWorldUnitsPerTexel;
                    vLightCameraOrthographicMin = vLightCameraOrthographicMin.Floor();
                    vLightCameraOrthographicMin *= vWorldUnitsPerTexel;

                    vLightCameraOrthographicMax /= vWorldUnitsPerTexel;
                    vLightCameraOrthographicMax = vLightCameraOrthographicMax.Floor();
                    vLightCameraOrthographicMax *= vWorldUnitsPerTexel;
                }

                //These are the unconfigured near and far plane values.  They are purposly awful to show 
                // how important calculating accurate near and far planes is.
                float fNearPlane = 0.0f;
                float fFarPlane = 10000.0f;

                if (this.SelectedNearFarFit == FitNearFar.AABB)
                {
                    XMVector vLightSpaceSceneAABBminValue = XMVector.Replicate(float.MaxValue);  // world space scene aabb 
                    XMVector vLightSpaceSceneAABBmaxValue = XMVector.Replicate(float.MinValue);

                    // We calculate the min and max vectors of the scene in light space. The min and max "Z" values of the  
                    // light space AABB can be used for the near and far plane. This is easier than intersecting the scene with the AABB
                    // and in some cases provides similar results.
                    for (int index = 0; index < 8; index++)
                    {
                        vLightSpaceSceneAABBminValue = XMVector.Min(vSceneAABBPointsLightSpace[index], vLightSpaceSceneAABBminValue);
                        vLightSpaceSceneAABBmaxValue = XMVector.Max(vSceneAABBPointsLightSpace[index], vLightSpaceSceneAABBmaxValue);
                    }

                    // The min and max z values are the near and far planes.
                    fNearPlane = vLightSpaceSceneAABBminValue.Z;
                    fFarPlane = vLightSpaceSceneAABBmaxValue.Z;
                }
                else if (this.SelectedNearFarFit == FitNearFar.SceneAABB)
                {
                    // By intersecting the light frustum with the scene AABB we can get a tighter bound on the near and far plane.
                    ComputeNearAndFar(out fNearPlane, out fFarPlane, vLightCameraOrthographicMin, vLightCameraOrthographicMax, vSceneAABBPointsLightSpace);
                }

                // Craete the orthographic projection for this cascade.
                this.m_matShadowProj[iCascadeIndex] = XMMatrix.OrthographicOffCenterLH(
                    vLightCameraOrthographicMin.X,
                    vLightCameraOrthographicMax.X,
                    vLightCameraOrthographicMin.Y,
                    vLightCameraOrthographicMax.Y,
                    fNearPlane,
                    fFarPlane);

                this.m_fCascadePartitionsFrustum[iCascadeIndex] = fFrustumIntervalEnd;
            }

            this.m_matShadowView = this._settings.LightCameraView;
        }

        public void RenderShadowsForAllCascades(D3D11Device pd3dDevice, D3D11DeviceContext pd3dDeviceContext, SdkMeshFile pMesh)
        {
            D3D11RasterizerState rs = pd3dDeviceContext.RasterizerStageGetState();

            pd3dDeviceContext.RasterizerStageSetState(this.m_prsShadow);

            for (int iCurrentCascade = 0; iCurrentCascade < this.m_CopyOfCascadeConfig.CascadeLevels; iCurrentCascade++)
            {
                pd3dDeviceContext.ClearDepthStencilView(this.m_pTemporaryShadowDepthBufferDSV, D3D11ClearOptions.Depth, 1.0f, 0);
                pd3dDeviceContext.OutputMergerSetRenderTargets(new[] { this.m_pCascadedShadowMapVarianceRTVArrayAll[iCurrentCascade] }, this.m_pTemporaryShadowDepthBufferDSV);
                pd3dDeviceContext.RasterizerStageSetViewports(new[] { this.m_RenderOneTileVP });

                //XMMatrix mWorld = this.m_pLightCamera->GetWorldMatrix();
                //XMMatrix mProj = this.m_pLightCamera->GetProjMatrix();

                XMMatrix mWorldViewProjection = this.m_matShadowView * this.m_matShadowProj[iCurrentCascade];

                // VS Per object
                AllShadowDataConstantBuffer pcbAllShadowConstants = new AllShadowDataConstantBuffer
                {
                    m_WorldViewProj = mWorldViewProjection.Transpose(),
                    // The model was exported in world space, so we can pass the identity up as the world transform.
                    m_World = XMMatrix.Identity.Transpose()
                };

                pd3dDeviceContext.UpdateSubresource(this.m_pcbGlobalConstantBuffer, 0, null, pcbAllShadowConstants, 0, 0);

                pd3dDeviceContext.InputAssemblerSetInputLayout(this.m_pVertexLayoutMesh);

                pd3dDeviceContext.VertexShaderSetShader(this.m_pvsRenderVarianceShadow, null);
                pd3dDeviceContext.PixelShaderSetShader(this.m_ppsRenderVarianceShadow, null);
                pd3dDeviceContext.GeometryShaderSetShader(null, null);
                pd3dDeviceContext.VertexShaderSetConstantBuffers(0, new[] { this.m_pcbGlobalConstantBuffer });
                pMesh.Render(0, 1, -1);
            }

            // todo
            //pd3dDeviceContext.InputAssemblerSetInputLayout(null);

            pd3dDeviceContext.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);
            pd3dDeviceContext.VertexShaderSetShader(this.m_pvsQuadBlur, null);
            pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowPoint });

            if (this.ShadowBlurSize > 1)
            {
                int iKernelShader = (this.ShadowBlurSize / 2 - 1); // 3 goes to 0, 5 goes to 1 

                for (int iCurrentCascade = 0; iCurrentCascade < this.m_CopyOfCascadeConfig.CascadeLevels; iCurrentCascade++)
                {
                    pd3dDeviceContext.PixelShaderSetShaderResources(5, new D3D11ShaderResourceView[] { null });
                    pd3dDeviceContext.OutputMergerSetRenderTargets(new[] { this.m_pCascadedShadowMapTempBlurRTV }, null);
                    pd3dDeviceContext.PixelShaderSetShaderResources(5, new[] { this.m_pCascadedShadowMapVarianceSRVArrayAll[iCurrentCascade] });
                    pd3dDeviceContext.PixelShaderSetShader(this.m_ppsQuadBlurX[iKernelShader], null);
                    pd3dDeviceContext.Draw(4, 0);

                    pd3dDeviceContext.PixelShaderSetShaderResources(5, new D3D11ShaderResourceView[] { null });
                    pd3dDeviceContext.OutputMergerSetRenderTargets(new[] { this.m_pCascadedShadowMapVarianceRTVArrayAll[iCurrentCascade] }, null);
                    pd3dDeviceContext.PixelShaderSetShaderResources(5, new[] { this.m_pCascadedShadowMapTempBlurSRV });
                    pd3dDeviceContext.PixelShaderSetShader(this.m_ppsQuadBlurY[iKernelShader], null);
                    pd3dDeviceContext.Draw(4, 0);
                }
            }

            // TODO
            //pd3dDeviceContext.RasterizerStageSetState(null);
            pd3dDeviceContext.RasterizerStageSetState(rs);
            D3D11Utils.DisposeAndNull(ref rs);

            pd3dDeviceContext.OutputMergerSetRenderTargets(new D3D11RenderTargetView[] { null }, null);
        }

        public void RenderScene(
            D3D11DeviceContext pd3dDeviceContext,
            D3D11RenderTargetView prtvBackBuffer,
            D3D11DepthStencilView pdsvBackBuffer,
            SdkMeshFile pMesh,
            in D3D11Viewport dxutViewPort,
            bool bVisualize)
        {
            // We have a seperate render state for the actual rasterization because of different depth biases and Cull modes.
            pd3dDeviceContext.RasterizerStageSetState(this.m_prsScene);
            // 
            pd3dDeviceContext.OutputMergerSetRenderTargets(new[] { prtvBackBuffer }, pdsvBackBuffer);
            pd3dDeviceContext.RasterizerStageSetViewports(new[] { dxutViewPort });
            pd3dDeviceContext.InputAssemblerSetInputLayout(this.m_pVertexLayoutMesh);

            XMMatrix matCameraProj = this._settings.ActiveCameraProjection;
            XMMatrix matCameraView = this._settings.ActiveCameraView;

            // The user has the option to view the ortho shadow cameras.
            if (this.SelectedCamera >= CameraSelection.OrthoCamera1)
            {
                // In the CAMERA_SELECTION enumeration, value 0 is EYE_CAMERA
                // value 1 is LIGHT_CAMERA and 2 to 10 are the ORTHO_CAMERA values.
                // Subtract to so that we can use the enum to index.
                matCameraProj = this.m_matShadowProj[(int)this.SelectedCamera - 2];
                matCameraView = this.m_matShadowView;
            }

            XMMatrix matWorldViewProjection = matCameraView * matCameraProj;

            AllShadowDataConstantBuffer pcbAllShadowConstants = new AllShadowDataConstantBuffer
            {
                m_WorldViewProj = matWorldViewProjection.Transpose(),
                m_World = XMMatrix.Identity.Transpose(),
                m_WorldView = matCameraView.Transpose(),
                m_Shadow = this.m_matShadowView.Transpose(),
                // These are the for loop begin end values. 
                // This is a floating point number that is used as the percentage to blur between maps.    
                m_fCascadeBlendArea = this.BlurBetweenCascadesAmount,
                m_fTexelSize = 1.0f / this.m_CopyOfCascadeConfig.BufferSize
            };

            pcbAllShadowConstants.m_fNativeTexelSizeInX = pcbAllShadowConstants.m_fTexelSize / this.m_CopyOfCascadeConfig.CascadeLevels;

            XMMatrix matTextureScale = XMMatrix.Scaling(0.5f, -0.5f, 1.0f);
            XMMatrix matTextureTranslation = XMMatrix.Translation(0.5f, 0.5f, 0.0f);
            //XMMatrix scaleToTile = XMMatrix.Scaling(1.0f / this.m_pCascadeConfig.CascadeLevels, 1.0f, 1.0f);

            pcbAllShadowConstants.m_vCascadeScale = new XMVector[8];
            pcbAllShadowConstants.m_vCascadeOffset = new XMVector[8];

            for (int index = 0; index < this.m_CopyOfCascadeConfig.CascadeLevels; index++)
            {
                XMMatrix mShadowTexture = this.m_matShadowProj[index] * matTextureScale * matTextureTranslation;

                pcbAllShadowConstants.m_vCascadeScale[index].X = mShadowTexture.M11;
                pcbAllShadowConstants.m_vCascadeScale[index].Y = mShadowTexture.M22;
                pcbAllShadowConstants.m_vCascadeScale[index].Z = mShadowTexture.M33;
                pcbAllShadowConstants.m_vCascadeScale[index].W = 1;

                pcbAllShadowConstants.m_vCascadeOffset[index].X = mShadowTexture.M41;
                pcbAllShadowConstants.m_vCascadeOffset[index].Y = mShadowTexture.M42;
                pcbAllShadowConstants.m_vCascadeOffset[index].Z = mShadowTexture.M43;
                pcbAllShadowConstants.m_vCascadeOffset[index].W = 0;
            }

            // Copy intervals for the depth interval selection method.
            pcbAllShadowConstants.m_fCascadeFrustumsEyeSpaceDepths = new float[CascadeConfig.MaxCascades];
            this.m_fCascadePartitionsFrustum.AsSpan().CopyTo(pcbAllShadowConstants.m_fCascadeFrustumsEyeSpaceDepths);

            // The border padding values keep the pixel shader from reading the borders during PCF filtering.
            pcbAllShadowConstants.m_fMaxBorderPadding = (this.m_pCascadeConfig.BufferSize - 1.0f) / this.m_pCascadeConfig.BufferSize;
            pcbAllShadowConstants.m_fMinBorderPadding = 1.0f / this.m_pCascadeConfig.BufferSize;

            XMVector ep = this._settings.LightCameraEyePoint;
            XMVector lp = this._settings.LightCameraLookAtPoint;
            ep -= lp;
            ep = XMVector3.Normalize(ep);

            pcbAllShadowConstants.m_vLightDir = XMVector.FromFloat(ep.X, ep.Y, ep.Z, 1.0f);
            pcbAllShadowConstants.m_nCascadeLevels = this.m_CopyOfCascadeConfig.CascadeLevels;
            pcbAllShadowConstants.m_iVisualizeCascades = bVisualize ? 1 : 0;

            pd3dDeviceContext.UpdateSubresource(this.m_pcbGlobalConstantBuffer, 0, null, pcbAllShadowConstants, 0, 0);

            pd3dDeviceContext.PixelShaderSetSamplers(0, new[] { this.m_pSamLinear });
            pd3dDeviceContext.PixelShaderSetSamplers(1, new[] { this.m_pSamLinear });

            switch (this.ShadowFilter)
            {
                case ShadowFilter.Anisotropic16x:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowAnisotropic16 });
                    break;

                case ShadowFilter.Anisotropic8x:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowAnisotropic8 });
                    break;

                case ShadowFilter.Anisotropic4x:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowAnisotropic4 });
                    break;

                case ShadowFilter.Anisotropic2x:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowAnisotropic2 });
                    break;

                case ShadowFilter.Linear:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowLinear });
                    break;

                case ShadowFilter.Point:
                    pd3dDeviceContext.PixelShaderSetSamplers(5, new[] { this.m_pSamShadowPoint });
                    break;
            }

            pd3dDeviceContext.GeometryShaderSetShader(null, null);
            pd3dDeviceContext.VertexShaderSetShader(this.m_pvsRenderScene[this.m_CopyOfCascadeConfig.CascadeLevels - 1], null);

            // There are up to 8 cascades,  blur between cascades, 
            // and two cascade selection maps.  This is a total of 32 permutations of the shader.

            pd3dDeviceContext.PixelShaderSetShader(
                this.m_ppsRenderSceneAllShaders[
                    this.m_CopyOfCascadeConfig.CascadeLevels - 1,
                    this.BlurBetweenCascades ? 1 : 0,
                    (int)this.SelectedCascadeSelection],
                null);

            pd3dDeviceContext.PixelShaderSetShaderResources(5, new[] { this.m_pCascadedShadowMapVarianceSRVArraySingle });

            pd3dDeviceContext.VertexShaderSetConstantBuffers(0, new[] { this.m_pcbGlobalConstantBuffer });
            pd3dDeviceContext.PixelShaderSetConstantBuffers(0, new[] { this.m_pcbGlobalConstantBuffer });

            pMesh.Render(0, 1, -1);

            pd3dDeviceContext.PixelShaderSetShaderResources(5, new D3D11ShaderResourceView[] { null, null, null, null, null, null, null, null });
        }
    }
}
