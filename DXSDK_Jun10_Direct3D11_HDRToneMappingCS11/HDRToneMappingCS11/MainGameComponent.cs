// Define this to do full pixel reduction.
// If this is on, the same flag must also be on in ReduceTo1DCS.hlsl.
//#define CS_FULL_PIXEL_REDUCITON 

using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dds;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    class MainGameComponent : IGameComponent
    {
        // Number of stages in the 3x3 down-scaling for post-processing in PS
        private const int NumToneMapTextures = 5;

        private static readonly int ToneMappingTexSize = (int)Math.Pow(3.0f, NumToneMapTextures - 1);

        private const int NumBloomTextures = 2;

        private DeviceResources deviceResources;

        private readonly Skybox g_Skybox;

        // Shaders used in CS path
        private D3D11ComputeShader g_pReduceTo1DCS;
        private D3D11ComputeShader g_pReduceToSingleCS;
        private D3D11ComputeShader g_pBrightPassAndHorizFilterCS;
        private D3D11ComputeShader g_pVertFilterCS;
        private D3D11ComputeShader g_pHorizFilterCS;
        private D3D11PixelShader g_pDumpBufferPS;

        // Blooming effect intermediate buffers used in CS path
        private readonly D3D11Buffer[] g_apBufBloom = new D3D11Buffer[NumBloomTextures];
        private readonly D3D11ShaderResourceView[] g_apBufBloomRV = new D3D11ShaderResourceView[NumBloomTextures];
        private readonly D3D11UnorderedAccessView[] g_apBufBloomUAV = new D3D11UnorderedAccessView[NumBloomTextures];

        // Render target texture for the skybox
        private D3D11Texture2D g_pTexRender;

        // Render target texture for the skybox when multi sampling is on
        private D3D11Texture2D g_pTexRenderMS;
        private D3D11Texture2D g_pMSDS;
        // Intermediate texture used in full screen blur
        private D3D11Texture2D g_pTexBlurred;
        private D3D11RenderTargetView g_pTexRenderRTV;
        private D3D11RenderTargetView g_pMSRTV;
        private D3D11RenderTargetView g_pTexBlurredRTV;
        private D3D11DepthStencilView g_pMSDSV;
        private D3D11ShaderResourceView g_pTexRenderRV;
        private D3D11ShaderResourceView g_pTexBlurredRV;

        // Stuff used for drawing the "full screen quad"
        private D3D11Buffer g_pScreenQuadVB;
        private D3D11InputLayout g_pQuadLayout;
        private D3D11VertexShader g_pQuadVS;
        private D3D11PixelShader g_pFinalPassPS;
        private D3D11PixelShader g_pFinalPassForCPUReductionPS;

        private const uint g_iCBCSBind = 0;
        private const uint g_iCBPSBind = 0;
        private const uint g_iCBBloomPSBind = 0;

        // Constant buffer for passing parameters into the CS
        private D3D11Buffer g_pcbCS;
        // Constant buffer for passing parameters into the PS for bloom effect
        private D3D11Buffer g_pcbBloom;
        // Constant buffer for passing parameters into the CS for horizontal and vertical convolution
        private D3D11Buffer g_pcbFilterCS;

        // Two StructuredBuffer used for ping-ponging in the CS reduction operation
        private D3D11Buffer g_pBufferReduction0;
        private D3D11Buffer g_pBufferReduction1;
        // Two buffer used in full screen blur in CS path
        private D3D11Buffer g_pBufferBlur0;
        private D3D11Buffer g_pBufferBlur1;
        // Buffer for reduction on CPU
        private D3D11Buffer g_pBufferCPURead;

        // UAV of the corresponding buffer object defined above
        private D3D11UnorderedAccessView g_pReductionUAView0;
        private D3D11UnorderedAccessView g_pReductionUAView1;
        private D3D11UnorderedAccessView g_pBlurUAView0;
        private D3D11UnorderedAccessView g_pBlurUAView1;

        // SRV of the corresponding buffer object defined above
        private D3D11ShaderResourceView g_pReductionRV0;
        private D3D11ShaderResourceView g_pReductionRV1;
        private D3D11ShaderResourceView g_pBlurRV0;
        private D3D11ShaderResourceView g_pBlurRV1;

        // CPU reduction result
        private float g_fCPUReduceResult = 0;

        // Tone mapping calculation textures used in PS path
        private readonly D3D11Texture2D[] g_apTexToneMap = new D3D11Texture2D[NumToneMapTextures];
        private readonly D3D11ShaderResourceView[] g_apTexToneMapRV = new D3D11ShaderResourceView[NumToneMapTextures];
        private readonly D3D11RenderTargetView[] g_apTexToneMapRTV = new D3D11RenderTargetView[NumToneMapTextures];
        // Bright pass filter texture used in PS path
        private D3D11Texture2D g_pTexBrightPass;
        private D3D11ShaderResourceView g_pTexBrightPassRV;
        private D3D11RenderTargetView g_pTexBrightPassRTV;
        // Blooming effect intermediate textures used in PS path
        private readonly D3D11Texture2D[] g_apTexBloom = new D3D11Texture2D[NumBloomTextures];
        private readonly D3D11ShaderResourceView[] g_apTexBloomRV = new D3D11ShaderResourceView[NumBloomTextures];
        private readonly D3D11RenderTargetView[] g_apTexBloomRTV = new D3D11RenderTargetView[NumBloomTextures];
        // Shaders in PS path
        private D3D11PixelShader g_pDownScale2x2LumPS;
        private D3D11PixelShader g_pDownScale3x3PS;
        private D3D11PixelShader g_pOldFinalPassPS;
        private D3D11PixelShader g_pDownScale3x3BrightPassPS;
        private D3D11PixelShader g_pBloomPS;

        private D3D11SamplerState g_pSampleStatePoint;
        private D3D11SamplerState g_pSampleStateLinear;

        public MainGameComponent()
        {
            this.g_Skybox = new Skybox();

            XMVector vecEye = new(0.0f, -10.5f, -3.0f, 0.0f);
            XMVector vecAt = new(0.0f, 0.0f, 0.0f, 0.0f);
            XMVector vecUp = new(0.0f, 1.0f, 0.0f, 0.0f);

            this.ViewMatrix = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);

            this.WorldMatrix = XMMatrix.Identity;
        }

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel110;

        public bool IsPostProcess { get; private set; } = true;

        public bool? IsPostProcessRequested { get; set; }

        public PostProcessMode PostProcessMode { get; private set; } = PostProcessMode.ComputeShader;

        public PostProcessMode? PostProcessModeRequested { get; set; }

        public bool IsBloom { get; private set; } = false;

        public bool? IsBloomRequested { get; set; }

        public bool IsFullScrBlur { get; private set; } = false;

        public bool? IsFullScrBlurRequested { get; set; }

        public bool IsCPUReduction { get; private set; } = false;

        public bool? IsCPUReductionRequested { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            DdsDirectX.CreateTexture(
                "uffizi_cross.dds",
                device,
                context,
                out D3D11Resource pCubeTexture,
                out D3D11ShaderResourceView pCubeRV);

            pCubeTexture.SetDebugName("uffizi_cross.dds");
            pCubeRV.SetDebugName("uffizi_cross.dds");

            this.g_Skybox.CreateDeviceDependentResources(this.deviceResources, 50, pCubeTexture, pCubeRV);

            // Create the shaders
            this.g_pReduceTo1DCS = device.CreateComputeShader(File.ReadAllBytes("ReduceTo1DCS.cso"), null);
            this.g_pReduceTo1DCS.SetDebugName("ReduceTo1DCS");

            this.g_pReduceToSingleCS = device.CreateComputeShader(File.ReadAllBytes("ReduceToSingleCS.cso"), null);
            this.g_pReduceToSingleCS.SetDebugName("ReduceToSingleCS");

            this.g_pFinalPassPS = device.CreatePixelShader(File.ReadAllBytes("PSFinalPass.cso"), null);
            this.g_pFinalPassPS.SetDebugName("PSFinalPass");

            this.g_pFinalPassForCPUReductionPS = device.CreatePixelShader(File.ReadAllBytes("PSFinalPassForCPUReduction.cso"), null);
            this.g_pFinalPassForCPUReductionPS.SetDebugName("PSFinalPassForCPUReduction");

            this.g_pDownScale2x2LumPS = device.CreatePixelShader(File.ReadAllBytes("DownScale2x2_Lum.cso"), null);
            this.g_pDownScale2x2LumPS.SetDebugName("DownScale2x2_Lum");

            this.g_pDownScale3x3PS = device.CreatePixelShader(File.ReadAllBytes("DownScale3x3.cso"), null);
            this.g_pDownScale3x3PS.SetDebugName("DownScale3x3");

            this.g_pOldFinalPassPS = device.CreatePixelShader(File.ReadAllBytes("FinalPass.cso"), null);
            this.g_pOldFinalPassPS.SetDebugName("FinalPass");

            this.g_pDownScale3x3BrightPassPS = device.CreatePixelShader(File.ReadAllBytes("DownScale3x3_BrightPass.cso"), null);
            this.g_pDownScale3x3BrightPassPS.SetDebugName("DownScale3x3_BrightPass");

            this.g_pBloomPS = device.CreatePixelShader(File.ReadAllBytes("Bloom.cso"), null);
            this.g_pBloomPS.SetDebugName("Bloom");

            this.g_pBrightPassAndHorizFilterCS = device.CreateComputeShader(File.ReadAllBytes("BrightPassAndHorizFilterCS.cso"), null);
            this.g_pBrightPassAndHorizFilterCS.SetDebugName("BrightPassAndHorizFilterCS");

            this.g_pVertFilterCS = device.CreateComputeShader(File.ReadAllBytes("CSVerticalFilter.cso"), null);
            this.g_pVertFilterCS.SetDebugName("CSVerticalFilter");

            this.g_pHorizFilterCS = device.CreateComputeShader(File.ReadAllBytes("CSHorizFilter.cso"), null);
            this.g_pHorizFilterCS.SetDebugName("CSHorizFilter");

            this.g_pDumpBufferPS = device.CreatePixelShader(File.ReadAllBytes("PSDump.cso"), null);
            this.g_pDumpBufferPS.SetDebugName("PSDump");

            this.g_pQuadVS = device.CreateVertexShader(File.ReadAllBytes("QuadVS.cso"), null);
            this.g_pQuadVS.SetDebugName("QuadVS");

            D3D11InputElementDesc[] quadlayout = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32A32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 16, D3D11InputClassification.PerVertexData, 0)
            };

            this.g_pQuadLayout = device.CreateInputLayout(quadlayout, File.ReadAllBytes("QuadVS.cso"));
            this.g_pQuadLayout.SetDebugName("Quad");

            // Setup constant buffers
            this.g_pcbCS = device.CreateBuffer(new D3D11BufferDesc(CSConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbCS.SetDebugName("CB_CS");

            this.g_pcbBloom = device.CreateBuffer(new D3D11BufferDesc(BloomPSConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbBloom.SetDebugName("CB_Bloom_PS");

            this.g_pcbFilterCS = device.CreateBuffer(new D3D11BufferDesc(FilterHorizontalConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbFilterCS.SetDebugName("CB_filter");

            // Samplers
            this.g_pSampleStateLinear = device.CreateSamplerState(new D3D11SamplerDesc(
                D3D11Filter.MinMagMipLinear,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                0.0f,
                0,
                D3D11ComparisonFunction.None,
                null,
                0.0f,
                0.0f));
            this.g_pSampleStateLinear.SetDebugName("Linear");

            this.g_pSampleStatePoint = device.CreateSamplerState(new D3D11SamplerDesc(
                D3D11Filter.MinMagMipPoint,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                0.0f,
                0,
                D3D11ComparisonFunction.None,
                null,
                0.0f,
                0.0f));
            this.g_pSampleStatePoint.SetDebugName("Point");

            // Create a screen quad for render to texture operations
            ScreenVertex[] svQuad = new ScreenVertex[]
            {
                new ScreenVertex(new XMFloat4(-1.0f, 1.0f, 0.5f, 1.0f), new XMFloat2(0.0f, 0.0f)),
                new ScreenVertex(new XMFloat4(1.0f, 1.0f, 0.5f, 1.0f), new XMFloat2(1.0f, 0.0f)),
                new ScreenVertex(new XMFloat4(-1.0f, -1.0f, 0.5f, 1.0f), new XMFloat2(0.0f, 1.0f)),
                new ScreenVertex(new XMFloat4(1.0f, -1.0f, 0.5f, 1.0f), new XMFloat2(1.0f, 1.0f)),
            };

            this.g_pScreenQuadVB = device.CreateBuffer(
                new D3D11BufferDesc(ScreenVertex.Size * (uint)svQuad.Length, D3D11BindOptions.VertexBuffer),
                svQuad,
                0,
                0);
            this.g_pScreenQuadVB.SetDebugName("ScreenQuad");
        }

        public void ReleaseDeviceDependentResources()
        {
            this.g_Skybox.ReleaseDeviceDependentResources();

            D3D11Utils.DisposeAndNull(ref this.g_pFinalPassPS);
            D3D11Utils.DisposeAndNull(ref this.g_pFinalPassForCPUReductionPS);
            D3D11Utils.DisposeAndNull(ref this.g_pReduceTo1DCS);
            D3D11Utils.DisposeAndNull(ref this.g_pReduceToSingleCS);
            D3D11Utils.DisposeAndNull(ref this.g_pBrightPassAndHorizFilterCS);
            D3D11Utils.DisposeAndNull(ref this.g_pVertFilterCS);
            D3D11Utils.DisposeAndNull(ref this.g_pHorizFilterCS);
            D3D11Utils.DisposeAndNull(ref this.g_pDownScale2x2LumPS);
            D3D11Utils.DisposeAndNull(ref this.g_pDownScale3x3PS);
            D3D11Utils.DisposeAndNull(ref this.g_pOldFinalPassPS);
            D3D11Utils.DisposeAndNull(ref this.g_pDownScale3x3BrightPassPS);
            D3D11Utils.DisposeAndNull(ref this.g_pBloomPS);
            D3D11Utils.DisposeAndNull(ref this.g_pDumpBufferPS);

            D3D11Utils.DisposeAndNull(ref this.g_pcbCS);
            D3D11Utils.DisposeAndNull(ref this.g_pcbBloom);
            D3D11Utils.DisposeAndNull(ref this.g_pcbFilterCS);

            D3D11Utils.DisposeAndNull(ref this.g_pSampleStateLinear);
            D3D11Utils.DisposeAndNull(ref this.g_pSampleStatePoint);

            D3D11Utils.DisposeAndNull(ref this.g_pScreenQuadVB);
            D3D11Utils.DisposeAndNull(ref this.g_pQuadVS);
            D3D11Utils.DisposeAndNull(ref this.g_pQuadLayout);
        }

        public void CreateWindowSizeDependentResources()
        {
            var device = this.deviceResources.D3DDevice;
            var sampleDesc = this.deviceResources.D3DSampleDesc;

            this.g_Skybox.CreateWindowSizeDependentResources();

            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 5000.0f);

            // Create the render target texture
            // Our skybox will be rendered to this texture for later post-process
            {
                D3D11Texture2DDesc Desc = new(
                    DxgiFormat.R32G32B32A32Float,
                    this.deviceResources.BackBufferWidth,
                    this.deviceResources.BackBufferHeight,
                    1,
                    1,
                    D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource);

                this.g_pTexRender = device.CreateTexture2D(Desc);
                this.g_pTexRender.SetDebugName("Render");
                this.g_pTexBlurred = device.CreateTexture2D(Desc);
                this.g_pTexBlurred.SetDebugName("Blurred");

                // Create the render target view
                D3D11RenderTargetViewDesc DescRT = new(D3D11RtvDimension.Texture2D, Desc.Format, 0);

                this.g_pTexRenderRTV = device.CreateRenderTargetView(this.g_pTexRender, DescRT);
                this.g_pTexRenderRTV.SetDebugName("Render RTV");
                this.g_pTexBlurredRTV = device.CreateRenderTargetView(this.g_pTexBlurred, DescRT);
                this.g_pTexBlurredRTV.SetDebugName("Blurred RTV");

                if (sampleDesc.Count > 1)
                {
                    // If multi sampling is on, we create the multi sample floating render target
                    D3D11Texture2DDesc DescMS = Desc;
                    DescMS.BindOptions = D3D11BindOptions.RenderTarget;
                    DescMS.SampleDesc = sampleDesc;

                    this.g_pTexRenderMS = device.CreateTexture2D(DescMS);
                    this.g_pTexRenderMS.SetDebugName("MSAA RT");

                    // Render target view for multi-sampling
                    D3D11RenderTargetViewDesc DescMSRT = DescRT;
                    DescMSRT.ViewDimension = D3D11RtvDimension.Texture2DMs;

                    this.g_pMSRTV = device.CreateRenderTargetView(this.g_pTexRenderMS, DescMSRT);
                    this.g_pMSRTV.SetDebugName("MSAA SRV");

                    // Depth stencil texture for multi-sampling
                    DescMS.Format = DxgiFormat.D32Float;
                    DescMS.BindOptions = D3D11BindOptions.DepthStencil;

                    this.g_pMSDS = device.CreateTexture2D(DescMS);
                    this.g_pMSDS.SetDebugName("MSAA Depth RT");

                    // Depth stencil view for multi-sampling
                    D3D11DepthStencilViewDesc DescDS = new(D3D11DsvDimension.Texture2DMs, DxgiFormat.D32Float);

                    this.g_pMSDSV = device.CreateDepthStencilView(this.g_pMSDS, DescDS);
                    this.g_pMSDSV.SetDebugName("MSAA Depth DSV");
                }

                // Create the resource view
                D3D11ShaderResourceViewDesc DescRV = new(D3D11SrvDimension.Texture2D, Desc.Format, 0, 1);

                this.g_pTexRenderRV = device.CreateShaderResourceView(this.g_pTexRender, DescRV);
                this.g_pTexRenderRV.SetDebugName("Render SRV");
                this.g_pTexBlurredRV = device.CreateShaderResourceView(this.g_pTexBlurred, DescRV);
                this.g_pTexBlurredRV.SetDebugName("Blurred SRV");
            }

            // Create the buffers used in full screen blur for CS path
            {
                D3D11BufferDesc DescBuffer = new(
                    this.deviceResources.BackBufferWidth * this.deviceResources.BackBufferHeight * 16,
                    D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                    D3D11Usage.Default,
                    D3D11CpuAccessOptions.None,
                    D3D11ResourceMiscOptions.BufferStructured,
                    16);

                this.g_pBufferBlur0 = device.CreateBuffer(DescBuffer);
                this.g_pBufferBlur0.SetDebugName("Blur0");
                this.g_pBufferBlur1 = device.CreateBuffer(DescBuffer);
                this.g_pBufferBlur1.SetDebugName("Blur1");

                D3D11UnorderedAccessViewDesc DescUAV = new(this.g_pBufferBlur0, DxgiFormat.Unknown, 0, DescBuffer.ByteWidth / DescBuffer.StructureByteStride);

                this.g_pBlurUAView0 = device.CreateUnorderedAccessView(this.g_pBufferBlur0, DescUAV);
                this.g_pBlurUAView0.SetDebugName("Blur0 UAV");
                this.g_pBlurUAView1 = device.CreateUnorderedAccessView(this.g_pBufferBlur1, DescUAV);
                this.g_pBlurUAView1.SetDebugName("Blur1 UAV");

                D3D11ShaderResourceViewDesc DescRV = new(this.g_pBufferBlur0, DxgiFormat.Unknown, DescUAV.Buffer.FirstElement, DescUAV.Buffer.NumElements);

                this.g_pBlurRV0 = device.CreateShaderResourceView(this.g_pBufferBlur0, DescRV);
                this.g_pBlurRV0.SetDebugName("Blur0 SRV");
                this.g_pBlurRV1 = device.CreateShaderResourceView(this.g_pBufferBlur1, DescRV);
                this.g_pBlurRV1.SetDebugName("Blur1 SRV");
            }

            {
                // Create two buffers for ping-ponging in the reduction operation used for calculating luminance
                D3D11BufferDesc DescBuffer = new(
                    (uint)Math.Ceiling(this.deviceResources.BackBufferWidth / 8.0f) * (uint)Math.Ceiling(this.deviceResources.BackBufferHeight / 8.0f) * 4,
                    D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                    D3D11Usage.Default,
                    D3D11CpuAccessOptions.None,
                    D3D11ResourceMiscOptions.BufferStructured,
                    4);

                this.g_pBufferReduction0 = device.CreateBuffer(DescBuffer);
                this.g_pBufferReduction0.SetDebugName("Reduction0");
                this.g_pBufferReduction1 = device.CreateBuffer(DescBuffer);
                this.g_pBufferReduction1.SetDebugName("Reduction1");

                // This Buffer is for reduction on CPU
                DescBuffer.CpuAccessOptions = D3D11CpuAccessOptions.Read;
                DescBuffer.Usage = D3D11Usage.Staging;
                DescBuffer.BindOptions = D3D11BindOptions.None;

                this.g_pBufferCPURead = device.CreateBuffer(DescBuffer);
                this.g_pBufferCPURead.SetDebugName("CPU Read");

                // Create UAV on the above two buffers object
                D3D11UnorderedAccessViewDesc DescUAV = new(this.g_pBufferReduction0, DxgiFormat.Unknown, 0, DescBuffer.ByteWidth / 4);

                this.g_pReductionUAView0 = device.CreateUnorderedAccessView(this.g_pBufferReduction0, DescUAV);
                this.g_pReductionUAView0.SetDebugName("Reduction0 UAV");
                this.g_pReductionUAView1 = device.CreateUnorderedAccessView(this.g_pBufferReduction1, DescUAV);
                this.g_pReductionUAView1.SetDebugName("Reduction1 UAV");

                // Create resource view for the two buffers object
                D3D11ShaderResourceViewDesc DescRV = new(this.g_pBufferReduction0, DxgiFormat.Unknown, DescUAV.Buffer.FirstElement, DescUAV.Buffer.NumElements);

                this.g_pReductionRV0 = device.CreateShaderResourceView(this.g_pBufferReduction0, DescRV);
                this.g_pReductionRV0.SetDebugName("Reduction0 SRV");
                this.g_pReductionRV1 = device.CreateShaderResourceView(this.g_pBufferReduction1, DescRV);
                this.g_pReductionRV1.SetDebugName("Reduction1 SRV");
            }

            // Textures for tone mapping for the PS path
            uint nSampleLen = 1;
            for (int i = 0; i < NumToneMapTextures; i++)
            {
                D3D11Texture2DDesc tmdesc = new(
                    DxgiFormat.R32Float,
                    nSampleLen,
                    nSampleLen,
                    1,
                    1,
                    D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource);

                this.g_apTexToneMap[i] = device.CreateTexture2D(tmdesc);
                this.g_apTexToneMap[i].SetDebugName("ToneMap");

                // Create the render target view
                D3D11RenderTargetViewDesc DescRT = new(this.g_apTexToneMap[i], D3D11RtvDimension.Texture2D, tmdesc.Format, 0);

                this.g_apTexToneMapRTV[i] = device.CreateRenderTargetView(this.g_apTexToneMap[i], DescRT);
                this.g_apTexToneMapRTV[i].SetDebugName("ToneMap RTV");

                // Create the shader resource view
                D3D11ShaderResourceViewDesc DescRV = new(this.g_apTexToneMap[i], D3D11SrvDimension.Texture2D, tmdesc.Format, 0, 1);

                this.g_apTexToneMapRV[i] = device.CreateShaderResourceView(this.g_apTexToneMap[i], DescRV);
                this.g_apTexToneMapRV[i].SetDebugName("ToneMap SRV");

                nSampleLen *= 3;
            }

            // Create the temporary blooming effect textures for PS path and buffers for CS path
            for (int i = 0; i < NumBloomTextures; i++)
            {
                // Texture for blooming effect in PS path
                D3D11Texture2DDesc bmdesc = new(
                    DxgiFormat.R8G8B8A8UNorm,
                    this.deviceResources.BackBufferWidth / 8,
                    this.deviceResources.BackBufferHeight / 8,
                    1,
                    1,
                    D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource);

                this.g_apTexBloom[i] = device.CreateTexture2D(bmdesc);
                this.g_apTexBloom[i].SetDebugName("PSBloom");

                // Create the render target view
                D3D11RenderTargetViewDesc DescRT = new(this.g_apTexBloom[i], D3D11RtvDimension.Texture2D, bmdesc.Format, 0);

                this.g_apTexBloomRTV[i] = device.CreateRenderTargetView(this.g_apTexBloom[i], DescRT);
                this.g_apTexBloomRTV[i].SetDebugName("PSBloom RTV");

                // Create the shader resource view
                {
                    D3D11ShaderResourceViewDesc DescRV = new(this.g_apTexBloom[i], D3D11SrvDimension.Texture2D, bmdesc.Format, 0, 1);

                    this.g_apTexBloomRV[i] = device.CreateShaderResourceView(this.g_apTexBloom[i], DescRV);
                    this.g_apTexBloomRV[i].SetDebugName("PSBloom SRV");
                }

                // Buffers for blooming effect in CS path
                D3D11BufferDesc bufdesc = new(
                    this.deviceResources.BackBufferWidth / 8 * this.deviceResources.BackBufferHeight / 8 * 16,
                    D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                    D3D11Usage.Default,
                    D3D11CpuAccessOptions.None,
                    D3D11ResourceMiscOptions.BufferStructured,
                    16);

                this.g_apBufBloom[i] = device.CreateBuffer(bufdesc);
                this.g_apBufBloom[i].SetDebugName("CSBloom");

                {
                    D3D11ShaderResourceViewDesc DescRV = new(this.g_apBufBloom[i], DxgiFormat.Unknown, 0, bufdesc.ByteWidth / bufdesc.StructureByteStride);

                    this.g_apBufBloomRV[i] = device.CreateShaderResourceView(this.g_apBufBloom[i], DescRV);
                    this.g_apBufBloomRV[i].SetDebugName("CSBloom RTV");

                    D3D11UnorderedAccessViewDesc DescUAV = new(this.g_apBufBloom[i], DxgiFormat.Unknown, 0, DescRV.Buffer.NumElements);

                    this.g_apBufBloomUAV[i] = device.CreateUnorderedAccessView(this.g_apBufBloom[i], DescUAV);
                    this.g_apBufBloomUAV[i].SetDebugName("CSBloom UAV");
                }
            }

            // Create the bright pass texture for PS path
            {
                D3D11Texture2DDesc Desc = new(
                    DxgiFormat.R8G8B8A8UNorm,
                    this.deviceResources.BackBufferWidth / 8,
                    this.deviceResources.BackBufferHeight / 8,
                    1,
                    1,
                    D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource);

                this.g_pTexBrightPass = device.CreateTexture2D(Desc);
                this.g_pTexBrightPass.SetDebugName("BrightPass");

                // Create the render target view
                D3D11RenderTargetViewDesc DescRT = new(this.g_pTexBrightPass, D3D11RtvDimension.Texture2D, Desc.Format, 0);

                this.g_pTexBrightPassRTV = device.CreateRenderTargetView(this.g_pTexBrightPass, DescRT);
                this.g_pTexBrightPassRTV.SetDebugName("BrightPass RTV");

                // Create the resource view
                D3D11ShaderResourceViewDesc DescRV = new(this.g_pTexBrightPass, D3D11SrvDimension.Texture2D, Desc.Format, 0, 1);

                this.g_pTexBrightPassRV = device.CreateShaderResourceView(this.g_pTexBrightPass, DescRV);
                this.g_pTexBrightPassRV.SetDebugName("BrightPass SRV");
            }
        }

        public void ReleaseWindowSizeDependentResources()
        {
            this.g_Skybox.ReleaseWindowSizeDependentResources();

            D3D11Utils.DisposeAndNull(ref this.g_pTexRender);
            D3D11Utils.DisposeAndNull(ref this.g_pTexRenderMS);
            D3D11Utils.DisposeAndNull(ref this.g_pMSDS);
            D3D11Utils.DisposeAndNull(ref this.g_pTexBlurred);
            D3D11Utils.DisposeAndNull(ref this.g_pTexRenderRTV);
            D3D11Utils.DisposeAndNull(ref this.g_pMSRTV);
            D3D11Utils.DisposeAndNull(ref this.g_pMSDSV);
            D3D11Utils.DisposeAndNull(ref this.g_pTexBlurredRTV);
            D3D11Utils.DisposeAndNull(ref this.g_pTexRenderRV);
            D3D11Utils.DisposeAndNull(ref this.g_pTexBlurredRV);

            D3D11Utils.DisposeAndNull(ref this.g_pBufferReduction0);
            D3D11Utils.DisposeAndNull(ref this.g_pBufferReduction1);
            D3D11Utils.DisposeAndNull(ref this.g_pBufferBlur0);
            D3D11Utils.DisposeAndNull(ref this.g_pBufferBlur1);
            D3D11Utils.DisposeAndNull(ref this.g_pBufferCPURead);
            D3D11Utils.DisposeAndNull(ref this.g_pReductionUAView0);
            D3D11Utils.DisposeAndNull(ref this.g_pReductionUAView1);
            D3D11Utils.DisposeAndNull(ref this.g_pBlurUAView0);
            D3D11Utils.DisposeAndNull(ref this.g_pBlurUAView1);
            D3D11Utils.DisposeAndNull(ref this.g_pReductionRV0);
            D3D11Utils.DisposeAndNull(ref this.g_pReductionRV1);
            D3D11Utils.DisposeAndNull(ref this.g_pBlurRV0);
            D3D11Utils.DisposeAndNull(ref this.g_pBlurRV1);

            for (int i = 0; i < NumToneMapTextures; i++)
            {
                // Tone mapping calculation textures
                D3D11Utils.DisposeAndNull(ref this.g_apTexToneMap[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apTexToneMapRV[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apTexToneMapRTV[i]);
            }

            for (int i = 0; i < NumBloomTextures; i++)
            {
                // Blooming effect intermediate texture
                D3D11Utils.DisposeAndNull(ref this.g_apTexBloom[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apTexBloomRV[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apTexBloomRTV[i]);

                D3D11Utils.DisposeAndNull(ref this.g_apBufBloom[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apBufBloomRV[i]);
                D3D11Utils.DisposeAndNull(ref this.g_apBufBloomUAV[i]);
            }

            D3D11Utils.DisposeAndNull(ref this.g_pTexBrightPassRV);
            D3D11Utils.DisposeAndNull(ref this.g_pTexBrightPassRTV);
            D3D11Utils.DisposeAndNull(ref this.g_pTexBrightPass);
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            if (this.IsPostProcessRequested.HasValue)
            {
                this.IsPostProcess = this.IsPostProcessRequested.Value;
                this.IsPostProcessRequested = null;
            }

            if (this.PostProcessModeRequested.HasValue)
            {
                this.PostProcessMode = this.PostProcessModeRequested.Value;
                this.PostProcessModeRequested = null;
            }

            if (this.IsBloomRequested.HasValue)
            {
                this.IsBloom = this.IsBloomRequested.Value;
                this.IsBloomRequested = null;
            }

            if (this.IsFullScrBlurRequested.HasValue)
            {
                this.IsFullScrBlur = this.IsFullScrBlurRequested.Value;
                this.IsFullScrBlurRequested = null;
            }

            if (this.IsCPUReductionRequested.HasValue)
            {
                this.IsCPUReduction = this.IsCPUReductionRequested.Value;
                this.IsCPUReductionRequested = null;
            }
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;
            var sampleDesc = this.deviceResources.D3DSampleDesc;
            var pBackBufferDesc = this.deviceResources.BackBuffer.Description;

            // red, green, blue, alpha
            float[] ClearColor = { 0.3f, 0.3f, 0.3f, 1.0f };

            // Set the render target to our own texture
            if (this.IsPostProcess)
            {
                if (sampleDesc.Count > 1)
                {
                    context.OutputMergerSetRenderTargets(new[] { this.g_pMSRTV }, this.g_pMSDSV);

                    context.ClearRenderTargetView(this.g_pMSRTV, ClearColor);
                    context.ClearDepthStencilView(this.g_pMSDSV, D3D11ClearOptions.Depth, 1.0f, 0);
                }
                else
                {
                    context.OutputMergerSetRenderTargets(new[] { this.g_pTexRenderRTV }, this.deviceResources.D3DDepthStencilView);

                    context.ClearRenderTargetView(this.g_pTexRenderRTV, ClearColor);
                    context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);
                }
            }
            else
            {
                context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
                context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, ClearColor);
                context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);
            }

            XMMatrix mWorldViewProjection = this.WorldMatrix * this.ViewMatrix * this.ProjectionMatrix;

            this.g_Skybox.Render(mWorldViewProjection.Transpose());

            if (this.IsPostProcess && sampleDesc.Count > 1)
            {
                D3D11Texture2DDesc Desc = this.g_pTexRender.Description;
                context.ResolveSubresource(this.g_pTexRender, 0, this.g_pTexRenderMS, 0, Desc.Format);
                context.OutputMergerSetRenderTargets(new D3D11RenderTargetView[] { null }, this.deviceResources.D3DDepthStencilView);
            }

            if (this.IsPostProcess)
            {
                // g_pTexRender is bound as the render target, release it here,
                // as it will be used later as the input texture to the CS
                context.OutputMergerSetRenderTargets(new D3D11RenderTargetView[] { null }, this.deviceResources.D3DDepthStencilView);

                if (this.PostProcessMode == PostProcessMode.ComputeShader)
                {
                    this.MeasureLuminanceCS(pBackBufferDesc);

                    if (this.IsFullScrBlur)
                    {
                        this.FullScrBlurCS(pBackBufferDesc);
                    }

                    if (this.IsBloom)
                    {
                        this.BloomCS(pBackBufferDesc);
                    }

                    this.DumpToTexture(
                        pBackBufferDesc.Width / 8,
                        pBackBufferDesc.Height / 8,
                        this.g_apBufBloomRV[0],
                        this.g_apTexBloomRTV[0]);

                    if (this.IsFullScrBlur)
                    {
                        this.DumpToTexture(
                            pBackBufferDesc.Width,
                            pBackBufferDesc.Height,
                            this.g_pBlurRV1,
                            this.g_pTexRenderRTV);
                    }
                }
                else //if ( this.PostProcessMode ==  PostProcessMode.PixelShader)
                {
                    this.MeasureLuminancePS();

                    if (this.IsBloom)
                    {
                        this.BrightPassFilterPS();
                        this.RenderBloomPS(pBackBufferDesc);
                    }

                    if (this.IsFullScrBlur)
                    {
                        this.FullScrBlurPS(pBackBufferDesc);
                    }
                }

                // Restore original render targets
                context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);

                // Tone-mapping
                if (this.PostProcessMode == PostProcessMode.ComputeShader)
                {
                    if (!this.IsCPUReduction)
                    {
                        context.PixelShaderSetShaderResources(0, new[]
                        {
                            this.g_pTexRenderRV,
                            this.g_pReductionRV1,
                            this.IsBloom ? this.g_apTexBloomRV[0] : null
                        });

                        PSConstantBufferData pcbCS = new()
                        {
#if CS_FULL_PIXEL_REDUCITON
                            param0 = 1.0f / (pBackBufferDesc.Width * pBackBufferDesc.Height)
#else
                            param0 = 1.0f / (ToneMappingTexSize * ToneMappingTexSize)
#endif
                        };

                        context.UpdateSubresource(this.g_pcbCS, 0, null, pcbCS, 0, 0);

                        context.PixelShaderSetConstantBuffers(g_iCBPSBind, new[] { this.g_pcbCS });

                        context.PixelShaderSetSamplers(0, new[] { this.g_pSampleStatePoint, this.g_pSampleStateLinear });

                        this.DrawFullScreenQuad(this.g_pFinalPassPS, pBackBufferDesc.Width, pBackBufferDesc.Height);
                    }
                    else
                    {
                        context.PixelShaderSetShaderResources(0, new[] { this.g_pTexRenderRV });

                        PSConstantBufferData pcbCS = new()
                        {
#if CS_FULL_PIXEL_REDUCITON
                            param0 = this.g_fCPUReduceResult / (pBackBufferDesc.Width * pBackBufferDesc.Height)
#else
                            param0 = this.g_fCPUReduceResult / (ToneMappingTexSize * ToneMappingTexSize)
#endif
                        };

                        context.UpdateSubresource(this.g_pcbCS, 0, null, pcbCS, 0, 0);

                        context.PixelShaderSetConstantBuffers(g_iCBPSBind, new[] { this.g_pcbCS });

                        this.DrawFullScreenQuad(this.g_pFinalPassForCPUReductionPS, pBackBufferDesc.Width, pBackBufferDesc.Height);
                    }
                }
                else //if ( this.PostProcessMode ==  PostProcessMode.PixelShader)
                {
                    context.PixelShaderSetShaderResources(0, new[]
                    {
                        this.g_pTexRenderRV,
                        this.g_apTexToneMapRV[0],
                        this.IsBloom ? this.g_apTexBloomRV[0] : null
                    });

                    context.PixelShaderSetSamplers(0, new[] { this.g_pSampleStatePoint, this.g_pSampleStateLinear });

                    this.DrawFullScreenQuad(this.g_pOldFinalPassPS, pBackBufferDesc.Width, pBackBufferDesc.Height);
                }

                context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null, null });
            }
        }

        private void DrawFullScreenQuad(D3D11PixelShader pPS, uint Width, uint Height)
        {
            var context = this.deviceResources.D3DContext;

            // Save the old viewport
            D3D11Viewport[] vpOld = context.RasterizerStageGetViewports();

            // Setup the viewport to match the backbuffer
            D3D11Viewport vp = new(0, 0, Width, Height, 0.0f, 1.0f);
            context.RasterizerStageSetViewports(new[] { vp });

            uint strides = ScreenVertex.Size;
            uint offsets = 0;
            D3D11Buffer[] pBuffers = { g_pScreenQuadVB };

            context.InputAssemblerSetInputLayout(this.g_pQuadLayout);
            context.InputAssemblerSetVertexBuffers(0, pBuffers, new[] { strides }, new[] { offsets });
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);

            context.VertexShaderSetShader(this.g_pQuadVS, null);
            context.PixelShaderSetShader(pPS, null);
            context.Draw(4, 0);

            // Restore the Old viewport
            context.RasterizerStageSetViewports(vpOld);
        }

        /// <summary>
        /// Measure the average luminance of the rendered skybox in PS path
        /// </summary>
        private void MeasureLuminancePS()
        {
            var context = this.deviceResources.D3DContext;

            //-------------------------------------------------------------------------
            // Initial sampling pass to convert the image to the log of the grayscale
            //-------------------------------------------------------------------------
            D3D11ShaderResourceView pTexSrc = this.g_pTexRenderRV;
            D3D11ShaderResourceView pTexDest = this.g_apTexToneMapRV[NumToneMapTextures - 1];
            D3D11RenderTargetView pSurfDest = this.g_apTexToneMapRTV[NumToneMapTextures - 1];

            D3D11Texture2DDesc descSrc = this.g_pTexRender.Description;
            D3D11Texture2DDesc descDest = this.g_apTexToneMap[NumToneMapTextures - 1].Description;

            context.OutputMergerSetRenderTargets(new[] { pSurfDest }, null);
            context.PixelShaderSetShaderResources(0, new[] { pTexSrc });
            context.PixelShaderSetSamplers(0, new[] { g_pSampleStatePoint });

            this.DrawFullScreenQuad(this.g_pDownScale2x2LumPS, descDest.Width, descDest.Height);

            context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });

            //-------------------------------------------------------------------------
            // Iterate through the remaining tone map textures
            //------------------------------------------------------------------------- 
            for (int i = NumToneMapTextures - 1; i > 0; i--)
            {
                // Cycle the textures
                pTexSrc = this.g_apTexToneMapRV[i];
                pTexDest = this.g_apTexToneMapRV[i - 1];
                pSurfDest = this.g_apTexToneMapRTV[i - 1];

                D3D11Texture2DDesc desc = this.g_apTexToneMap[i].Description;

                context.OutputMergerSetRenderTargets(new[] { pSurfDest }, null);
                context.PixelShaderSetShaderResources(0, new[] { pTexSrc });

                this.DrawFullScreenQuad(this.g_pDownScale3x3PS, desc.Width / 3, desc.Height / 3);

                context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            }
        }

        /// <summary>
        /// Bright pass for bloom effect in PS path
        /// </summary>
        private void BrightPassFilterPS()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.g_pTexBrightPassRTV }, null);
            context.PixelShaderSetShaderResources(0, new[] { this.g_pTexRenderRV, this.g_apTexToneMapRV[0] });
            context.PixelShaderSetSamplers(0, new[] { this.g_pSampleStatePoint });

            this.DrawFullScreenQuad(this.g_pDownScale3x3BrightPassPS, this.deviceResources.BackBufferWidth / 8, this.deviceResources.BackBufferHeight / 8);

            context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null });
        }

        private static float GaussianDistribution(float x, float y, float rho)
        {
            float g = 1.0f / (float)Math.Sqrt(2.0f * Math.PI * rho * rho);
            g *= (float)Math.Exp(-(x * x + y * y) / (2 * rho * rho));

            return g;
        }

        private static void GetSampleOffsets_Bloom_D3D11(uint dwD3DTexSize, float[] afTexCoordOffset, XMVector[] avColorWeight, float fDeviation, float fMultiplier)
        {
            int i = 0;
            float tu = 1.0f / dwD3DTexSize;

            // Fill the center texel
            float weight = 1.0f * GaussianDistribution(0, 0, fDeviation);
            avColorWeight[7] = new(weight, weight, weight, 1.0f);

            afTexCoordOffset[7] = 0.0f;

            // Fill one side
            for (i = 1; i < 8; i++)
            {
                weight = fMultiplier * GaussianDistribution((float)i, 0, fDeviation);
                afTexCoordOffset[7 - i] = -i * tu;

                avColorWeight[7 - i] = new(weight, weight, weight, 1.0f);
            }

            // Copy to the other side
            for (i = 8; i < 15; i++)
            {
                avColorWeight[i] = avColorWeight[14 - i];
                afTexCoordOffset[i] = -afTexCoordOffset[14 - i];
            }
        }

        private static void GetSampleWeights_D3D11(XMVector[] avColorWeight, float fDeviation, float fMultiplier)
        {
            // Fill the center texel
            float weight = 1.0f * GaussianDistribution(0, 0, fDeviation);
            avColorWeight[7] = new(weight, weight, weight, 1.0f);

            // Fill the right side
            for (int i = 1; i < 8; i++)
            {
                weight = fMultiplier * GaussianDistribution((float)i, 0, fDeviation);
                avColorWeight[7 - i] = new(weight, weight, weight, 1.0f);
            }

            // Copy to the left side
            for (int i = 8; i < 15; i++)
            {
                avColorWeight[i] = avColorWeight[14 - i];
            }
        }

        /// <summary>
        /// Blur using a separable convolution kernel in PS path
        /// </summary>
        private void BlurPS(
            uint dwWidth,
            uint dwHeight,
            D3D11ShaderResourceView pFromRV,
            D3D11ShaderResourceView pAuxRV,
            D3D11RenderTargetView pAuxRTV,
            D3D11RenderTargetView pToRTV)
        {
            var context = this.deviceResources.D3DContext;

            int i = 0;

            // Horizontal Blur
            BloomPSConstantBufferData pcbBloom = new()
            {
                avSampleOffsets = new XMVector[15],
                avSampleWeights = new XMVector[15]
            };

            float[] afSampleOffsets = new float[15];

            GetSampleOffsets_Bloom_D3D11(dwWidth, afSampleOffsets, pcbBloom.avSampleWeights, 3.0f, 1.25f);

            for (i = 0; i < 15; i++)
            {
                pcbBloom.avSampleOffsets[i] = new(afSampleOffsets[i], 0.0f, 0.0f, 0.0f);
            }

            context.UpdateSubresource(this.g_pcbBloom, 0, null, pcbBloom, 0, 0);
            context.PixelShaderSetConstantBuffers(g_iCBBloomPSBind, new[] { this.g_pcbBloom });

            context.OutputMergerSetRenderTargets(new[] { pAuxRTV }, null);

            context.PixelShaderSetShaderResources(0, new[] { pFromRV });
            context.PixelShaderSetSamplers(0, new[] { this.g_pSampleStatePoint });
            this.DrawFullScreenQuad(this.g_pBloomPS, dwWidth, dwHeight);
            context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null, null, null });

            // Vertical Blur
            GetSampleOffsets_Bloom_D3D11(dwHeight, afSampleOffsets, pcbBloom.avSampleWeights, 3.0f, 1.25f);
            for (i = 0; i < 15; i++)
            {
                pcbBloom.avSampleOffsets[i] = new(0.0f, afSampleOffsets[i], 0.0f, 0.0f);
            }

            context.UpdateSubresource(this.g_pcbBloom, 0, null, pcbBloom, 0, 0);
            context.PixelShaderSetConstantBuffers(g_iCBBloomPSBind, new[] { this.g_pcbBloom });

            context.OutputMergerSetRenderTargets(new[] { pToRTV }, null);

            context.PixelShaderSetShaderResources(0, new[] { pAuxRV });
            this.DrawFullScreenQuad(this.g_pBloomPS, dwWidth, dwHeight);
            context.PixelShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null, null, null });

            context.PixelShaderSetConstantBuffers(g_iCBBloomPSBind, new D3D11Buffer[] { null });
        }

        /// <summary>
        /// Bloom effect in PS path
        /// </summary>
        private void RenderBloomPS(in D3D11Texture2DDesc pBackBufferDesc)
        {
            this.BlurPS(
                pBackBufferDesc.Width / 8,
                pBackBufferDesc.Height / 8,
                this.g_pTexBrightPassRV,
                this.g_apTexBloomRV[1],
                this.g_apTexBloomRTV[1],
                this.g_apTexBloomRTV[0]);
        }

        /// <summary>
        /// Full screen blur effect in PS path
        /// </summary>
        private void FullScrBlurPS(in D3D11Texture2DDesc pBackBufferDesc)
        {
            this.BlurPS(
                pBackBufferDesc.Width,
                pBackBufferDesc.Height,
                this.g_pTexRenderRV,
                this.g_pTexBlurredRV,
                this.g_pTexBlurredRTV,
                this.g_pTexRenderRTV);
        }

        /// <summary>
        /// Helper function which makes CS invocation more convenient
        /// </summary>
        private void RunComputeShader<T>(
            D3D11ComputeShader pComputeShader,
            D3D11ShaderResourceView[] pShaderResourceViews,
            D3D11Buffer pCBCS,
            T pCSData,
            D3D11UnorderedAccessView pUnorderedAccessView,
            uint X,
            uint Y,
            uint Z)
            where T : struct
        {
            var context = this.deviceResources.D3DContext;

            context.ComputeShaderSetShader(pComputeShader, null);
            context.ComputeShaderSetShaderResources(0, pShaderResourceViews);
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { pUnorderedAccessView }, new uint[] { 0 });

            if (pCBCS)
            {
                context.UpdateSubresource(pCBCS, 0, null, pCSData, 0, 0);
                context.ComputeShaderSetConstantBuffers(0, new[] { pCBCS });
            }

            context.Dispatch(X, Y, Z);

            context.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new uint[] { 0 });
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null, null });
            context.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { null });
        }

        private static void Swap<T>(ref T left, ref T right)
        {
            (left, right) = (right, left);
        }

        /// <summary>
        /// Measure the average luminance of the rendered skybox in CS path
        /// </summary>
        /// <param name="pBackBufferDesc"></param>
        private void MeasureLuminanceCS(in D3D11Texture2DDesc pBackBufferDesc)
        {
            var context = this.deviceResources.D3DContext;

#if CS_FULL_PIXEL_REDUCITON
            uint dimx = (uint)Math.Ceiling(pBackBufferDesc.Width / 8.0f);
            dimx = (uint)Math.Ceiling(dimx / 2.0f);
            uint dimy = (uint)Math.Ceiling(pBackBufferDesc.Height / 8.0f);
            dimy = (uint)Math.Ceiling(dimy / 2.0f);
#else
            uint dimx = (uint)Math.Ceiling(ToneMappingTexSize / 8.0f);
            uint dimy = dimx;
#endif

            // First CS pass, reduce the render target texture into a 1D buffer
            {
                CSConstantBufferData cbCS = new()
                {
                    param0 = dimx,
                    param1 = dimy,
                    param2 = pBackBufferDesc.Width,
                    param3 = pBackBufferDesc.Height
                };

                RunComputeShader(
                    this.g_pReduceTo1DCS,
                    new[] { this.g_pTexRenderRV },
                    this.g_pcbCS,
                    cbCS,
                    g_pReductionUAView0,
                    dimx, dimy, 1);
            }

            // Reduction CS passes, the reduction result will be in the first element of g_pTex1DReduction1
            {
                if (!this.IsCPUReduction)
                {
                    uint dim = dimx * dimy;
                    uint nNumToReduce = dim;
                    dim = (uint)Math.Ceiling(dim / 128.0f);

                    if (nNumToReduce > 1)
                    {
                        for (; ; )
                        {
                            CSConstantBufferData cbCS = new()
                            {
                                param0 = nNumToReduce,
                                param1 = dim,
                                param2 = 0,
                                param3 = 0
                            };

                            RunComputeShader(this.g_pReduceToSingleCS,
                                new[] { this.g_pReductionRV0 },
                                              this.g_pcbCS,
                                              cbCS,
                                              this.g_pReductionUAView1,
                                              dim,
                                              1,
                                              1);

                            nNumToReduce = dim;
                            dim = (uint)Math.Ceiling(dim / 128.0f);

                            if (nNumToReduce == 1)
                                break;

                            Swap(ref this.g_pBufferReduction0, ref this.g_pBufferReduction1);
                            Swap(ref this.g_pReductionUAView0, ref this.g_pReductionUAView1);
                            Swap(ref this.g_pReductionRV0, ref this.g_pReductionRV1);
                        }
                    }
                    else
                    {
                        Swap(ref this.g_pBufferReduction0, ref this.g_pBufferReduction1);
                        Swap(ref this.g_pReductionUAView0, ref this.g_pReductionUAView1);
                        Swap(ref this.g_pReductionRV0, ref this.g_pReductionRV1);
                    }
                }
                else
                {
                    // read back to CPU and reduce on the CPU
                    D3D11Box box = new(0, 0, 0, dimx * dimy * 4, 1, 1);

                    context.CopySubresourceRegion(this.g_pBufferCPURead, 0, 0, 0, 0, this.g_pBufferReduction0, 0, box);

                    D3D11MappedSubResource MappedResource = context.Map(this.g_pBufferCPURead, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                    g_fCPUReduceResult = 0.0f;

                    byte[] data = new byte[4];

                    for (int i = 0; i < dimx * dimy; ++i)
                    {
                        Marshal.Copy(MappedResource.Data + i * 4, data, 0, 4);

                        g_fCPUReduceResult += BitConverter.ToSingle(data, 0);
                    }

                    context.Unmap(this.g_pBufferCPURead, 0);
                }
            }
        }

        /// <summary>
        /// Bloom effect in CS path
        /// </summary>
        private void BloomCS(in D3D11Texture2DDesc pBackBufferDesc)
        {
            var context = this.deviceResources.D3DContext;

            // Bright pass and horizontal blur
            FilterHorizontalConstantBufferData cbFilterHorizontal = new()
            {
                avSampleWeights = new XMVector[15]
            };

            GetSampleWeights_D3D11(cbFilterHorizontal.avSampleWeights, 3.0f, 1.25f);

            cbFilterHorizontal.outputwidth = pBackBufferDesc.Width / 8;
#if CS_FULL_PIXEL_REDUCITON
            cbFilterHorizontal.finverse = 1.0f / (pBackBufferDesc.Width * pBackBufferDesc.Height);
#else
            cbFilterHorizontal.finverse = 1.0f / (ToneMappingTexSize * ToneMappingTexSize);
#endif
            cbFilterHorizontal.inputsize = new(pBackBufferDesc.Width, pBackBufferDesc.Height);

            this.RunComputeShader(
                this.g_pBrightPassAndHorizFilterCS,
                new[] { this.g_pTexRenderRV, this.g_pReductionRV1 },
                this.g_pcbFilterCS,
                cbFilterHorizontal,
                this.g_apBufBloomUAV[1],
                (uint)Math.Ceiling((float)cbFilterHorizontal.outputwidth / (128 - 7 * 2)),
                pBackBufferDesc.Height / 8, 1);

            // Vertical blur
            FilterVerticalConstantBufferData cbFilterVertical = new()
            {
                avSampleWeights = new XMVector[15]
            };

            cbFilterVertical.outputsize = new(pBackBufferDesc.Width / 8, pBackBufferDesc.Height / 8);
            cbFilterVertical.inputsize = new(pBackBufferDesc.Width / 8, pBackBufferDesc.Height / 8);

            this.RunComputeShader(
                this.g_pVertFilterCS,
                new[] { this.g_apBufBloomRV[1] },
                this.g_pcbFilterCS,
                cbFilterVertical,
                this.g_apBufBloomUAV[0],
                pBackBufferDesc.Width / 8,
                (uint)Math.Ceiling((float)cbFilterVertical.outputsize.Y / (128 - 7 * 2)),
                1);
        }

        /// <summary>
        /// Full screen blur effect in CS path
        /// </summary>
        /// <param name="pBackBufferDesc"></param>
        private void FullScrBlurCS(in D3D11Texture2DDesc pBackBufferDesc)
        {
            FilterVerticalConstantBufferData cbFilterVertical = new()
            {
                avSampleWeights = new XMVector[15]
            };

            GetSampleWeights_D3D11(cbFilterVertical.avSampleWeights, 3.0f, 1.25f);

            cbFilterVertical.outputsize = new(pBackBufferDesc.Width, pBackBufferDesc.Height);
            cbFilterVertical.inputsize = new(pBackBufferDesc.Width, pBackBufferDesc.Height);

            this.RunComputeShader(
                this.g_pHorizFilterCS,
                new[] { null, this.g_pTexRenderRV },
                this.g_pcbFilterCS,
                cbFilterVertical,
                this.g_pBlurUAView0,
                (uint)Math.Ceiling((float)pBackBufferDesc.Width / (128 - 7 * 2)),
                pBackBufferDesc.Height,
                1);

            this.RunComputeShader(
                g_pVertFilterCS,
                new[] { this.g_pBlurRV0 },
                this.g_pcbFilterCS,
                cbFilterVertical,
                this.g_pBlurUAView1,
                pBackBufferDesc.Width,
                (uint)Math.Ceiling((float)pBackBufferDesc.Height / (128 - 7 * 2)),
                1);
        }

        /// <summary>
        /// Convert buffer result output from CS to a texture, used in CS path
        /// </summary>
        private void DumpToTexture(
            uint dwWidth,
            uint dwHeight,
            D3D11ShaderResourceView pFromRV,
            D3D11RenderTargetView pToRTV)
        {
            var context = this.deviceResources.D3DContext;

            context.PixelShaderSetShaderResources(0, new[] { pFromRV });
            context.OutputMergerSetRenderTargets(new[] { pToRTV }, null);

            XMUInt2 p = new(dwWidth, dwHeight);
            context.UpdateSubresource(this.g_pcbCS, 0, null, p, 0, 0);

            context.PixelShaderSetConstantBuffers(g_iCBPSBind, new[] { this.g_pcbCS });

            this.DrawFullScreenQuad(this.g_pDumpBufferPS, dwWidth, dwHeight);
        }
    }
}
