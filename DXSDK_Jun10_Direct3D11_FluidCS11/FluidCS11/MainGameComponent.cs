using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//--------------------------------------------------------------------------------------
// Smoothed Particle Hydrodynamics Algorithm Based Upon:
// Particle-Based Fluid Simulation for Interactive Applications
// Matthias Müller
//--------------------------------------------------------------------------------------

//--------------------------------------------------------------------------------------
// Optimized Grid Algorithm Based Upon:
// Broad-Phase Collision Detection with CUDA
// Scott Le Grand
//--------------------------------------------------------------------------------------

namespace FluidCS11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private bool simulationBuffersChanged = false;

        // Shaders

        private D3D11VertexShader g_pParticleVS;

        private D3D11GeometryShader g_pParticleGS;

        private D3D11PixelShader g_pParticlePS;

        private D3D11ComputeShader g_pBuildGridCS;

        private D3D11ComputeShader g_pClearGridIndicesCS;

        private D3D11ComputeShader g_pBuildGridIndicesCS;

        private D3D11ComputeShader g_pRearrangeParticlesCS;

        private D3D11ComputeShader g_pDensity_SimpleCS;

        private D3D11ComputeShader g_pForce_SimpleCS;

        private D3D11ComputeShader g_pDensity_SharedCS;

        private D3D11ComputeShader g_pForce_SharedCS;

        private D3D11ComputeShader g_pDensity_GridCS;

        private D3D11ComputeShader g_pForce_GridCS;

        private D3D11ComputeShader g_pIntegrateCS;

        private D3D11ComputeShader g_pSortBitonic;

        private D3D11ComputeShader g_pSortTranspose;

        // Structured Buffers

        private D3D11Buffer g_pParticles;

        private D3D11ShaderResourceView g_pParticlesSRV;

        private D3D11UnorderedAccessView g_pParticlesUAV;

        private D3D11Buffer g_pSortedParticles;

        private D3D11ShaderResourceView g_pSortedParticlesSRV;

        private D3D11UnorderedAccessView g_pSortedParticlesUAV;

        private D3D11Buffer g_pParticleDensity;

        private D3D11ShaderResourceView g_pParticleDensitySRV;

        private D3D11UnorderedAccessView g_pParticleDensityUAV;

        private D3D11Buffer g_pParticleForces;

        private D3D11ShaderResourceView g_pParticleForcesSRV;

        private D3D11UnorderedAccessView g_pParticleForcesUAV;

        private D3D11Buffer g_pGrid;

        private D3D11ShaderResourceView g_pGridSRV;

        private D3D11UnorderedAccessView g_pGridUAV;

        private D3D11Buffer g_pGridPingPong;

        private D3D11ShaderResourceView g_pGridPingPongSRV;

        private D3D11UnorderedAccessView g_pGridPingPongUAV;

        private D3D11Buffer g_pGridIndices;

        private D3D11ShaderResourceView g_pGridIndicesSRV;

        private D3D11UnorderedAccessView g_pGridIndicesUAV;

        // Constant Buffers

        private D3D11Buffer g_pcbSimulationConstants;

        private D3D11Buffer g_pcbRenderConstants;

        private D3D11Buffer g_pSortCB;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public uint NumParticles { get; set; } = Constants.NUM_PARTICLES_16K;

        public XMFloat2 Gravity { get; set; } = Constants.GRAVITY_DOWN;

        public SimulationMode SimMode { get; set; } = SimulationMode.Grid;

        public void InvalidateSimulationBuffers()
        {
            this.simulationBuffersChanged = true;
        }

        private void CreateConstantBuffer<T>(out D3D11Buffer cb) where T : struct
        {
            var desc = new D3D11BufferDesc((uint)Marshal.SizeOf<T>(), D3D11BindOptions.ConstantBuffer, D3D11Usage.Default);
            cb = this.deviceResources.D3DDevice.CreateBuffer(desc);
        }

        private void CreateStructuredBuffer<T>(uint numElements, out D3D11Buffer buffer, out D3D11ShaderResourceView srv, out D3D11UnorderedAccessView uav, T[] initialData = null) where T : struct
        {
            if (initialData is not null && initialData.Length != numElements)
            {
                throw new ArgumentOutOfRangeException(nameof(initialData));
            }

            // Create SB
            D3D11BufferDesc bufferDesc = new(
                numElements * (uint)Marshal.SizeOf<T>(),
                D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.BufferStructured,
                (uint)Marshal.SizeOf<T>());

            if (initialData is null)
            {
                buffer = this.deviceResources.D3DDevice.CreateBuffer(bufferDesc);
            }
            else
            {
                buffer = this.deviceResources.D3DDevice.CreateBuffer(bufferDesc, initialData, 0, 0);
            }

            // Create SRV
            D3D11ShaderResourceViewDesc srvDesc = new(buffer, DxgiFormat.Unknown, 0, numElements);
            srv = this.deviceResources.D3DDevice.CreateShaderResourceView(buffer, srvDesc);

            // Create UAV
            D3D11UnorderedAccessViewDesc uavDesc = new(buffer, DxgiFormat.Unknown, 0, numElements);
            uav = this.deviceResources.D3DDevice.CreateUnorderedAccessView(buffer, uavDesc);
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;

            // Rendering Shaders

            this.g_pParticleVS = device.CreateVertexShader(File.ReadAllBytes("FluidRenderVS.cso"), null);
            this.g_pParticleVS.SetDebugName("ParticleVS");

            this.g_pParticleGS = device.CreateGeometryShader(File.ReadAllBytes("FluidRenderGS.cso"), null);
            this.g_pParticleGS.SetDebugName("ParticleGS");

            this.g_pParticlePS = device.CreatePixelShader(File.ReadAllBytes("FluidRenderPS.cso"), null);
            this.g_pParticlePS.SetDebugName("ParticlePS");

            // Compute Shaders

            this.g_pIntegrateCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_IntegrateCS.cso"), null);
            this.g_pIntegrateCS.SetDebugName("IntegrateCS");

            this.g_pDensity_SimpleCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_DensityCS_Simple.cso"), null);
            this.g_pDensity_SimpleCS.SetDebugName("DensityCS_Simple");

            this.g_pForce_SimpleCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_ForceCS_Simple.cso"), null);
            this.g_pForce_SimpleCS.SetDebugName("ForceCS_Simple");

            this.g_pDensity_SharedCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_DensityCS_Shared.cso"), null);
            this.g_pDensity_SharedCS.SetDebugName("DensityCS_Shared");

            this.g_pForce_SharedCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_ForceCS_Shared.cso"), null);
            this.g_pForce_SharedCS.SetDebugName("ForceCS_Shared");

            this.g_pDensity_GridCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_DensityCS_Grid.cso"), null);
            this.g_pDensity_GridCS.SetDebugName("DensityCS_Grid");

            this.g_pForce_GridCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_ForceCS_Grid.cso"), null);
            this.g_pForce_GridCS.SetDebugName("ForceCS_Grid");

            this.g_pBuildGridCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_BuildGridCS.cso"), null);
            this.g_pBuildGridCS.SetDebugName("BuildGridCS");

            this.g_pClearGridIndicesCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_ClearGridIndicesCS.cso"), null);
            this.g_pClearGridIndicesCS.SetDebugName("ClearGridIndicesCS");

            this.g_pBuildGridIndicesCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_BuildGridIndicesCS.cso"), null);
            this.g_pBuildGridIndicesCS.SetDebugName("BuildGridIndicesCS");

            this.g_pRearrangeParticlesCS = device.CreateComputeShader(File.ReadAllBytes("Fluid_RearrangeParticlesCS.cso"), null);
            this.g_pRearrangeParticlesCS.SetDebugName("RearrangeParticlesCS");

            // Sort Shaders
            this.g_pSortBitonic = device.CreateComputeShader(File.ReadAllBytes("CSSortBitonic.cso"), null);
            this.g_pSortTranspose = device.CreateComputeShader(File.ReadAllBytes("CSSortTranspose.cso"), null);
            this.g_pSortBitonic.SetDebugName("BitonicSort");
            this.g_pSortTranspose.SetDebugName("MatrixTranspose");

            // Create the Simulation Buffers
            this.CreateSimulationBuffers();

            // Create Constant Buffers
            this.CreateConstantBuffer<SimulationConstantBuffer>(out this.g_pcbSimulationConstants);
            this.CreateConstantBuffer<RenderConstantBuffer>(out this.g_pcbRenderConstants);
            this.CreateConstantBuffer<SortConstantBuffer>(out this.g_pSortCB);
            this.g_pcbSimulationConstants.SetDebugName("Simluation");
            this.g_pcbRenderConstants.SetDebugName("Render");
            this.g_pSortCB.SetDebugName("Sort");
        }

        private void CreateSimulationBuffers()
        {
            // Destroy the old buffers in case the number of particles has changed

            D3D11Utils.DisposeAndNull(ref this.g_pParticles);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticles);
            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticlesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticlesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pParticleForces);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleForcesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleForcesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensity);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensitySRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensityUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pGridSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGrid);

            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPongSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPongUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPong);

            D3D11Utils.DisposeAndNull(ref this.g_pGridIndicesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridIndicesUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridIndices);

            // Create the initial particle positions
            // This is only used to populate the GPU buffers on creation

            uint startingWidth = (uint)Math.Sqrt(this.NumParticles);
            var particles = new Particle[this.NumParticles];

            for (uint i = 0; i < this.NumParticles; i++)
            {
                // Arrange the particles in a nice square
                uint x = i % startingWidth;
                uint y = i / startingWidth;
                particles[i].Position = new XMFloat2(Constants.g_fInitialParticleSpacing * x, Constants.g_fInitialParticleSpacing * y);
                particles[i].Velocity = new XMFloat2();
            }

            // Create Structured Buffers

            this.CreateStructuredBuffer<Particle>(this.NumParticles, out this.g_pParticles, out this.g_pParticlesSRV, out this.g_pParticlesUAV, particles);
            this.g_pParticles.SetDebugName("Particles");
            this.g_pParticlesSRV.SetDebugName("Particles SRV");
            this.g_pParticlesUAV.SetDebugName("Particles UAV");

            this.CreateStructuredBuffer<Particle>(this.NumParticles, out this.g_pSortedParticles, out this.g_pSortedParticlesSRV, out this.g_pSortedParticlesUAV, particles);
            this.g_pSortedParticles.SetDebugName("Sorted");
            this.g_pSortedParticlesSRV.SetDebugName("Sorted SRV");
            this.g_pSortedParticlesUAV.SetDebugName("Sorted UAV");

            this.CreateStructuredBuffer<ParticleForces>(this.NumParticles, out this.g_pParticleForces, out this.g_pParticleForcesSRV, out this.g_pParticleForcesUAV);
            this.g_pParticleForces.SetDebugName("Forces");
            this.g_pParticleForcesSRV.SetDebugName("Forces SRV");
            this.g_pParticleForcesUAV.SetDebugName("Forces UAV");

            this.CreateStructuredBuffer<ParticleDensity>(this.NumParticles, out this.g_pParticleDensity, out this.g_pParticleDensitySRV, out this.g_pParticleDensityUAV);
            this.g_pParticleDensity.SetDebugName("Density");
            this.g_pParticleDensitySRV.SetDebugName("Density SRV");
            this.g_pParticleDensityUAV.SetDebugName("Density UAV");

            this.CreateStructuredBuffer<uint>(this.NumParticles, out this.g_pGrid, out this.g_pGridSRV, out this.g_pGridUAV);
            this.g_pGrid.SetDebugName("Grid");
            this.g_pGridSRV.SetDebugName("Grid SRV");
            this.g_pGridUAV.SetDebugName("Grid UAV");

            this.CreateStructuredBuffer<uint>(this.NumParticles, out this.g_pGridPingPong, out this.g_pGridPingPongSRV, out this.g_pGridPingPongUAV);
            this.g_pGridPingPong.SetDebugName("PingPong");
            this.g_pGridPingPongSRV.SetDebugName("PingPong SRV");
            this.g_pGridPingPongUAV.SetDebugName("PingPong UAV");

            this.CreateStructuredBuffer<XMUInt2>(Constants.NUM_GRID_INDICES, out this.g_pGridIndices, out this.g_pGridIndicesSRV, out this.g_pGridIndicesUAV);
            this.g_pGridIndices.SetDebugName("Indices");
            this.g_pGridIndicesSRV.SetDebugName("Indices SRV");
            this.g_pGridIndicesUAV.SetDebugName("Indices UAV");

            this.simulationBuffersChanged = false;
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pcbSimulationConstants);
            D3D11Utils.DisposeAndNull(ref this.g_pcbRenderConstants);
            D3D11Utils.DisposeAndNull(ref this.g_pSortCB);

            D3D11Utils.DisposeAndNull(ref this.g_pParticleVS);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleGS);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlePS);

            D3D11Utils.DisposeAndNull(ref this.g_pIntegrateCS);
            D3D11Utils.DisposeAndNull(ref this.g_pDensity_SimpleCS);
            D3D11Utils.DisposeAndNull(ref this.g_pForce_SimpleCS);
            D3D11Utils.DisposeAndNull(ref this.g_pDensity_SharedCS);
            D3D11Utils.DisposeAndNull(ref this.g_pForce_SharedCS);
            D3D11Utils.DisposeAndNull(ref this.g_pDensity_GridCS);
            D3D11Utils.DisposeAndNull(ref this.g_pForce_GridCS);
            D3D11Utils.DisposeAndNull(ref this.g_pBuildGridCS);
            D3D11Utils.DisposeAndNull(ref this.g_pClearGridIndicesCS);
            D3D11Utils.DisposeAndNull(ref this.g_pBuildGridIndicesCS);
            D3D11Utils.DisposeAndNull(ref this.g_pRearrangeParticlesCS);
            D3D11Utils.DisposeAndNull(ref this.g_pSortBitonic);
            D3D11Utils.DisposeAndNull(ref this.g_pSortTranspose);

            D3D11Utils.DisposeAndNull(ref this.g_pParticles);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticlesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticles);
            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticlesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pSortedParticlesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pParticleForces);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleForcesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleForcesUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensity);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensitySRV);
            D3D11Utils.DisposeAndNull(ref this.g_pParticleDensityUAV);

            D3D11Utils.DisposeAndNull(ref this.g_pGridSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGrid);

            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPongSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPongUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridPingPong);

            D3D11Utils.DisposeAndNull(ref this.g_pGridIndicesSRV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridIndicesUAV);
            D3D11Utils.DisposeAndNull(ref this.g_pGridIndices);
        }

        public void CreateWindowSizeDependentResources()
        {
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

            if (this.simulationBuffersChanged)
            {
                this.CreateSimulationBuffers();
            }

            this.SimulateFluid(timer.ElapsedSeconds);
        }

        /// <summary>
        /// GPU Bitonic Sort
        /// </summary>
        /// <remarks>
        /// For more information, please see the ComputeShaderSort11 sample
        /// </remarks>
        /// <param name="inUAV"></param>
        /// <param name="inSRV"></param>
        /// <param name="tempUAV"></param>
        /// <param name="tempSRV"></param>
        private void GPUSort(
            D3D11UnorderedAccessView inUAV,
            D3D11ShaderResourceView inSRV,
            D3D11UnorderedAccessView tempUAV,
            D3D11ShaderResourceView tempSRV)
        {
            var context = this.deviceResources.D3DContext;

            context.ComputeShaderSetConstantBuffers(0, new[] { this.g_pSortCB });

            uint NUM_ELEMENTS = this.NumParticles;
            uint MATRIX_WIDTH = Constants.BITONIC_BLOCK_SIZE;
            uint MATRIX_HEIGHT = NUM_ELEMENTS / Constants.BITONIC_BLOCK_SIZE;

            // Sort the data
            // First sort the rows for the levels <= to the block size
            for (uint level = 2; level <= Constants.BITONIC_BLOCK_SIZE; level <<= 1)
            {
                SortConstantBuffer constants = new()
                {
                    Level = level,
                    LevelMask = level,
                    Width = MATRIX_HEIGHT,
                    Height = MATRIX_WIDTH
                };

                context.UpdateSubresource(this.g_pSortCB, 0, null, constants, 0, 0);

                // Sort the row data
                context.ComputeShaderSetUnorderedAccessViews(0, new[] { inUAV }, new[] { 0U });
                context.ComputeShaderSetShader(this.g_pSortBitonic, null);
                context.Dispatch(NUM_ELEMENTS / Constants.BITONIC_BLOCK_SIZE, 1, 1);
            }

            // Then sort the rows and columns for the levels > than the block size
            // Transpose. Sort the Columns. Transpose. Sort the Rows.
            for (uint level = (Constants.BITONIC_BLOCK_SIZE << 1); level <= NUM_ELEMENTS; level <<= 1)
            {
                SortConstantBuffer constants1 = new()
                {
                    Level = level / Constants.BITONIC_BLOCK_SIZE,
                    LevelMask = (level & ~NUM_ELEMENTS) / Constants.BITONIC_BLOCK_SIZE,
                    Width = MATRIX_WIDTH,
                    Height = MATRIX_HEIGHT
                };

                context.UpdateSubresource(this.g_pSortCB, 0, null, constants1, 0, 0);

                // Transpose the data from buffer 1 into buffer 2
                context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
                context.ComputeShaderSetUnorderedAccessViews(0, new[] { tempUAV }, new[] { 0U });
                context.ComputeShaderSetShaderResources(0, new[] { inSRV });
                context.ComputeShaderSetShader(this.g_pSortTranspose, null);
                context.Dispatch(MATRIX_WIDTH / Constants.TRANSPOSE_BLOCK_SIZE, MATRIX_HEIGHT / Constants.TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the transposed column data
                context.ComputeShaderSetShader(this.g_pSortBitonic, null);
                context.Dispatch(NUM_ELEMENTS / Constants.BITONIC_BLOCK_SIZE, 1, 1);

                SortConstantBuffer constants2 = new()
                {
                    Level = Constants.BITONIC_BLOCK_SIZE,
                    LevelMask = level,
                    Width = MATRIX_HEIGHT,
                    Height = MATRIX_WIDTH
                };

                context.UpdateSubresource(this.g_pSortCB, 0, null, constants2, 0, 0);

                // Transpose the data from buffer 2 back into buffer 1
                context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
                context.ComputeShaderSetUnorderedAccessViews(0, new[] { inUAV }, new[] { 0U });
                context.ComputeShaderSetShaderResources(0, new[] { tempSRV });
                context.ComputeShaderSetShader(this.g_pSortTranspose, null);
                context.Dispatch(MATRIX_HEIGHT / Constants.TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / Constants.TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the row data
                context.ComputeShaderSetShader(this.g_pSortBitonic, null);
                context.Dispatch(NUM_ELEMENTS / Constants.BITONIC_BLOCK_SIZE, 1, 1);
            }
        }

        private void SimulateFluid(double elapsedTime)
        {
            var context = this.deviceResources.D3DContext;

            // Update per-frame variables
            SimulationConstantBuffer pData = new();

            // Simulation Constants
            pData.NumParticles = this.NumParticles;
            // Clamp the time step when the simulation runs slowly to prevent numerical explosion
            pData.TimeStep = Math.Min(Constants.g_fMaxAllowableTimeStep, (float)elapsedTime);
            pData.Smoothlen = Constants.g_fSmoothlen;
            pData.PressureStiffness = Constants.g_fPressureStiffness;
            pData.RestDensity = Constants.g_fRestDensity;
            pData.DensityCoef = Constants.g_fParticleMass * 315.0f / (64.0f * XMMath.PI * (float)Math.Pow(Constants.g_fSmoothlen, 9));
            pData.GradPressureCoef = Constants.g_fParticleMass * -45.0f / (XMMath.PI * (float)Math.Pow(Constants.g_fSmoothlen, 6));
            pData.LapViscosityCoef = Constants.g_fParticleMass * Constants.g_fViscosity * 45.0f / (XMMath.PI * (float)Math.Pow(Constants.g_fSmoothlen, 6));

            pData.Gravity = this.Gravity;

            // Cells are spaced the size of the smoothing length search radius
            // That way we only need to search the 8 adjacent cells + current cell
            pData.GridDim = new XMFloat4(
                1.0f / Constants.g_fSmoothlen,
                1.0f / Constants.g_fSmoothlen,
                0,
                0);

            // Collision information for the map
            pData.WallStiffness = Constants.g_fWallStiffness;
            pData.Planes = Constants.g_vPlanes;

            context.UpdateSubresource(this.g_pcbSimulationConstants, 0, null, pData, 0, 0);

            switch (this.SimMode)
            {
                // Simple N^2 Algorithm
                case SimulationMode.Simple:
                    this.SimulateFluid_Simple();
                    break;

                // Optimized N^2 Algorithm using Shared Memory
                case SimulationMode.Shared:
                    this.SimulateFluid_Shared();
                    break;

                // Optimized Grid + Sort Algorithm
                case SimulationMode.Grid:
                    this.SimulateFluid_Grid();
                    break;
            }

            // Unset
            context.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0U });
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetShaderResources(1, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetShaderResources(2, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetShaderResources(3, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetShaderResources(4, new D3D11ShaderResourceView[] { null });
        }

        /// <summary>
        /// GPU Fluid Simulation - Simple N^2 Algorithm
        /// </summary>
        private void SimulateFluid_Simple()
        {
            var context = this.deviceResources.D3DContext;

            // Setup
            context.ComputeShaderSetConstantBuffers(0, new[] { this.g_pcbSimulationConstants });
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pParticlesSRV });

            // Density
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleDensityUAV }, new[] { 0U });
            context.ComputeShaderSetShader(this.g_pDensity_SimpleCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Force
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleForcesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(1, new[] { this.g_pParticleDensitySRV });
            context.ComputeShaderSetShader(this.g_pForce_SimpleCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Integrate
            context.CopyResource(this.g_pSortedParticles, this.g_pParticles);
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pSortedParticlesSRV });
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticlesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(2, new[] { this.g_pParticleForcesSRV });
            context.ComputeShaderSetShader(this.g_pIntegrateCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);
        }

        /// <summary>
        /// GPU Fluid Simulation - Optimized N^2 Algorithm using Shared Memory
        /// </summary>
        private void SimulateFluid_Shared()
        {
            var context = this.deviceResources.D3DContext;

            // Setup
            context.ComputeShaderSetConstantBuffers(0, new[] { this.g_pcbSimulationConstants });
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pParticlesSRV });

            // Density
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleDensityUAV }, new[] { 0U });
            context.ComputeShaderSetShader(this.g_pDensity_SharedCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Force
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleForcesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(1, new[] { this.g_pParticleDensitySRV });
            context.ComputeShaderSetShader(this.g_pForce_SharedCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Integrate
            context.CopyResource(this.g_pSortedParticles, this.g_pParticles);
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pSortedParticlesSRV });
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticlesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(2, new[] { this.g_pParticleForcesSRV });
            context.ComputeShaderSetShader(this.g_pIntegrateCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);
        }

        /// <summary>
        /// GPU Fluid Simulation - Optimized Algorithm using a Grid + Sort
        /// </summary>
        /// <remarks>
        /// Algorithm Overview:
        /// Build Grid: For every particle, calculate a hash based on the grid cell it is in
        /// Sort Grid: Sort all of the particles based on the grid ID hash
        /// Particles in the same cell will now be adjacent in memory
        /// Build Grid Indices: Located the start and end offsets for each cell
        /// Rearrange: Rearrange the particles into the same order as the grid for easy lookup
        /// Density, Force, Integrate: Perform the normal fluid simulation algorithm
        /// Except now, only calculate particles from the 8 adjacent cells + current cell
        /// </remarks>
        private void SimulateFluid_Grid()
        {
            var context = this.deviceResources.D3DContext;

            // Setup
            context.ComputeShaderSetConstantBuffers(0, new[] { this.g_pcbSimulationConstants });
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pGridUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pParticlesSRV });

            // Build Grid
            context.ComputeShaderSetShader(this.g_pBuildGridCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Sort Grid
            this.GPUSort(this.g_pGridUAV, this.g_pGridSRV, this.g_pGridPingPongUAV, this.g_pGridPingPongSRV);

            // Setup
            context.ComputeShaderSetConstantBuffers(0, new[] { this.g_pcbSimulationConstants });
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pGridIndicesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(3, new[] { this.g_pGridSRV });

            // Build Grid Indices
            context.ComputeShaderSetShader(this.g_pClearGridIndicesCS, null);
            context.Dispatch(Constants.NUM_GRID_INDICES / Constants.SIMULATION_BLOCK_SIZE, 1, 1);
            context.ComputeShaderSetShader(this.g_pBuildGridIndicesCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Setup
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pSortedParticlesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pParticlesSRV });
            context.ComputeShaderSetShaderResources(3, new[] { this.g_pGridSRV });

            // Rearrange
            context.ComputeShaderSetShader(this.g_pRearrangeParticlesCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Setup
            context.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0U });
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetShaderResources(0, new[] { this.g_pSortedParticlesSRV });
            context.ComputeShaderSetShaderResources(3, new[] { this.g_pGridSRV });
            context.ComputeShaderSetShaderResources(4, new[] { this.g_pGridIndicesSRV });

            // Density
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleDensityUAV }, new[] { 0U });
            context.ComputeShaderSetShader(this.g_pDensity_GridCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Force
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticleForcesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(1, new[] { this.g_pParticleDensitySRV });
            context.ComputeShaderSetShader(this.g_pForce_GridCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);

            // Integrate
            context.ComputeShaderSetUnorderedAccessViews(0, new[] { this.g_pParticlesUAV }, new[] { 0U });
            context.ComputeShaderSetShaderResources(2, new[] { this.g_pParticleForcesSRV });
            context.ComputeShaderSetShader(this.g_pIntegrateCS, null);
            context.Dispatch(this.NumParticles / Constants.SIMULATION_BLOCK_SIZE, 1, 1);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the render target and depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.05f, 0.05f, 0.05f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            this.RenderFluid();
        }

        private void RenderFluid()
        {
            var context = this.deviceResources.D3DContext;

            // Simple orthographic projection to display the entire map
            XMMatrix mView = XMMatrix.Translation(-Constants.g_fMapWidth / 2.0f, -Constants.g_fMapHeight / 2.0f, 0);
            XMMatrix mProjection = XMMatrix.OrthographicLH(Constants.g_fMapWidth, Constants.g_fMapHeight, 0, 1);
            XMMatrix mViewProjection = mView * mProjection;

            // Update Constants
            RenderConstantBuffer pData = new();

            pData.ViewProjection = mViewProjection.Transpose();
            pData.ParticleSize = Constants.g_fParticleRenderSize;

            context.UpdateSubresource(this.g_pcbRenderConstants, 0, null, pData, 0, 0);

            // Set the shaders
            context.VertexShaderSetShader(this.g_pParticleVS, null);
            context.GeometryShaderSetShader(this.g_pParticleGS, null);
            context.PixelShaderSetShader(this.g_pParticlePS, null);

            // Set the constant buffers
            context.VertexShaderSetConstantBuffers(0, new[] { this.g_pcbRenderConstants });
            context.GeometryShaderSetConstantBuffers(0, new[] { this.g_pcbRenderConstants });
            context.PixelShaderSetConstantBuffers(0, new[] { this.g_pcbRenderConstants });

            // Setup the particles buffer and IA
            context.VertexShaderSetShaderResources(0, new[] { this.g_pParticlesSRV });
            context.VertexShaderSetShaderResources(1, new[] { this.g_pParticleDensitySRV });
            context.InputAssemblerSetVertexBuffers(0, new D3D11Buffer[] { null }, new[] { 0U }, new[] { 0U });
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.PointList);

            // Draw the mesh
            context.Draw(this.NumParticles, 0);

            // Unset the particles buffer
            context.VertexShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.VertexShaderSetShaderResources(1, new D3D11ShaderResourceView[] { null });
        }
    }
}
