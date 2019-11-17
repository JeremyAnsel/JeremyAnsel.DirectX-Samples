using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lesson5.Components
{
    class BasicLoader
    {
        private readonly D3D11Device d3dDevice;

        public BasicLoader(D3D11Device d3dDevice)
        {
            this.d3dDevice = d3dDevice ?? throw new ArgumentNullException(nameof(d3dDevice));
        }

        public void LoadTexture(string filename, uint width, uint height, out D3D11Texture2D texture, out D3D11ShaderResourceView textureView)
        {
            byte[] textureData = File.ReadAllBytes(filename);

            this.CreateTexture(textureData, width, height, out texture, out textureView);
        }

        public void LoadShader(string filename, D3D11InputElementDesc[] layoutDesc, out D3D11VertexShader shader, out D3D11InputLayout layout)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateVertexShader(data, null);

            try
            {
                this.CreateInputLayout(data, layoutDesc, out layout);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref shader);
                throw;
            }
        }

        public void LoadShader(string filename, out D3D11PixelShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreatePixelShader(data, null);
        }

        public void LoadShader(string filename, out D3D11ComputeShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateComputeShader(data, null);
        }

        public void LoadShader(string filename, out D3D11GeometryShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateGeometryShader(data, null);
        }

        public void LoadShader(string filename, D3D11StreamOutputDeclarationEntry[] streamOutDeclaration, uint[] bufferStrides, uint rasterizedStream, out D3D11GeometryShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateGeometryShaderWithStreamOutput(data, streamOutDeclaration, bufferStrides, rasterizedStream, null);
        }

        public void LoadShader(string filename, out D3D11HullShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateHullShader(data, null);
        }

        public void LoadShader(string filename, out D3D11DomainShader shader)
        {
            byte[] data = File.ReadAllBytes(filename);

            shader = this.d3dDevice.CreateDomainShader(data, null);
        }

        private void CreateTexture(byte[] data, uint width, uint height, out D3D11Texture2D texture, out D3D11ShaderResourceView textureView)
        {
            D3D11Texture2DDesc textureDesc = new D3D11Texture2DDesc(DxgiFormat.R8G8B8A8UNorm, width, height, 1, 1);

            D3D11SubResourceData[] textureSubResData = new[]
                {
                    new D3D11SubResourceData(data, width * 4)
                };

            D3D11Texture2D texture2D = this.d3dDevice.CreateTexture2D(textureDesc, textureSubResData);
            D3D11ShaderResourceView shaderResourceView;

            try
            {
                D3D11ShaderResourceViewDesc textureViewDesc = new D3D11ShaderResourceViewDesc
                {
                    Format = textureDesc.Format,
                    ViewDimension = D3D11SrvDimension.Texture2D,
                    Texture2D = new D3D11Texture2DSrv
                    {
                        MipLevels = textureDesc.MipLevels,
                        MostDetailedMip = 0
                    }
                };

                shaderResourceView = this.d3dDevice.CreateShaderResourceView(texture2D, textureViewDesc);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref texture2D);
                throw;
            }

            texture = texture2D;
            textureView = shaderResourceView;
        }

        private void CreateInputLayout(byte[] bytecode, D3D11InputElementDesc[] layoutDesc, out D3D11InputLayout layout)
        {
            if (layoutDesc == null)
            {
                // If no input layout is specified, use the BasicVertex layout.
                D3D11InputElementDesc[] basicVertexLayoutDesc = new D3D11InputElementDesc[]
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
                        SemanticName = "TEXCOORD",
                        SemanticIndex = 0,
                        Format = DxgiFormat.R32G32Float,
                        InputSlot = 0,
                        AlignedByteOffset = 24,
                        InputSlotClass = D3D11InputClassification.PerVertexData,
                        InstanceDataStepRate = 0
                    }
                };

                layout = this.d3dDevice.CreateInputLayout(basicVertexLayoutDesc, bytecode);
            }
            else
            {
                layout = this.d3dDevice.CreateInputLayout(layoutDesc, bytecode);
            }
        }
    }
}
