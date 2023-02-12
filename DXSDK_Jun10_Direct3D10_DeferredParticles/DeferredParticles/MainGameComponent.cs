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
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    class MainGameComponent : IGameComponent
    {
        public const int MaxBuildings = 5;
        public const int MaxFlashColors = 4;
        public const int MaxMushroomClouds = 8;
        public const int MaxGroundBursts = 23;
        public const int MaxParticleSystems = 30;
        public const int MaxFlashLights = 8;
        public const int MaxInstances = 200;

        private DeviceResources deviceResources;

        private SdkMeshFile g_WallMesh;
        private readonly SdkMeshFile[] g_ChunkMesh = new SdkMeshFile[BreakableWall.NumChunks];
        private readonly Building[] g_Building = Enumerable
            .Range(0, MaxBuildings)
            .Select(t => new Building())
            .ToArray();

        private readonly List<XMMatrix> g_BaseMeshMatrices = new List<XMMatrix>();
        private readonly List<XMMatrix>[] g_ChunkMeshMatrices = Enumerable
            .Range(0, BreakableWall.NumChunks)
            .Select(t => new List<XMMatrix>())
            .ToArray();

        private D3D11PixelShader g_CompositeParticlesPS;
        private D3D11VertexShader g_CompositeParticlesVS;
        private D3D11VertexShader g_MeshInstVS;
        private D3D11PixelShader g_MeshPS;
        private D3D11VertexShader g_MeshVS;
        private D3D11PixelShader g_RenderParticlesDeferredPS;
        private D3D11PixelShader g_RenderParticlesPS;
        private D3D11VertexShader g_RenderParticlesVS;

        private D3D11SamplerState g_sampler;
        private D3D11DepthStencilState g_EnableDepthDepthStencilState;
        private D3D11DepthStencilState g_DisableDepthDepthStencilState;
        private D3D11DepthStencilState g_DepthReadDepthStencilState;
        private D3D11BlendState g_DeferredBlending;
        private D3D11BlendState g_ForwardBlending;
        private D3D11BlendState g_CompositeBlending;
        private D3D11BlendState g_DisableBlending;

        private D3D11InputLayout g_pVertexLayout;
        private D3D11InputLayout g_pScreenQuadLayout;
        private D3D11InputLayout g_pMeshLayout;

        private int g_NumParticles = 200;
        private float g_fSpread = 4.0f;
        private float g_fStartSize = 0.0f;
        private float g_fEndSize = 10.0f;
        private float g_fSizeExponent = 128.0f;

        private float g_fMushroomCloudLifeSpan = 10.0f;
        private float g_fGroundBurstLifeSpan = 9.0f;
        private float g_fPopperLifeSpan = 9.0f;

        private float g_fMushroomStartSpeed = 20.0f;
        private float g_fStalkStartSpeed = 50.0f;
        private float g_fGroundBurstStartSpeed = 100.0f;
        private float g_fLandMineStartSpeed = 250.0f;

        private float g_fEndSpeed = 4.0f;
        private float g_fSpeedExponent = 32.0f;
        private float g_fFadeExponent = 4.0f;
        private float g_fRollAmount = 0.2f;
        private float g_fWindFalloff = 20.0f;
        private XMFloat3 g_vPosMul = new(1, 1, 1);
        private XMFloat3 g_vDirMul = new(1, 1, 1);
        private XMFloat3 g_vWindVel = new(-2.0f, 10.0f, 0);
        private XMFloat3 g_vGravity = new(0, -9.8f, 0.0f);

        private float g_fGroundPlane = 0.5f;
        private float g_fLightRaise = 1.0f;

        private float g_fWorldBounds = 100.0f;

        private XMVector[] g_vFlashColor = new XMVector[MaxFlashColors]
        {
            new XMVector( 1.0f, 0.5f, 0.00f, 0.9f ),
            new XMVector( 1.0f, 0.3f, 0.05f, 0.9f ),
            new XMVector( 1.0f, 0.4f, 0.00f, 0.9f ),
            new XMVector( 0.8f, 0.3f, 0.05f, 0.9f )
        };

        private XMVector g_vFlashAttenuation = new(0, 0.0f, 3.0f, 0);
        private XMVector g_vMeshLightAttenuation = new(0, 0, 1.5f, 0);
        private float g_fFlashLife = 0.50f;
        private float g_fFlashIntensity = 1000.0f;

        private int g_NumParticlesToDraw = 0;

        private ParticleSystem[] g_ppParticleSystem;
        private D3D11Buffer g_pParticleBuffer;
        private D3D11Buffer g_pScreenQuadVB;

        private D3D11Buffer g_instancedGlobalsConstantBuffer;
        private D3D11Buffer g_perFrameConstantBuffer;
        private D3D11Buffer g_glowLightsConstantBuffer;

        private D3D11Texture2D g_pOffscreenParticleTex;
        private D3D11ShaderResourceView g_pOffscreenParticleSRV;
        private D3D11RenderTargetView g_pOffscreenParticleRTV;
        private D3D11Texture2D g_pOffscreenParticleColorTex;
        private D3D11ShaderResourceView g_pOffscreenParticleColorSRV;
        private D3D11RenderTargetView g_pOffscreenParticleColorRTV;

        private D3D11ShaderResourceView g_pParticleTextureSRV;

        private float g_time;

        public MainGameComponent()
        {
            EyePosition = new(0.0f, 150.0f, 336.0f);

            WorldMatrix = XMMatrix.Identity;
            XMFloat3 vecEye = new(0.0f, 150.0f, 336.0f);
            XMFloat3 vecAt = new(0.0f, 0.0f, 0.0f);
            XMFloat3 vecUp = new(0.0f, 1.0f, 0.0f);
            ViewMatrix = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);

            var vLightDir = new XMFloat3(1, 1, 0);
            vLightDir = XMVector3.Normalize(vLightDir);
            LightDirection = vLightDir;
        }

        public XMFloat3 EyePosition { get; set; }

        public XMMatrix WorldMatrix { get; set; }

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public XMFloat3 LightDirection { get; set; }

        public bool RenderDeferred { get; set; } = true;

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = deviceResources.D3DDevice;
            var context = deviceResources.D3DContext;

            // Create shaders
            g_CompositeParticlesPS = device.CreatePixelShader(File.ReadAllBytes("CompositeParticlesPS.cso"), null);
            g_CompositeParticlesVS = device.CreateVertexShader(File.ReadAllBytes("CompositeParticlesVS.cso"), null);
            g_MeshInstVS = device.CreateVertexShader(File.ReadAllBytes("MeshInstVS.cso"), null);
            g_MeshPS = device.CreatePixelShader(File.ReadAllBytes("MeshPS.cso"), null);
            g_MeshVS = device.CreateVertexShader(File.ReadAllBytes("MeshVS.cso"), null);
            g_RenderParticlesDeferredPS = device.CreatePixelShader(File.ReadAllBytes("RenderParticlesDeferredPS.cso"), null);
            g_RenderParticlesPS = device.CreatePixelShader(File.ReadAllBytes("RenderParticlesPS.cso"), null);
            g_RenderParticlesVS = device.CreateVertexShader(File.ReadAllBytes("RenderParticlesVS.cso"), null);

            // Create states
            g_sampler = device.CreateSamplerState(new D3D11SamplerDesc(
                D3D11Filter.Anisotropic,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                D3D11Constants.DefaultMaxAnisotropy,
                D3D11ComparisonFunction.Never,
                null,
                0.0f,
                float.MaxValue));

            g_EnableDepthDepthStencilState = device.CreateDepthStencilState(D3D11DepthStencilDesc.Default with
            {
                IsDepthEnabled = true,
                DepthWriteMask = D3D11DepthWriteMask.All,
                DepthFunction = D3D11ComparisonFunction.LessEqual
            });

            g_DisableDepthDepthStencilState = device.CreateDepthStencilState(D3D11DepthStencilDesc.Default with
            {
                IsDepthEnabled = false,
                DepthWriteMask = D3D11DepthWriteMask.Zero,
                DepthFunction = D3D11ComparisonFunction.LessEqual
            });

            g_DepthReadDepthStencilState = device.CreateDepthStencilState(D3D11DepthStencilDesc.Default with
            {
                IsDepthEnabled = true,
                DepthWriteMask = D3D11DepthWriteMask.Zero,
                DepthFunction = D3D11ComparisonFunction.LessEqual
            });

            D3D11BlendDesc blendDesc;
            D3D11RenderTargetBlendDesc[] blendDescRenderTargets;

            blendDesc = D3D11BlendDesc.Default with
            {
                IsAlphaToCoverageEnabled = false
            };
            blendDescRenderTargets = blendDesc.GetRenderTargets();
            blendDescRenderTargets[0].IsBlendEnabled = true;
            blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDescRenderTargets[0].SourceBlend = D3D11BlendValue.One;
            blendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            blendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.One;
            blendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            blendDescRenderTargets[1].IsBlendEnabled = true;
            blendDescRenderTargets[1].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDescRenderTargets[1].SourceBlend = D3D11BlendValue.One;
            blendDescRenderTargets[1].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[1].BlendOperation = D3D11BlendOperation.Add;
            blendDescRenderTargets[1].SourceBlendAlpha = D3D11BlendValue.One;
            blendDescRenderTargets[1].DestinationBlendAlpha = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[1].BlendOperationAlpha = D3D11BlendOperation.Add;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            g_DeferredBlending = device.CreateBlendState(blendDesc);

            blendDesc = D3D11BlendDesc.Default with
            {
                IsAlphaToCoverageEnabled = false
            };
            blendDescRenderTargets = blendDesc.GetRenderTargets();
            blendDescRenderTargets[0].IsBlendEnabled = true;
            blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            blendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            blendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            blendDescRenderTargets[1].IsBlendEnabled = true;
            blendDescRenderTargets[1].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDescRenderTargets[1].SourceBlend = D3D11BlendValue.SourceAlpha;
            blendDescRenderTargets[1].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[1].BlendOperation = D3D11BlendOperation.Add;
            blendDescRenderTargets[1].SourceBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[1].DestinationBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[1].BlendOperationAlpha = D3D11BlendOperation.Add;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            g_ForwardBlending = device.CreateBlendState(blendDesc);

            blendDesc = D3D11BlendDesc.Default with
            {
                IsAlphaToCoverageEnabled = false
            };
            blendDescRenderTargets = blendDesc.GetRenderTargets();
            blendDescRenderTargets[0].IsBlendEnabled = true;
            blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            blendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            blendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            blendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            blendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            blendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            g_CompositeBlending = device.CreateBlendState(blendDesc);

            blendDesc = D3D11BlendDesc.Default with
            {
                IsAlphaToCoverageEnabled = false
            };
            blendDescRenderTargets = blendDesc.GetRenderTargets();
            blendDescRenderTargets[0].IsBlendEnabled = false;
            blendDesc.SetRenderTargets(blendDescRenderTargets);
            g_DisableBlending = device.CreateBlendState(blendDesc);

            // Create our vertex input layout
            D3D11InputElementDesc[] layout = new D3D11InputElementDesc[]
            {
                new("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new("LIFE", 0, DxgiFormat.R32Float, 0, 20, D3D11InputClassification.PerVertexData, 0),
                new("THETA", 0, DxgiFormat.R32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
                new("COLOR", 0, DxgiFormat.R8G8B8A8UNorm,  0, 28, D3D11InputClassification.PerVertexData, 0)
            };

            g_pVertexLayout = device.CreateInputLayout(layout, File.ReadAllBytes("RenderParticlesVS.cso"));

            D3D11InputElementDesc[] screenlayout = new D3D11InputElementDesc[]
            {
                new("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
            };

            g_pScreenQuadLayout = device.CreateInputLayout(screenlayout, File.ReadAllBytes("CompositeParticlesVS.cso"));

            D3D11InputElementDesc[] meshlayout = new D3D11InputElementDesc[]
            {
                new("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
            };

            g_pMeshLayout = device.CreateInputLayout(meshlayout, File.ReadAllBytes("MeshVS.cso"));

            // Load the meshes
            g_WallMesh = SdkMeshFile.FromFile(device, context, "wallsegment.sdkmesh");
            g_ChunkMesh[0] = SdkMeshFile.FromFile(device, context, "wallchunk0.sdkmesh");
            g_ChunkMesh[1] = SdkMeshFile.FromFile(device, context, "wallchunk1.sdkmesh");
            g_ChunkMesh[2] = SdkMeshFile.FromFile(device, context, "wallchunk2.sdkmesh");
            g_ChunkMesh[3] = SdkMeshFile.FromFile(device, context, "wallchunk3.sdkmesh");
            g_ChunkMesh[4] = SdkMeshFile.FromFile(device, context, "wallchunk4.sdkmesh");
            g_ChunkMesh[5] = SdkMeshFile.FromFile(device, context, "wallchunk5.sdkmesh");
            g_ChunkMesh[6] = SdkMeshFile.FromFile(device, context, "wallchunk6.sdkmesh");
            g_ChunkMesh[7] = SdkMeshFile.FromFile(device, context, "wallchunk7.sdkmesh");
            g_ChunkMesh[8] = SdkMeshFile.FromFile(device, context, "wallchunk8.sdkmesh");

            // Buildings
            g_Building[0].CreateBuilding(new XMFloat3(0, 0, 0), 2.0f, 50, 0, 50);

            float fBuildingRange = g_fWorldBounds - 20.0f;

            for (int i = 1; i < MaxBuildings; i++)
            {
                XMFloat3 vCenter = new()
                {
                    X = Particles.RPercent() * fBuildingRange,
                    Y = 0,
                    Z = Particles.RPercent() * fBuildingRange
                };

                uint x = ((uint)Particles.Rand() % 2) + 2;
                uint y = ((uint)Particles.Rand() % 2) + 3;
                uint z = ((uint)Particles.Rand() % 2) + 2;
                g_Building[i].CreateBuilding(vCenter, 2.0f, x * 2, y * 2, z * 2);
            }

            ResetBuildings();

            // Particle system
            int NumStalkParticles = 500;
            int NumGroundExpParticles = 345;
            int NumLandMineParticles = 125;
            int MaxParticles =
                MaxMushroomClouds * (g_NumParticles + NumStalkParticles)
                + (MaxGroundBursts - MaxMushroomClouds) * NumGroundExpParticles
                + (MaxParticleSystems - MaxGroundBursts) * NumLandMineParticles;
            Particles.CreateParticleArray(MaxParticles);

            XMVector vColor0 = new(1.0f, 1.0f, 1.0f, 1);
            XMVector vColor1 = new(0.6f, 0.6f, 0.6f, 1);

            g_ppParticleSystem = new ParticleSystem[MaxParticleSystems];

            g_NumParticlesToDraw = 0;
            for (int i = 0; i < MaxMushroomClouds; i += 2)
            {
                XMFloat3 vLocation = new()
                {
                    X = Particles.RPercent() * 50.0f,
                    Y = g_fGroundPlane,
                    Z = Particles.RPercent() * 50.0f
                };

                g_ppParticleSystem[i] = new MushroomParticleSystem
                {
                    NewExplosion = NewExplosion
                };
                g_ppParticleSystem[i].CreateParticleSystem(g_NumParticles);
                g_ppParticleSystem[i].SetSystemAttributes(
                    vLocation,
                    g_fSpread,
                    g_fMushroomCloudLifeSpan,
                    g_fFadeExponent,
                    g_fStartSize,
                    g_fEndSize,
                    g_fSizeExponent,
                    g_fMushroomStartSpeed,
                    g_fEndSpeed,
                    g_fSpeedExponent,
                    g_fRollAmount,
                    g_fWindFalloff,
                    1,
                    0.0f,
                    new XMFloat3(0, 0, 0),
                    new XMFloat3(0, 0, 0),
                    vColor0,
                    vColor1,
                    g_vPosMul,
                    g_vDirMul);

                g_NumParticlesToDraw += g_NumParticles;

                g_ppParticleSystem[i + 1] = new StalkParticleSystem
                {
                    NewExplosion = NewExplosion
                };
                g_ppParticleSystem[i + 1].CreateParticleSystem(NumStalkParticles);
                g_ppParticleSystem[i + 1].SetSystemAttributes(
                    vLocation,
                    15.0f,
                    g_fMushroomCloudLifeSpan,
                    g_fFadeExponent * 2.0f,
                    g_fStartSize * 0.5f,
                    g_fEndSize * 0.5f,
                    g_fSizeExponent,
                    g_fStalkStartSpeed,
                    -1.0f,
                    g_fSpeedExponent,
                    g_fRollAmount,
                    g_fWindFalloff,
                    1,
                    0.0f,
                    new XMFloat3(0, 0, 0),
                    new XMFloat3(0, 0, 0),
                    vColor0,
                    vColor1,
                    new XMFloat3(1, 0.1f, 1),
                    new XMFloat3(1, 0.1f, 1));

                g_NumParticlesToDraw += NumStalkParticles;
            }

            for (int i = MaxMushroomClouds; i < MaxGroundBursts; i++)
            {
                XMFloat3 vLocation = new()
                {
                    X = Particles.RPercent() * 50.0f,
                    Y = g_fGroundPlane,
                    Z = Particles.RPercent() * 50.0f
                };

                g_ppParticleSystem[i] = new GroundBurstParticleSystem
                {
                    NewExplosion = NewExplosion
                };
                g_ppParticleSystem[i].CreateParticleSystem(NumGroundExpParticles);
                g_ppParticleSystem[i].SetSystemAttributes(
                    vLocation,
                    1.0f,
                    g_fGroundBurstLifeSpan,
                    g_fFadeExponent,
                    0.5f,
                    8.0f,
                    1.0f,
                    g_fGroundBurstStartSpeed,
                    g_fEndSpeed,
                    4.0f,
                    g_fRollAmount,
                    1.0f,
                    30,
                    100.0f,
                    new XMFloat3(0, 0.5f, 0),
                    new XMFloat3(1.0f, 0.5f, 1.0f),
                    vColor0,
                    vColor1,
                    g_vPosMul,
                    g_vDirMul);

                g_NumParticlesToDraw += NumGroundExpParticles;
            }

            for (int i = MaxGroundBursts; i < MaxParticleSystems; i++)
            {
                XMFloat3 vLocation = new()
                {
                    X = Particles.RPercent() * 50.0f,
                    Y = g_fGroundPlane,
                    Z = Particles.RPercent() * 50.0f
                };

                g_ppParticleSystem[i] = new LandMineParticleSystem
                {
                    NewExplosion = NewExplosion
                };
                g_ppParticleSystem[i].CreateParticleSystem(NumLandMineParticles);
                g_ppParticleSystem[i].SetSystemAttributes(
                    vLocation,
                    1.5f,
                    g_fPopperLifeSpan,
                    g_fFadeExponent,
                    1.0f,
                    6.0f,
                    1.0f,
                    g_fLandMineStartSpeed,
                    g_fEndSpeed,
                    2.0f,
                    g_fRollAmount,
                    4.0f,
                    0,
                    70.0f,
                    new XMFloat3(0, 0.8f, 0),
                    new XMFloat3(0.3f, 0.2f, 0.3f),
                    vColor0,
                    vColor1,
                    g_vPosMul,
                    g_vDirMul);

                g_NumParticlesToDraw += NumGroundExpParticles;
            }

            g_pParticleBuffer = device.CreateBuffer(new D3D11BufferDesc(
                ParticleVertex.Size * 6 * (uint)g_NumParticlesToDraw,
                D3D11BindOptions.VertexBuffer,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.None,
                0));

            DdsDirectX.CreateTexture("DeferredParticle.dds", device, context, out g_pParticleTextureSRV);

            // Create the screen quad
            XMFloat3[] verts = new XMFloat3[4]
            {
                new XMFloat3( -1, -1, 0.5f ),
                new XMFloat3( -1, 1, 0.5f ),
                new XMFloat3( 1, -1, 0.5f ),
                new XMFloat3( 1, 1, 0.5f )
            };

            g_pScreenQuadVB = device.CreateBuffer(
                new D3D11BufferDesc(
                    4 * (uint)Marshal.SizeOf<XMFloat3>(),
                    D3D11BindOptions.VertexBuffer,
                    D3D11Usage.Immutable,
                    D3D11CpuAccessOptions.None,
                    D3D11ResourceMiscOptions.None,
                    0),
                verts,
                (uint)Marshal.SizeOf<XMFloat3>(),
                0);

            // Create constants buffers
            this.g_instancedGlobalsConstantBuffer = device.CreateBuffer(new D3D11BufferDesc(InstancedGlobalsConstantBuffer.Size, D3D11BindOptions.ConstantBuffer));
            this.g_perFrameConstantBuffer = device.CreateBuffer(new D3D11BufferDesc(PerFrameConstantBuffer.Size, D3D11BindOptions.ConstantBuffer));
            this.g_glowLightsConstantBuffer = device.CreateBuffer(new D3D11BufferDesc(GlowLightsConstantBuffer.Size, D3D11BindOptions.ConstantBuffer));
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref g_CompositeParticlesPS);
            D3D11Utils.DisposeAndNull(ref g_CompositeParticlesVS);
            D3D11Utils.DisposeAndNull(ref g_MeshInstVS);
            D3D11Utils.DisposeAndNull(ref g_MeshPS);
            D3D11Utils.DisposeAndNull(ref g_MeshVS);
            D3D11Utils.DisposeAndNull(ref g_RenderParticlesDeferredPS);
            D3D11Utils.DisposeAndNull(ref g_RenderParticlesPS);
            D3D11Utils.DisposeAndNull(ref g_RenderParticlesVS);

            D3D11Utils.DisposeAndNull(ref g_sampler);
            D3D11Utils.DisposeAndNull(ref g_EnableDepthDepthStencilState);
            D3D11Utils.DisposeAndNull(ref g_DisableDepthDepthStencilState);
            D3D11Utils.DisposeAndNull(ref g_DepthReadDepthStencilState);
            D3D11Utils.DisposeAndNull(ref g_DeferredBlending);
            D3D11Utils.DisposeAndNull(ref g_ForwardBlending);
            D3D11Utils.DisposeAndNull(ref g_CompositeBlending);
            D3D11Utils.DisposeAndNull(ref g_DisableBlending);

            D3D11Utils.DisposeAndNull(ref g_pVertexLayout);
            D3D11Utils.DisposeAndNull(ref g_pScreenQuadLayout);
            D3D11Utils.DisposeAndNull(ref g_pMeshLayout);

            g_WallMesh?.Release();
            for (uint i = 0; i < BreakableWall.NumChunks; i++)
            {
                g_ChunkMesh[i]?.Release();
            }

            D3D11Utils.DisposeAndNull(ref g_pScreenQuadVB);
            D3D11Utils.DisposeAndNull(ref g_pParticleBuffer);
            D3D11Utils.DisposeAndNull(ref g_pParticleTextureSRV);

            D3D11Utils.DisposeAndNull(ref g_instancedGlobalsConstantBuffer);
            D3D11Utils.DisposeAndNull(ref g_perFrameConstantBuffer);
            D3D11Utils.DisposeAndNull(ref g_glowLightsConstantBuffer);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 2.0f, 4000.0f);

            var device = this.deviceResources.D3DDevice;

            // Create the offscreen particle buffer
            D3D11Texture2DDesc Desc = new()
            {
                Width = deviceResources.BackBufferWidth,
                Height = deviceResources.BackBufferHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiFormat.R16G16B16A16Float,
                SampleDesc = new DxgiSampleDesc(1, 0),
                Usage = D3D11Usage.Default,
                BindOptions = D3D11BindOptions.RenderTarget | D3D11BindOptions.ShaderResource,
                CpuAccessOptions = D3D11CpuAccessOptions.None,
                MiscOptions = D3D11ResourceMiscOptions.None
            };

            g_pOffscreenParticleTex = device.CreateTexture2D(Desc);

            Desc.Format = DxgiFormat.R8G8B8A8UNorm;
            g_pOffscreenParticleColorTex = device.CreateTexture2D(Desc);

            D3D11RenderTargetViewDesc RTVDesc = new()
            {
                Format = DxgiFormat.R16G16B16A16Float,
                ViewDimension = D3D11RtvDimension.Texture2D,
                Texture2D = new D3D11Texture2DRtv
                {
                    MipSlice = 0
                }
            };

            g_pOffscreenParticleRTV = device.CreateRenderTargetView(g_pOffscreenParticleTex, RTVDesc);

            RTVDesc.Format = DxgiFormat.R8G8B8A8UNorm;
            g_pOffscreenParticleColorRTV = device.CreateRenderTargetView(g_pOffscreenParticleColorTex, RTVDesc);

            D3D11ShaderResourceViewDesc SRVDesc = new()
            {
                Format = DxgiFormat.R16G16B16A16Float,
                ViewDimension = D3D11SrvDimension.Texture2D,
                Texture2D = new D3D11Texture2DSrv
                {
                    MostDetailedMip = 0,
                    MipLevels = Desc.MipLevels
                }
            };

            g_pOffscreenParticleSRV = device.CreateShaderResourceView(g_pOffscreenParticleTex, SRVDesc);

            SRVDesc.Format = DxgiFormat.R8G8B8A8UNorm;
            g_pOffscreenParticleColorSRV = device.CreateShaderResourceView(g_pOffscreenParticleColorTex, SRVDesc);
        }

        public void ReleaseWindowSizeDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleTex);
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleSRV);
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleRTV);
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleColorTex);
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleColorSRV);
            D3D11Utils.DisposeAndNull(ref g_pOffscreenParticleColorRTV);
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            var device = deviceResources.D3DDevice;
            var context = deviceResources.D3DContext;

            g_time = (float)timer.TotalSeconds;

            XMFloat3 vEye = this.EyePosition;
            XMMatrix mView = this.ViewMatrix;

            XMFloat3 vRight = XMVector3.Normalize(new XMFloat3(mView.M11, mView.M21, mView.M31));
            XMFloat3 vUp = XMVector3.Normalize(new XMFloat3(mView.M12, mView.M22, mView.M32));
            XMFloat3 vForward = XMVector3.Normalize(new XMFloat3(mView.M13, mView.M23, mView.M33));

            int NumActiveSystems = 0;
            XMVector[] vGlowLightPosIntensity = new XMVector[MaxParticleSystems];
            XMVector[] vGlowLightColor = new XMVector[MaxParticleSystems];

            // Advanced building pieces
            for (int i = 0; i < MaxBuildings; i++)
            {
                g_Building[i].AdvancePieces(timer.ElapsedSeconds, g_vGravity);
            }

            // Advance the system
            for (int i = 0; i < MaxParticleSystems; i++)
            {
                g_ppParticleSystem[i].AdvanceSystem(timer.TotalSeconds, timer.ElapsedSeconds, vRight, vUp, g_vWindVel, g_vGravity);
            }

            ParticleVertex[] pVerts = new ParticleVertex[g_pParticleBuffer.Description.ByteWidth / ParticleVertex.Size];
            Particles.CopyParticlesToVertexBuffer(pVerts, vEye, vRight, vUp);
            context.UpdateSubresource(g_pParticleBuffer, 0, null, pVerts, ParticleVertex.Size, 0);

            for (int i = 0; i < MaxMushroomClouds; i += 2)
            {
                float fCurrentTime = g_ppParticleSystem[i].GetCurrentTime();
                float fLifeSpan = g_ppParticleSystem[i].GetLifeSpan();

                if (fCurrentTime > fLifeSpan)
                {
                    XMFloat3 vCenter = new()
                    {
                        X = Particles.RPercent() * g_fWorldBounds,
                        Y = g_fGroundPlane,
                        Z = Particles.RPercent() * g_fWorldBounds
                    };

                    float fStartTime = -Math.Abs(Particles.RPercent()) * 4.0f;
                    XMVector vFlashColor = g_vFlashColor[Particles.Rand() % MaxFlashColors];

                    g_ppParticleSystem[i].SetCenter(vCenter);
                    g_ppParticleSystem[i].SetStartTime(fStartTime);
                    g_ppParticleSystem[i].SetFlashColor(vFlashColor);
                    g_ppParticleSystem[i].Init();

                    g_ppParticleSystem[i + 1].SetCenter(vCenter);
                    g_ppParticleSystem[i + 1].SetStartTime(fStartTime);
                    g_ppParticleSystem[i + 1].SetFlashColor(vFlashColor);
                    g_ppParticleSystem[i + 1].Init();
                }
                else if (fCurrentTime > 0.0f && fCurrentTime < g_fFlashLife && NumActiveSystems < MaxFlashLights)
                {
                    XMFloat3 vCenter = g_ppParticleSystem[i].GetCenter();
                    XMVector vFlashColor = g_ppParticleSystem[i].GetFlashColor();

                    float fIntensity = g_fFlashIntensity * ((g_fFlashLife - fCurrentTime) / g_fFlashLife);
                    vGlowLightPosIntensity[NumActiveSystems] = new XMVector(
                        vCenter.X,
                        vCenter.Y + g_fLightRaise,
                        vCenter.Z,
                        fIntensity);
                    vGlowLightColor[NumActiveSystems] = vFlashColor;
                    NumActiveSystems++;
                }
            }

            // Ground bursts
            for (int i = MaxMushroomClouds; i < MaxGroundBursts; i++)
            {
                float fCurrentTime = g_ppParticleSystem[i].GetCurrentTime();
                float fLifeSpan = g_ppParticleSystem[i].GetLifeSpan();

                if (fCurrentTime > fLifeSpan)
                {
                    XMFloat3 vCenter = new()
                    {
                        X = Particles.RPercent() * g_fWorldBounds,
                        Y = g_fGroundPlane,
                        Z = Particles.RPercent() * g_fWorldBounds
                    };

                    float fStartTime = -Math.Abs(Particles.RPercent()) * 4.0f;
                    XMVector vFlashColor = g_vFlashColor[Particles.Rand() % MaxFlashColors];

                    float fStartSpeed = g_fGroundBurstStartSpeed + Particles.RPercent() * 30.0f;
                    g_ppParticleSystem[i].SetCenter(vCenter);
                    g_ppParticleSystem[i].SetStartTime(fStartTime);
                    g_ppParticleSystem[i].SetStartSpeed(fStartSpeed);
                    g_ppParticleSystem[i].SetFlashColor(vFlashColor);
                    g_ppParticleSystem[i].Init();
                }
                else if (fCurrentTime > 0.0f && fCurrentTime < g_fFlashLife && NumActiveSystems < MaxFlashLights)
                {
                    XMFloat3 vCenter = g_ppParticleSystem[i].GetCenter();
                    XMVector vFlashColor = g_ppParticleSystem[i].GetFlashColor();

                    float fIntensity = g_fFlashIntensity * ((g_fFlashLife - fCurrentTime) / g_fFlashLife);
                    vGlowLightPosIntensity[NumActiveSystems] = new XMVector(
                        vCenter.X,
                        vCenter.Y + g_fLightRaise,
                        vCenter.Z,
                        fIntensity);
                    vGlowLightColor[NumActiveSystems] = vFlashColor;
                    NumActiveSystems++;
                }
            }

            // Land mines
            for (int i = MaxGroundBursts; i < MaxParticleSystems; i++)
            {
                float fCurrentTime = g_ppParticleSystem[i].GetCurrentTime();
                float fLifeSpan = g_ppParticleSystem[i].GetLifeSpan();

                if (fCurrentTime > fLifeSpan)
                {
                    XMFloat3 vCenter = new()
                    {
                        X = Particles.RPercent() * g_fWorldBounds,
                        Y = g_fGroundPlane,
                        Z = Particles.RPercent() * g_fWorldBounds
                    };
                    float fStartTime = -Math.Abs(Particles.RPercent()) * 4.0f;
                    XMVector vFlashColor = g_vFlashColor[Particles.Rand() % MaxFlashColors];

                    float fStartSpeed = g_fLandMineStartSpeed + Particles.RPercent() * 100.0f;
                    g_ppParticleSystem[i].SetCenter(vCenter);
                    g_ppParticleSystem[i].SetStartTime(fStartTime);
                    g_ppParticleSystem[i].SetStartSpeed(fStartSpeed);
                    g_ppParticleSystem[i].SetFlashColor(vFlashColor);
                    g_ppParticleSystem[i].Init();
                }
                else if (fCurrentTime > 0.0f && fCurrentTime < g_fFlashLife && NumActiveSystems < MaxFlashLights)
                {
                    XMFloat3 vCenter = g_ppParticleSystem[i].GetCenter();
                    XMVector vFlashColor = g_ppParticleSystem[i].GetFlashColor();

                    float fIntensity = g_fFlashIntensity * ((g_fFlashLife - fCurrentTime) / g_fFlashLife);
                    vGlowLightPosIntensity[NumActiveSystems] = new XMVector(
                        vCenter.X,
                        vCenter.Y + g_fLightRaise,
                        vCenter.Z,
                        fIntensity);
                    vGlowLightColor[NumActiveSystems] = vFlashColor;
                    NumActiveSystems++;
                }
            }

            // Setup light variables
            GlowLightsConstantBuffer cbGlowLights = new()
            {
                g_NumGlowLights = (uint)NumActiveSystems,
                g_vGlowLightPosIntensity = vGlowLightPosIntensity,
                g_vGlowLightColor = vGlowLightColor,
                g_vGlowLightAttenuation = g_vFlashAttenuation,
                g_vMeshLightAttenuation = g_vMeshLightAttenuation
            };

            context.UpdateSubresource(g_glowLightsConstantBuffer, 0, null, cbGlowLights, 0, 0);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Get the projection & view matrix from the camera class
            XMFloat3 vEyePt = this.EyePosition;
            XMVector vLightDir = this.LightDirection;
            XMMatrix mWorld = XMMatrix.Identity;
            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;
            XMMatrix mWorldViewProjection = mView * mProj;
            XMMatrix mViewProj = mView * mProj;
            XMMatrix mInvViewProj = mViewProj.Inverse();
            XMMatrix mSceneWorld = XMMatrix.Scaling(20, 20, 20);
            XMMatrix mSceneWVP = mSceneWorld * mViewProj;

            // Per frame variables
            PerFrameConstantBuffer cbPerFrame = new()
            {
                g_fTime = this.g_time,
                g_LightDir = vLightDir,
                g_vEyePt = vEyePt,
                g_vRight = XMVector3.Normalize(new XMFloat3(mView.M11, mView.M21, mView.M31)),
                g_vUp = XMVector3.Normalize(new XMFloat3(mView.M12, mView.M22, mView.M32)),
                g_vForward = XMVector3.Normalize(new XMFloat3(mView.M13, mView.M23, mView.M33)),
                g_mWorldViewProjection = mSceneWVP.Transpose(),
                g_mViewProj = mViewProj.Transpose(),
                g_mInvViewProj = mInvViewProj.Transpose(),
                g_mWorld = mSceneWorld.Transpose()
            };

            context.UpdateSubresource(g_perFrameConstantBuffer, 0, null, cbPerFrame, 0, 0);

            context.VertexShaderSetConstantBuffers(0, new[] { g_glowLightsConstantBuffer, g_perFrameConstantBuffer });
            context.PixelShaderSetConstantBuffers(0, new[] { g_glowLightsConstantBuffer, g_perFrameConstantBuffer });

            // Gather up the instance matrices for the buildings and pieces
            g_BaseMeshMatrices.Clear();

            for (int i = 0; i < BreakableWall.NumChunks; i++)
            {
                g_ChunkMeshMatrices[i].Clear();
            }

            // Get matrices
            for (int i = 0; i < MaxBuildings; i++)
            {
                g_Building[i].CollectBaseMeshMatrices(g_BaseMeshMatrices);

                for (uint c = 0; c < BreakableWall.NumChunks; c++)
                {
                    g_Building[i].CollectChunkMeshMatrices(c, g_ChunkMeshMatrices[c]);
                }
            }

            // Set our input layout
            context.InputAssemblerSetInputLayout(g_pMeshLayout);
            context.PixelShaderSetSamplers(0, new[] { g_sampler });

            InstancedGlobalsConstantBuffer cbInstancedGlobals = new()
            {
                g_mWorldInst = new XMMatrix[MaxInstances]
            };

            int NumMeshes = 0;
            int numrendered = 0;

            // Intact walls
            NumMeshes = g_BaseMeshMatrices.Count;
            numrendered = 0;
            while (numrendered < NumMeshes)
            {
                int NumToRender = Math.Min(MaxInstances, NumMeshes - numrendered);

                for (int i = 0; i < NumToRender; i++)
                {
                    cbInstancedGlobals.g_mWorldInst[i] = g_BaseMeshMatrices[numrendered + i].Transpose();
                }

                context.UpdateSubresource(g_instancedGlobalsConstantBuffer, 0, null, cbInstancedGlobals, 0, 0);
                context.VertexShaderSetConstantBuffers(2, new[] { g_instancedGlobalsConstantBuffer });
                context.PixelShaderSetConstantBuffers(2, new[] { g_instancedGlobalsConstantBuffer });

                RenderMeshInstTechnique();
                RenderInstanced(g_WallMesh, NumToRender);

                numrendered += NumToRender;
            }

            // Chunks
            for (uint c = 0; c < BreakableWall.NumChunks; c++)
            {
                NumMeshes = g_ChunkMeshMatrices[c].Count;
                numrendered = 0;
                while (numrendered < NumMeshes)
                {
                    int NumToRender = Math.Min(MaxInstances, NumMeshes - numrendered);

                    for (int i = 0; i < NumToRender; i++)
                    {
                        cbInstancedGlobals.g_mWorldInst[i] = g_ChunkMeshMatrices[c][numrendered + i].Transpose();
                    }

                    context.UpdateSubresource(g_instancedGlobalsConstantBuffer, 0, null, cbInstancedGlobals, 0, 0);
                    context.VertexShaderSetConstantBuffers(2, new[] { g_instancedGlobalsConstantBuffer });
                    context.PixelShaderSetConstantBuffers(2, new[] { g_instancedGlobalsConstantBuffer });

                    RenderMeshInstTechnique();
                    RenderInstanced(g_ChunkMesh[c], NumToRender);

                    numrendered += NumToRender;
                }
            }

            // Render particles
            cbPerFrame.g_mWorldViewProjection = mWorldViewProjection.Transpose();
            cbPerFrame.g_mWorld = mWorld.Transpose();
            context.UpdateSubresource(g_perFrameConstantBuffer, 0, null, cbPerFrame, 0, 0);

            if (this.RenderDeferred)
            {
                RenderParticlesIntoBuffer();
                CompositeParticlesIntoScene();
            }
            else
            {
                RenderParticlesTechnique();
                RenderParticles();
            }
        }

        private void RenderInstanced(SdkMeshFile pMesh, int NumInstances)
        {
            var context = this.deviceResources.D3DContext;

            SdkMeshMesh mesh = pMesh.Meshes[0];

            D3D11Buffer[] vb = new D3D11Buffer[mesh.VertexBuffers.Length];
            uint[] strides = new uint[mesh.VertexBuffers.Length];
            uint[] offsets = new uint[mesh.VertexBuffers.Length];

            for (int i = 0; i < mesh.VertexBuffers.Length; i++)
            {
                vb[i] = mesh.VertexBuffers[i].Buffer;
                strides[i] = mesh.VertexBuffers[i].StrideBytes;
                offsets[i] = 0;
            }

            D3D11Buffer ib = mesh.IndexBuffer.Buffer;
            DxgiFormat ibFormat = mesh.IndexBuffer.IndexFormat;

            context.InputAssemblerSetVertexBuffers(0, vb, strides, offsets);
            context.InputAssemblerSetIndexBuffer(ib, ibFormat, 0);

            foreach (SdkMeshSubset subset in mesh.Subsets)
            {
                context.InputAssemblerSetPrimitiveTopology(subset.PrimitiveTopology);

                SdkMeshMaterial material = pMesh.Materials[subset.MaterialIndex];

                context.PixelShaderSetShaderResources(0, new[] { material.DiffuseTextureView });

                //context.DrawIndexed((uint)subset.IndexCount, (uint)subset.IndexStart, subset.VertexStart);
                context.DrawIndexedInstanced((uint)subset.IndexCount, (uint)NumInstances, (uint)subset.IndexStart, subset.VertexStart, 0);
            }
        }

        /// <summary>
        /// Render particles
        /// </summary>
        private void RenderParticles()
        {
            var context = this.deviceResources.D3DContext;

            //IA setup
            context.InputAssemblerSetInputLayout(g_pVertexLayout);

            context.InputAssemblerSetVertexBuffers(0, new[] { g_pParticleBuffer }, new uint[] { ParticleVertex.Size }, new uint[] { 0 });
            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.PixelShaderSetShaderResources(0, new[] { g_pParticleTextureSRV });

            //Render
            g_NumParticlesToDraw = Particles.GetNumActiveParticles();
            context.Draw(6 * (uint)g_NumParticlesToDraw, 0);
        }

        /// <summary>
        /// Render particles into the offscreen buffer
        /// </summary>
        private void RenderParticlesIntoBuffer()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the new render target
            float[] color = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
            context.ClearRenderTargetView(g_pOffscreenParticleRTV, color);
            context.ClearRenderTargetView(g_pOffscreenParticleColorRTV, color);

            // get the old render targets
            context.OutputMergerGetRenderTargets(2, out D3D11RenderTargetView[] pOldRTV, out D3D11DepthStencilView pOldDSV);

            // Set the new render targets
            context.OutputMergerSetRenderTargets(new[] { g_pOffscreenParticleRTV, g_pOffscreenParticleColorRTV }, pOldDSV);

            // Render the particles
            RenderParticlesToBufferTechnique();
            RenderParticles();

            // restore the original render targets
            context.OutputMergerSetRenderTargets(pOldRTV, pOldDSV);
            D3D11Utils.DisposeAndNull(ref pOldRTV[0]);
            D3D11Utils.DisposeAndNull(ref pOldRTV[1]);
            D3D11Utils.DisposeAndNull(ref pOldDSV);
        }

        /// <summary>
        /// Composite offscreen particle buffer back into the scene
        /// </summary>
        private void CompositeParticlesIntoScene()
        {
            var context = this.deviceResources.D3DContext;

            // Render the particles
            CompositeParticlesToSceneTechnique();

            //IA setup
            context.InputAssemblerSetInputLayout(g_pScreenQuadLayout);

            context.InputAssemblerSetVertexBuffers(0, new[] { g_pScreenQuadVB }, new[] { (uint)Marshal.SizeOf<XMFloat3>() }, new uint[] { 0 });
            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleStrip);

            context.PixelShaderSetShaderResources(0, new[] { g_pOffscreenParticleSRV, g_pOffscreenParticleColorSRV });

            //Render
            context.Draw(4, 0);

            // Un-set this resource, as it's associated with an OM output
            context.PixelShaderSetShaderResources(1, new D3D11ShaderResourceView[] { null });
        }

        private void RenderParticlesToBufferTechnique()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(g_RenderParticlesVS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(g_RenderParticlesDeferredPS, null);
            context.OutputMergerSetBlendState(g_DeferredBlending, null, 0xffffffff);
            context.OutputMergerSetDepthStencilState(g_DepthReadDepthStencilState, 0);
        }

        private void CompositeParticlesToSceneTechnique()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(g_CompositeParticlesVS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(g_CompositeParticlesPS, null);
            context.OutputMergerSetBlendState(g_CompositeBlending, null, 0xffffffff);
            //context.OutputMergerSetBlendState(g_DisableBlending, null, 0xffffffff);
            context.OutputMergerSetDepthStencilState(g_DisableDepthDepthStencilState, 0);
        }

        private void RenderParticlesTechnique()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(g_RenderParticlesVS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(g_RenderParticlesPS, null);
            context.OutputMergerSetBlendState(g_ForwardBlending, null, 0xffffffff);
            context.OutputMergerSetDepthStencilState(g_DepthReadDepthStencilState, 0);
        }

        private void RenderMeshTechnique()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(g_MeshVS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(g_MeshPS, null);
            context.OutputMergerSetBlendState(g_DisableBlending, null, 0xffffffff);
            context.OutputMergerSetDepthStencilState(g_EnableDepthDepthStencilState, 0);
        }

        private void RenderMeshInstTechnique()
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(g_MeshInstVS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(g_MeshPS, null);
            context.OutputMergerSetBlendState(g_DisableBlending, null, 0xffffffff);
            context.OutputMergerSetDepthStencilState(g_EnableDepthDepthStencilState, 0);
        }

        public void ResetBuildings()
        {
            float f2Third = 0.6666f;

            XMFloat3[] vChunkOffsets = new XMFloat3[BreakableWall.NumChunks]
            {
                new XMFloat3( f2Third, -f2Third, 0.0f ),
                new XMFloat3(-f2Third, f2Third, 0.0f),
                new XMFloat3(f2Third, f2Third, 0.0f),
                new XMFloat3(-f2Third, -f2Third, 0.0f),
                new XMFloat3(f2Third, 0, 0.0f),
                new XMFloat3(0, f2Third, 0.0f),
                new XMFloat3(-f2Third, 0, 0.0f),
                new XMFloat3(0, -f2Third, 0.0f),
                new XMFloat3(0, 0, 0.0f)
            };

            for (int i = 0; i < MaxBuildings; i++)
            {
                g_Building[i].SetBaseMesh(g_WallMesh);

                for (uint c = 0; c < BreakableWall.NumChunks; c++)
                {
                    g_Building[i].SetChunkMesh(c, g_ChunkMesh[c], vChunkOffsets[c]);
                }
            }
        }

        public void NewExplosion(in XMFloat3 vCenter, float fSize)
        {
            XMFloat3 vDirMul = new(0.2f, 1.0f, 0.2f);
            float fMinPower = 5.0f;
            float fMaxPower = 30.0f;

            for (int i = 0; i < MaxBuildings; i++)
            {
                g_Building[i].CreateExplosion(vCenter, vDirMul, fSize, fMinPower, fMaxPower);
            }
        }
    }
}
