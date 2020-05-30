//--------------------------------------------------------------------------------------
// File: ComputeShaderSort11.cpp
//
// Demonstrates how to use compute shaders to perform sorting on the GPU with DirectX 11.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace ComputeShaderSort11
{
    class Program
    {
        // The number of elements to sort is limited to an even power of 2
        // At minimum 8,192 elements - BITONIC_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE
        // At maximum 262,144 elements - BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE
        private const uint BITONIC_BLOCK_SIZE = 512;
        private const uint NUM_ELEMENTS = BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE;
        private const uint TRANSPOSE_BLOCK_SIZE = 16;
        private const uint MATRIX_WIDTH = BITONIC_BLOCK_SIZE;
        private const uint MATRIX_HEIGHT = NUM_ELEMENTS / BITONIC_BLOCK_SIZE;

        private static readonly uint[] data = new uint[NUM_ELEMENTS];
        private static readonly uint[] results = new uint[NUM_ELEMENTS];

        private static DeviceResources deviceResources;

        private static D3D11ComputeShader g_pComputeShaderBitonic;
        private static D3D11ComputeShader g_pComputeShaderTranspose;
        private static D3D11Buffer g_pCB;
        private static D3D11Buffer g_pBuffer1;
        private static D3D11ShaderResourceView g_pBuffer1SRV;
        private static D3D11UnorderedAccessView g_pBuffer1UAV;
        private static D3D11Buffer g_pBuffer2;
        private static D3D11ShaderResourceView g_pBuffer2SRV;
        private static D3D11UnorderedAccessView g_pBuffer2UAV;
        private static D3D11Buffer g_pReadBackBuffer;

        static void Main(string[] args)
        {
            try
            {
                InitData();
                InitDevice();
                CreateResources();

                Console.WriteLine($"Sorting {NUM_ELEMENTS} Elements");

                // GPU Bitonic Sort
                Console.WriteLine("Starting GPU Bitonic Sort...");
                var start1 = Stopwatch.StartNew();
                GPUSort();
                start1.Stop();
                Console.WriteLine($"...GPU Bitonic Sort Finished in {start1.Elapsed}");

                // Sort the data on the CPU to compare for correctness
                Console.WriteLine("Starting CPU Sort...");
                var start2 = Stopwatch.StartNew();
                CPUSort();
                start2.Stop();
                Console.WriteLine($"...CPU Sort Finished in {start2.Elapsed}");

                // Compare the results for correctness
                bool bComparisonSucceeded = true;
                for (int i = 0; i < NUM_ELEMENTS; i++)
                {
                    if (data[i] != results[i])
                    {
                        bComparisonSucceeded = false;
                        break;
                    }
                }

                Console.WriteLine("Comparison " + (bComparisonSucceeded ? "Succeeded" : "FAILED"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                CleanupResources();
            }

            Console.ReadKey(false);
        }

        static void InitData()
        {
            var random = new Random();

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (uint)random.Next();
            }
        }

        static void InitDevice()
        {
            deviceResources = new RenderTargetDeviceResources(1, 1, D3D11FeatureLevel.FeatureLevel110, null);
        }

        static void CreateResources()
        {
            var d3dDevice = deviceResources.D3DDevice;

            // Create the Bitonic Sort Compute Shader
            g_pComputeShaderBitonic = d3dDevice.CreateComputeShader(File.ReadAllBytes("CSSortBitonic.cso"), null);

            // Create the Matrix Transpose Compute Shader
            g_pComputeShaderTranspose = d3dDevice.CreateComputeShader(File.ReadAllBytes("CSSortTranspose.cso"), null);

            // Create the Const Buffer
            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            g_pCB = d3dDevice.CreateBuffer(constantBufferDesc);

            // Create the Buffer of Elements
            // Create 2 buffers for switching between when performing the transpose
            var bufferDesc = new D3D11BufferDesc(
                NUM_ELEMENTS * sizeof(uint),
                D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.BufferStructured,
                sizeof(uint));

            g_pBuffer1 = d3dDevice.CreateBuffer(bufferDesc);
            g_pBuffer2 = d3dDevice.CreateBuffer(bufferDesc);

            // Create the Shader Resource View for the Buffers
            // This is used for reading the buffer during the transpose
            var srvbufferDesc = new D3D11ShaderResourceViewDesc(D3D11SrvDimension.Buffer, DxgiFormat.Unknown)
            {
                Buffer = new D3D11BufferSrv { ElementWidth = NUM_ELEMENTS }
            };

            g_pBuffer1SRV = d3dDevice.CreateShaderResourceView(g_pBuffer1, srvbufferDesc);
            g_pBuffer2SRV = d3dDevice.CreateShaderResourceView(g_pBuffer2, srvbufferDesc);

            // Create the Unordered Access View for the Buffers
            // This is used for writing the buffer during the sort and transpose
            var uavbufferDesc = new D3D11UnorderedAccessViewDesc(D3D11UavDimension.Buffer, DxgiFormat.Unknown)
            {
                Buffer = new D3D11BufferUav { NumElements = NUM_ELEMENTS }
            };

            g_pBuffer1UAV = d3dDevice.CreateUnorderedAccessView(g_pBuffer1, uavbufferDesc);
            g_pBuffer2UAV = d3dDevice.CreateUnorderedAccessView(g_pBuffer2, uavbufferDesc);

            // Create the Readback Buffer
            // This is used to read the results back to the CPU
            var readbackBufferDesc = new D3D11BufferDesc(
                NUM_ELEMENTS * sizeof(uint),
                D3D11BindOptions.None,
                D3D11Usage.Staging,
                D3D11CpuAccessOptions.Read,
                D3D11ResourceMiscOptions.None,
                sizeof(uint));

            g_pReadBackBuffer = d3dDevice.CreateBuffer(readbackBufferDesc);
        }

        static void GPUSortSetConstants(uint iLevel, uint iLevelMask, uint iWidth, uint iHeight)
        {
            var d3dContext = deviceResources.D3DContext;

            var cb = new ConstantBufferData
            {
                iLevel = iLevel,
                iLevelMask = iLevelMask,
                iWidth = iWidth,
                iHeight = iHeight
            };

            d3dContext.UpdateSubresource(g_pCB, 0, null, cb, 0, 0);
        }

        static void GPUSort()
        {
            var d3dContext = deviceResources.D3DContext;

            // Upload the data
            d3dContext.UpdateSubresource(g_pBuffer1, 0, null, data, 0, 0);

            d3dContext.ComputeShaderSetConstantBuffers(0, new[] { g_pCB });

            // Sort the data
            // First sort the rows for the levels <= to the block size
            for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level *= 2)
            {
                GPUSortSetConstants(level, level, MATRIX_HEIGHT, MATRIX_WIDTH);

                // Sort the row data
                d3dContext.ComputeShaderSetUnorderedAccessViews(0, new[] { g_pBuffer1UAV }, new[] { 0U });
                d3dContext.ComputeShaderSetShader(g_pComputeShaderBitonic, null);
                d3dContext.Dispatch(NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1);
            }

            // Then sort the rows and columns for the levels > than the block size
            // Transpose. Sort the Columns. Transpose. Sort the Rows.
            for (uint level = (BITONIC_BLOCK_SIZE * 2); level <= NUM_ELEMENTS; level *= 2)
            {
                GPUSortSetConstants(level / BITONIC_BLOCK_SIZE, (level & ~NUM_ELEMENTS) / BITONIC_BLOCK_SIZE, MATRIX_WIDTH, MATRIX_HEIGHT);

                // Transpose the data from buffer 1 into buffer 2
                d3dContext.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
                d3dContext.ComputeShaderSetUnorderedAccessViews(0, new[] { g_pBuffer2UAV }, new[] { 0U });
                d3dContext.ComputeShaderSetShaderResources(0, new[] { g_pBuffer1SRV });
                d3dContext.ComputeShaderSetShader(g_pComputeShaderTranspose, null);
                d3dContext.Dispatch(MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the transposed column data
                d3dContext.ComputeShaderSetShader(g_pComputeShaderBitonic, null);
                d3dContext.Dispatch(NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1);

                GPUSortSetConstants(BITONIC_BLOCK_SIZE, level, MATRIX_HEIGHT, MATRIX_WIDTH);

                // Transpose the data from buffer 2 back into buffer 1
                d3dContext.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
                d3dContext.ComputeShaderSetUnorderedAccessViews(0, new[] { g_pBuffer1UAV }, new[] { 0U });
                d3dContext.ComputeShaderSetShaderResources(0, new[] { g_pBuffer2SRV });
                d3dContext.ComputeShaderSetShader(g_pComputeShaderTranspose, null);
                d3dContext.Dispatch(MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the row data
                d3dContext.ComputeShaderSetShader(g_pComputeShaderBitonic, null);
                d3dContext.Dispatch(NUM_ELEMENTS / BITONIC_BLOCK_SIZE, 1, 1);
            }

            // Download the data
            d3dContext.CopyResource(g_pReadBackBuffer, g_pBuffer1);
            D3D11MappedSubResource mappedResource = d3dContext.Map(g_pReadBackBuffer, 0, D3D11MapCpuPermission.Read, D3D11MapOptions.None);
            try
            {
                for (int i = 0; i < NUM_ELEMENTS; i++)
                {
                    results[i] = (uint)Marshal.ReadInt32(mappedResource.Data + i * sizeof(uint));
                }
            }
            finally
            {
                d3dContext.Unmap(g_pReadBackBuffer, 0);
            }
        }

        static void CPUSort()
        {
            Array.Sort(data, (x, y) => x.CompareTo(y));
        }

        static void CleanupResources()
        {
            D3D11Utils.ReleaseAndNull(ref g_pReadBackBuffer);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer2UAV);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer1UAV);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer2SRV);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer1SRV);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer2);
            D3D11Utils.ReleaseAndNull(ref g_pBuffer1);
            D3D11Utils.ReleaseAndNull(ref g_pCB);
            D3D11Utils.ReleaseAndNull(ref g_pComputeShaderTranspose);
            D3D11Utils.ReleaseAndNull(ref g_pComputeShaderBitonic);

            deviceResources.Release();
            deviceResources = null;
        }
    }
}
