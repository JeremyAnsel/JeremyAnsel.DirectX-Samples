using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dds;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NBodyGravityCS11
{
    class MainGameComponent : IGameComponent
    {
        public const float Spread = 400.0f;

        // the number of particles in the n-body simulation
        public const int MaxParticles = 10000;

        private DeviceResources deviceResources;

        private D3D11VertexShader g_pRenderParticlesVS;

        private D3D11GeometryShader g_pRenderParticlesGS;

        private D3D11PixelShader g_pRenderParticlesPS;

        private D3D11ComputeShader g_pCalcCS;

        private D3D11InputLayout g_pParticleVertexLayout;

        private D3D11Buffer g_pParticleBuffer;

        private D3D11Buffer g_pParticlePosVelo0;

        private D3D11Buffer g_pParticlePosVelo1;

        private D3D11ShaderResourceView g_pParticlePosVeloRV0;

        private D3D11ShaderResourceView g_pParticlePosVeloRV1;

        private D3D11UnorderedAccessView g_pParticlePosVeloUAV0;

        private D3D11UnorderedAccessView g_pParticlePosVeloUAV1;

        private D3D11Buffer g_pcbGS;

        private D3D11Buffer g_pcbCS;

        private D3D11ShaderResourceView g_pParticleTexRV;

        private D3D11SamplerState g_pSampleStateLinear;

        private D3D11BlendState g_pBlendingStateParticle;

        private D3D11DepthStencilState g_pDepthStencilState;

        public MainGameComponent()
        {
        }

        public int DiskGalaxyFormationType { get; set; } = 0;

        public XMMatrix ViewMatrix { get; set; }

        public XMMatrix ProjectionMatrix { get; set; }

        public Random Random { get; set; }

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
            byte[] renderParticlesVSBytecode = File.ReadAllBytes("ParticleDrawVS.cso");
            this.g_pRenderParticlesVS = d3dDevice.CreateVertexShader(renderParticlesVSBytecode, null);
            this.g_pRenderParticlesGS = d3dDevice.CreateGeometryShader(File.ReadAllBytes("ParticleDrawGS.cso"), null);
            this.g_pRenderParticlesPS = d3dDevice.CreatePixelShader(File.ReadAllBytes("ParticleDrawPS.cso"), null);
            this.g_pCalcCS = d3dDevice.CreateComputeShader(File.ReadAllBytes("NBodyGravityCS.cso"), null);

            // Create our vertex input layout
            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc
                {
                    SemanticName = "COLOR",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32A32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.g_pParticleVertexLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, renderParticlesVSBytecode);

            this.CreateParticleBuffer();
            this.CreateParticlePosVeloBuffers();

            // Setup constant buffer
            this.g_pcbGS = d3dDevice.CreateBuffer(new D3D11BufferDesc(ConstantBufferGS.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbCS = d3dDevice.CreateBuffer(new D3D11BufferDesc(ConstantBufferCS.Size, D3D11BindOptions.ConstantBuffer));

            // Load the Particle Texture
            DdsDirectX.CreateTexture(
                "Particle.dds",
                this.deviceResources.D3DDevice,
                this.deviceResources.D3DContext,
                out this.g_pParticleTexRV);

            D3D11SamplerDesc SamplerDesc = D3D11SamplerDesc.Default;
            SamplerDesc.AddressU = D3D11TextureAddressMode.Clamp;
            SamplerDesc.AddressV = D3D11TextureAddressMode.Clamp;
            SamplerDesc.AddressW = D3D11TextureAddressMode.Clamp;
            SamplerDesc.Filter = D3D11Filter.MinMagMipLinear;
            this.g_pSampleStateLinear = d3dDevice.CreateSamplerState(SamplerDesc);

            D3D11BlendDesc BlendStateDesc = D3D11BlendDesc.Default;
            D3D11RenderTargetBlendDesc[] BlendStateDescRenderTargets = BlendStateDesc.GetRenderTargets();
            BlendStateDescRenderTargets[0].IsBlendEnabled = true;
            BlendStateDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            BlendStateDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            BlendStateDescRenderTargets[0].DestinationBlend = D3D11BlendValue.One;
            BlendStateDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            BlendStateDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            BlendStateDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            BlendStateDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            BlendStateDesc.SetRenderTargets(BlendStateDescRenderTargets);
            this.g_pBlendingStateParticle = d3dDevice.CreateBlendState(BlendStateDesc);

            D3D11DepthStencilDesc DepthStencilDesc = D3D11DepthStencilDesc.Default;
            DepthStencilDesc.IsDepthEnabled = false;
            DepthStencilDesc.DepthWriteMask = D3D11DepthWriteMask.Zero;
            this.g_pDepthStencilState = d3dDevice.CreateDepthStencilState(DepthStencilDesc);

            XMFloat3 eye = new XMFloat3(-Spread * 2, Spread * 4, -Spread * 3);
            XMFloat3 at = new XMFloat3(0.0f, 0.0f, 0.0f);
            XMFloat3 up = new XMFloat3(0.0f, 1.0f, 0.0f);
            this.ViewMatrix = XMMatrix.LookAtLH(eye, at, up);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pRenderParticlesVS);
            D3D11Utils.DisposeAndNull(ref this.g_pRenderParticlesGS);
            D3D11Utils.DisposeAndNull(ref this.g_pRenderParticlesPS);
            D3D11Utils.DisposeAndNull(ref this.g_pCalcCS);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleVertexLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleBuffer);
            this.ReleaseParticlePosVeloBuffers();
            D3D11Utils.DisposeAndNull(ref this.g_pcbGS);
            D3D11Utils.DisposeAndNull(ref this.g_pcbCS);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleTexRV);
            D3D11Utils.DisposeAndNull(ref this.g_pSampleStateLinear);
            D3D11Utils.DisposeAndNull(ref this.g_pBlendingStateParticle);
            D3D11Utils.DisposeAndNull(ref this.g_pDepthStencilState);
        }

        private void CreateParticleBuffer()
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            ParticleVertex[] pVertices = new ParticleVertex[MaxParticles];

            for (int i = 0; i < pVertices.Length; i++)
            {
                pVertices[i].Color = new XMFloat4(1, 1, 0.2f, 1);
            }

            D3D11BufferDesc vbdesc = D3D11BufferDesc.From(pVertices, D3D11BindOptions.VertexBuffer);
            this.g_pParticleBuffer = d3dDevice.CreateBuffer(vbdesc, pVertices, 0, 0);
        }

        private void CreateParticlePosVeloBuffers()
        {
            var d3dDevice = this.deviceResources.D3DDevice;

            // Initialize the data in the buffers
            Particle[] pData1 = new Particle[MaxParticles];

            Random rand = this.Random ?? new Random(0);

            if (this.DiskGalaxyFormationType == 0)
            {
                // Disk Galaxy Formation
                float fCenterSpread = Spread * 0.50f;

                LoadParticles(
                    rand,
                    pData1,
                    0,
                    new XMFloat3(fCenterSpread, 0, 0),
                    new XMFloat4(0, 0, -20, 1 / 10000.0f / 10000.0f),
                    Spread,
                    pData1.Length / 2);

                LoadParticles(
                    rand,
                    pData1,
                    pData1.Length / 2,
                    new XMFloat3(-fCenterSpread, 0, 0),
                    new XMFloat4(0, 0, 20, 1 / 10000.0f / 10000.0f),
                    Spread,
                    pData1.Length - pData1.Length / 2);
            }
            else
            {
                // Disk Galaxy Formation with impacting third cluster
                LoadParticles(
                    rand,
                    pData1,
                    0,
                    new XMFloat3(Spread, 0, 0),
                    new XMFloat4(0, 0, -8, 1 / 10000.0f / 10000.0f),
                    Spread,
                    pData1.Length / 3);

                LoadParticles(
                    rand,
                    pData1,
                    pData1.Length / 3,
                    new XMFloat3(-Spread, 0, 0),
                    new XMFloat4(0, 0, 8, 1 / 10000.0f / 10000.0f),
                    Spread,
                    pData1.Length / 3);

                LoadParticles(
                    rand,
                    pData1,
                    2 * pData1.Length / 3,
                    new XMFloat3(0, 0, Spread * 15.0f),
                    new XMFloat4(0, 0, -60, 1 / 10000.0f / 10000.0f),
                    Spread,
                    pData1.Length - 2 * pData1.Length / 3);
            }

            D3D11BufferDesc desc = D3D11BufferDesc.From(pData1, D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource);
            desc.MiscOptions = D3D11ResourceMiscOptions.BufferStructured;
            desc.StructureByteStride = Particle.Size;

            this.g_pParticlePosVelo0 = d3dDevice.CreateBuffer(desc, pData1, 0, 0);
            this.g_pParticlePosVelo1 = d3dDevice.CreateBuffer(desc, pData1, 0, 0);

            D3D11ShaderResourceViewDesc DescRV = new D3D11ShaderResourceViewDesc(
                this.g_pParticlePosVelo0,
                DxgiFormat.Unknown,
                0,
                desc.ByteWidth / desc.StructureByteStride);

            this.g_pParticlePosVeloRV0 = d3dDevice.CreateShaderResourceView(this.g_pParticlePosVelo0, DescRV);
            this.g_pParticlePosVeloRV1 = d3dDevice.CreateShaderResourceView(this.g_pParticlePosVelo1, DescRV);

            D3D11UnorderedAccessViewDesc DescUAV = new D3D11UnorderedAccessViewDesc(
                this.g_pParticlePosVelo0,
                DxgiFormat.Unknown,
                0,
                desc.ByteWidth / desc.StructureByteStride);

            this.g_pParticlePosVeloUAV0 = d3dDevice.CreateUnorderedAccessView(this.g_pParticlePosVelo0, DescUAV);
            this.g_pParticlePosVeloUAV1 = d3dDevice.CreateUnorderedAccessView(this.g_pParticlePosVelo1, DescUAV);
        }

        private void ReleaseParticlePosVeloBuffers()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVelo0);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVelo1);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVeloRV0);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVeloRV1);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVeloUAV0);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePosVeloUAV1);
        }

        private static void LoadParticles(Random rand, Particle[] pParticles, int startIndex, XMFloat3 center, XMFloat4 velocity, float spread, int numParticles)
        {
            for (int i = 0; i < numParticles; i++)
            {
                XMFloat3 delta = new XMFloat3(spread, spread, spread);

                while (XMVector3.LengthSquare(delta).X > spread * spread)
                {
                    delta.X = RPercent(rand) * spread;
                    delta.Y = RPercent(rand) * spread;
                    delta.Z = RPercent(rand) * spread;
                }

                pParticles[startIndex + i].pos.X = center.X + delta.X;
                pParticles[startIndex + i].pos.Y = center.Y + delta.Y;
                pParticles[startIndex + i].pos.Z = center.Z + delta.Z;
                pParticles[startIndex + i].pos.W = 10000.0f * 10000.0f;

                pParticles[startIndex + i].velo = velocity;
            }
        }

        private static float RPercent(Random rand)
        {
            float ret = rand.Next(10000) - 5000;
            return ret / 5000.0f;
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 10.0f, 500000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            var context = this.deviceResources.D3DContext;

            uint dimx = (uint)Math.Ceiling(MaxParticles / 128.0f);

            context.ComputeShaderSetShader(this.g_pCalcCS, null);

            // For CS input
            D3D11ShaderResourceView[] aRViews = { this.g_pParticlePosVeloRV0 };
            context.ComputeShaderSetShaderResources(0, aRViews);

            // For CS output
            D3D11UnorderedAccessView[] aUAViews = { this.g_pParticlePosVeloUAV1 };
            context.ComputeShaderSetUnorderedAccessViews(0, aUAViews, new uint[1]);

            // For CS constant buffer
            ConstantBufferCS pcbCS = new ConstantBufferCS();
            pcbCS.param.X = MaxParticles;
            pcbCS.param.Y = dimx;
            pcbCS.paramf.X = 0.1f;
            pcbCS.paramf.Y = 1.0f;
            context.UpdateSubresource(this.g_pcbCS, 0, null, pcbCS, 0, 0);
            D3D11Buffer[] ppCB = { g_pcbCS };
            context.ComputeShaderSetConstantBuffers(0, ppCB);

            // Run the CS
            context.Dispatch(dimx, 1, 1);

            // Unbind resources for CS
            D3D11UnorderedAccessView[] ppUAViewNULL = { null };
            context.ComputeShaderSetUnorderedAccessViews(0, ppUAViewNULL, new uint[1]);
            D3D11ShaderResourceView[] ppSRVNULL = { null };
            context.ComputeShaderSetShaderResources(0, ppSRVNULL);

            //pd3dImmediateContext->CSSetShader( NULL, NULL, 0 );

            Swap(ref this.g_pParticlePosVelo0, ref this.g_pParticlePosVelo1);
            Swap(ref this.g_pParticlePosVeloRV0, ref this.g_pParticlePosVeloRV1);
            Swap(ref this.g_pParticlePosVeloUAV0, ref this.g_pParticlePosVeloUAV1);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, null);

            float[] ClearColor = { 0.0f, 0.0f, 0.0f, 1.0f };
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, ClearColor);
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            XMMatrix mView = this.ViewMatrix;
            XMMatrix mProj = this.ProjectionMatrix;

            // Render the particles
            this.RenderParticles(mView, mProj);
        }

        private void RenderParticles(XMMatrix mView, XMMatrix mProj)
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerGetBlendState(out D3D11BlendState pBlendState0, out float[] BlendFactor0, out uint SampleMask0);
            context.OutputMergerGetDepthStencilState(out D3D11DepthStencilState pDepthStencilState0, out uint StencilRef0);

            context.VertexShaderSetShader(this.g_pRenderParticlesVS, null);
            context.GeometryShaderSetShader(this.g_pRenderParticlesGS, null);
            context.PixelShaderSetShader(this.g_pRenderParticlesPS, null);

            context.InputAssemblerSetInputLayout(this.g_pParticleVertexLayout);

            // Set IA parameters
            D3D11Buffer[] pBuffers = { this.g_pParticleBuffer };
            uint[] stride = { ParticleVertex.Size };
            uint[] offset = { 0 };
            context.InputAssemblerSetVertexBuffers(0, pBuffers, stride, offset);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.PointList);

            D3D11ShaderResourceView[] aRViews = { this.g_pParticlePosVeloRV0 };
            context.VertexShaderSetShaderResources(0, aRViews);

            ConstantBufferGS pCBGS = new ConstantBufferGS
            {
                m_WorldViewProj = mView * mProj,
                m_InvView = mView.Inverse()
            };

            context.UpdateSubresource(this.g_pcbGS, 0, null, pCBGS, 0, 0);
            context.GeometryShaderSetConstantBuffers(0, new[] { this.g_pcbGS });

            context.PixelShaderSetShaderResources(0, new[] { this.g_pParticleTexRV });
            context.PixelShaderSetSamplers(0, new[] { this.g_pSampleStateLinear });

            context.OutputMergerSetBlendState(this.g_pBlendingStateParticle, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(this.g_pDepthStencilState, 0);

            context.Draw(MaxParticles, 0);

            D3D11ShaderResourceView[] ppSRVNULL = { null };
            context.VertexShaderSetShaderResources(0, ppSRVNULL);
            context.PixelShaderSetShaderResources(0, ppSRVNULL);

            /*ID3D11Buffer* ppBufNULL[1] = { NULL };
            pd3dImmediateContext->GSSetConstantBuffers( 0, 1, ppBufNULL );*/

            context.GeometryShaderSetShader(null, null);
            context.OutputMergerSetBlendState(pBlendState0, BlendFactor0, SampleMask0);
            context.OutputMergerSetDepthStencilState(pDepthStencilState0, StencilRef0);

            if (pBlendState0)
            {
                D3D11Utils.DisposeAndNull(ref pBlendState0);
            }

            if (pDepthStencilState0)
            {
                D3D11Utils.DisposeAndNull(ref pDepthStencilState0);
            }
        }

        private static void Swap<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }
    }
}
