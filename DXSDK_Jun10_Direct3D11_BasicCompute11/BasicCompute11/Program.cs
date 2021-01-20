//--------------------------------------------------------------------------------------
// File: BasicCompute11.cpp
//
// Demonstrates the basics to get DirectX 11 Compute Shader (aka DirectCompute) up and
// running by implementing Array A + Array B
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

// Comment out the following line to use raw buffers instead of structured buffers
#define USE_STRUCTURED_BUFFERS

// If defined, then the hardware/driver must report support for double-precision CS 5.0 shaders or the sample fails to run
//#define TEST_DOUBLE

using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BasicCompute11
{
    class Program
    {
        // The number of elements in a buffer to be tested
        private const uint NUM_ELEMENTS = 1024;

        private static DeviceResources deviceResources;

        private static D3D11ComputeShader g_pCS;
        private static D3D11Buffer g_pBuf0;
        private static D3D11Buffer g_pBuf1;
        private static D3D11Buffer g_pBufResult;
        private static D3D11ShaderResourceView g_pBuf0SRV;
        private static D3D11ShaderResourceView g_pBuf1SRV;
        private static D3D11UnorderedAccessView g_pBufResultUAV;

        private static readonly BufType[] g_vBuf0 = new BufType[NUM_ELEMENTS];
        private static readonly BufType[] g_vBuf1 = new BufType[NUM_ELEMENTS];

        static void Main(string[] args)
        {
            try
            {
                Console.Write("Creating device...");
                CreateComputeDevice();
                Console.WriteLine("done");

                Console.Write("Creating Compute Shader...");
                CreateComputeShader();
                Console.WriteLine("done");

                Console.Write("Creating buffers and filling them with initial data...");

                for (int i = 0; i < NUM_ELEMENTS; ++i)
                {
                    g_vBuf0[i].i = i;
                    g_vBuf0[i].f = (float)i;
#if TEST_DOUBLE
                    g_vBuf0[i].d = (double)i;
#endif

                    g_vBuf1[i].i = i;
                    g_vBuf1[i].f = (float)i;
#if TEST_DOUBLE
                    g_vBuf1[i].d = (double)i;
#endif
                }

#if USE_STRUCTURED_BUFFERS
                g_pBuf0 = CreateStructuredBuffer(deviceResources.D3DDevice, g_vBuf0);
                g_pBuf1 = CreateStructuredBuffer(deviceResources.D3DDevice, g_vBuf1);
                g_pBufResult = CreateStructuredBuffer(deviceResources.D3DDevice, new BufType[NUM_ELEMENTS]);
#else
                g_pBuf0 = CreateRawBuffer(deviceResources.D3DDevice, g_vBuf0);
                g_pBuf1 = CreateRawBuffer(deviceResources.D3DDevice, g_vBuf1 );
                g_pBufResult = CreateRawBuffer(deviceResources.D3DDevice, new BufType[NUM_ELEMENTS] );
#endif

#if DEBUG
                g_pBuf0?.SetDebugName("Buffer0");
                g_pBuf1?.SetDebugName("Buffer1");
                g_pBufResult?.SetDebugName("Result");
#endif

                Console.WriteLine("done");

                Console.Write("Creating buffer views...");
                g_pBuf0SRV = CreateBufferSRV(deviceResources.D3DDevice, g_pBuf0);
                g_pBuf1SRV = CreateBufferSRV(deviceResources.D3DDevice, g_pBuf1);
                g_pBufResultUAV = CreateBufferUAV(deviceResources.D3DDevice, g_pBufResult);

#if DEBUG
                g_pBuf0SRV?.SetDebugName("Buffer0 SRV");
                g_pBuf1SRV?.SetDebugName("Buffer1 SRV");
                g_pBufResultUAV?.SetDebugName("Result UAV");
#endif

                Console.WriteLine("done");

                Console.Write("Running Compute Shader...");
                RunComputeShader(deviceResources.D3DContext, g_pCS, new[] { g_pBuf0SRV, g_pBuf1SRV }, g_pBufResultUAV, NUM_ELEMENTS, 1, 1);
                Console.WriteLine("done");

                // Read back the result from GPU, verify its correctness against result computed by CPU
                D3D11Buffer debugbuf = CreateAndCopyToDebugBuf(deviceResources.D3DDevice, deviceResources.D3DContext, g_pBufResult);

                try
                {
                    D3D11MappedSubResource mappedResource = deviceResources.D3DContext.Map(debugbuf, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);

                    try
                    {
                        // Set a break point here and put down the expression "p, 1024" in your watch window to see what has been written out by our CS
                        // This is also a common trick to debug CS programs.

                        // Verify that if Compute Shader has done right
                        Console.Write("Verifying against CPU result...");
                        bool bSuccess = true;

                        for (int i = 0; i < NUM_ELEMENTS; ++i)
                        {
                            BufType p = Marshal.PtrToStructure<BufType>(mappedResource.Data + i * (int)BufType.Size);

                            if ((p.i != g_vBuf0[i].i + g_vBuf1[i].i)
                                || (p.f != g_vBuf0[i].f + g_vBuf1[i].f)
#if TEST_DOUBLE
                                || (p.d != g_vBuf0[i].d + g_vBuf1[i].d)
#endif
                            )
                            {
                                Console.WriteLine("failure");
                                bSuccess = false;
                                break;
                            }
                        }

                        if (bSuccess)
                        {
                            Console.WriteLine("succeeded");
                        }
                    }
                    finally
                    {
                        deviceResources.D3DContext.Unmap(debugbuf, 0);
                    }

                }
                finally
                {
                    D3D11Utils.ReleaseAndNull(ref debugbuf);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.Write("Cleaning up...");
                CleanupResources();
                Console.WriteLine("done");
            }

            Console.ReadKey(false);
        }

        static void CreateComputeDevice()
        {
            var options = new DeviceResourcesOptions();
#if DEBUG
            options.Debug = true;
#endif

            deviceResources = new RenderTargetDeviceResources(1, 1, D3D11FeatureLevel.FeatureLevel110, options);

#if TEST_DOUBLE
            // Double-precision support is an optional feature of CS 5.0
            D3D11FeatureDataDoubles supportDoubles = deviceResources.D3DDevice.CheckFeatureSupportDoubles();
            if (!supportDoubles.IsDoublePrecisionFloatShaderOperationsSupported)
            {
                Console.WriteLine("No hardware double-precision capable device found, trying to create ref device.");

                deviceResources.Release();
                deviceResources = null;

                options.ForceWarp = true;
                deviceResources = new RenderTargetDeviceResources(1, 1, D3D11FeatureLevel.FeatureLevel110, options);
            }
#endif
        }

        static void CreateComputeShader()
        {
            var d3dDevice = deviceResources.D3DDevice;

            string path;

#if USE_STRUCTURED_BUFFERS
#if TEST_DOUBLE
            path = "BasicCompute11_StructuredBuffer_Double.cso";
#else
            path = "BasicCompute11_StructuredBuffer_NoDouble.cso";
#endif
#else
#if TEST_DOUBLE
            path = "BasicCompute11_NoStructuredBuffer_Double.cso";
#else
            path = "BasicCompute11_NoStructuredBuffer_NoDouble.cso";
#endif
#endif

            g_pCS = d3dDevice.CreateComputeShader(File.ReadAllBytes(path), null);
        }

        static D3D11Buffer CreateStructuredBuffer<T>(D3D11Device pDevice, T[] pInitData)
            where T : struct
        {
            var desc = D3D11BufferDesc.From(pInitData, D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource);
            desc.MiscOptions = D3D11ResourceMiscOptions.BufferStructured;
            desc.StructureByteStride = (uint)Marshal.SizeOf(typeof(T));

            return pDevice.CreateBuffer(desc, pInitData, 0, 0);
        }

        static D3D11Buffer CreateRawBuffer<T>(D3D11Device pDevice, T[] pInitData)
            where T : struct
        {
            var desc = D3D11BufferDesc.From(pInitData, D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource | D3D11BindOptions.IndexBuffer | D3D11BindOptions.VertexBuffer);
            desc.MiscOptions = D3D11ResourceMiscOptions.BufferAllowRawViews;

            return pDevice.CreateBuffer(desc, pInitData, 0, 0);
        }

        static D3D11ShaderResourceView CreateBufferSRV(D3D11Device pDevice, D3D11Buffer pBuffer)
        {
            var descBuf = pBuffer.Description;

            D3D11ShaderResourceViewDesc desc;

            if (descBuf.MiscOptions.HasFlag(D3D11ResourceMiscOptions.BufferAllowRawViews))
            {
                // This is a Raw Buffer
                desc = new D3D11ShaderResourceViewDesc(pBuffer, DxgiFormat.R32Typeless, 0, descBuf.ByteWidth / 4, D3D11BufferExSrvOptions.Raw);
            }
            else if (descBuf.MiscOptions.HasFlag(D3D11ResourceMiscOptions.BufferStructured))
            {
                // This is a Structured Buffer
                desc = new D3D11ShaderResourceViewDesc(pBuffer, DxgiFormat.Unknown, 0, descBuf.ByteWidth / descBuf.StructureByteStride, D3D11BufferExSrvOptions.None);
            }
            else
            {
                throw new InvalidOperationException();
            }

            return pDevice.CreateShaderResourceView(pBuffer, desc);
        }

        static D3D11UnorderedAccessView CreateBufferUAV(D3D11Device pDevice, D3D11Buffer pBuffer)
        {
            var descBuf = pBuffer.Description;

            D3D11UnorderedAccessViewDesc desc;

            if (descBuf.MiscOptions.HasFlag(D3D11ResourceMiscOptions.BufferAllowRawViews))
            {
                // This is a Raw Buffer
                // Format must be DXGI_FORMAT_R32_TYPELESS, when creating Raw Unordered Access View
                desc = new D3D11UnorderedAccessViewDesc(pBuffer, DxgiFormat.R32Typeless, 0, descBuf.ByteWidth / 4, D3D11BufferUavOptions.Raw);
            }
            else if (descBuf.MiscOptions.HasFlag(D3D11ResourceMiscOptions.BufferStructured))
            {
                // This is a Structured Buffer
                // Format must be must be DXGI_FORMAT_UNKNOWN, when creating a View of a Structured Buffer
                desc = new D3D11UnorderedAccessViewDesc(pBuffer, DxgiFormat.Unknown, 0, descBuf.ByteWidth / descBuf.StructureByteStride, D3D11BufferUavOptions.None);
            }
            else
            {
                throw new InvalidOperationException();
            }

            return pDevice.CreateUnorderedAccessView(pBuffer, desc);
        }

        static void RunComputeShader(
            D3D11DeviceContext pd3dImmediateContext,
            D3D11ComputeShader pComputeShader,
            D3D11ShaderResourceView[] pShaderResourceViews,
            D3D11UnorderedAccessView pUnorderedAccessView,
            uint X, uint Y, uint Z)
        {
            pd3dImmediateContext.ComputeShaderSetShader(pComputeShader, null);
            pd3dImmediateContext.ComputeShaderSetShaderResources(0, pShaderResourceViews);
            pd3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new[] { pUnorderedAccessView }, new[] { 0U });

            pd3dImmediateContext.Dispatch(X, Y, Z);

            pd3dImmediateContext.ComputeShaderSetShader(null, null);
            pd3dImmediateContext.ComputeShaderSetUnorderedAccessViews(0, new D3D11UnorderedAccessView[] { null }, new[] { 0U });
            pd3dImmediateContext.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null, null });
            pd3dImmediateContext.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { null });
        }

        static D3D11Buffer CreateAndCopyToDebugBuf(D3D11Device pDevice, D3D11DeviceContext pd3dImmediateContext, D3D11Buffer pBuffer)
        {
            var desc = pBuffer.Description;

            desc.CpuAccessOptions = D3D11CpuAccessOptions.Read;
            desc.Usage = D3D11Usage.Staging;
            desc.BindOptions = D3D11BindOptions.None;
            desc.MiscOptions = D3D11ResourceMiscOptions.None;

            D3D11Buffer debugbuf = pDevice.CreateBuffer(desc);

#if DEBUG
            debugbuf.SetDebugName("Debug");
#endif

            pd3dImmediateContext.CopyResource(debugbuf, pBuffer);

            return debugbuf;
        }

        static void CleanupResources()
        {
            D3D11Utils.ReleaseAndNull(ref g_pBuf0SRV);
            D3D11Utils.ReleaseAndNull(ref g_pBuf1SRV);
            D3D11Utils.ReleaseAndNull(ref g_pBufResultUAV);
            D3D11Utils.ReleaseAndNull(ref g_pBuf0);
            D3D11Utils.ReleaseAndNull(ref g_pBuf1);
            D3D11Utils.ReleaseAndNull(ref g_pBufResult);
            D3D11Utils.ReleaseAndNull(ref g_pCS);

            deviceResources?.Release();
            deviceResources = null;
        }
    }
}
