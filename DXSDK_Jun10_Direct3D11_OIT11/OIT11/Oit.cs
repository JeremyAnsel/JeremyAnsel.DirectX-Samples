using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JeremyAnsel.DirectX.D3D11;
using System.IO;
using JeremyAnsel.DirectX.Dxgi;

namespace OIT11
{
    class Oit : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11PixelShader fragmentCountPS;

        private D3D11ComputeShader createPrefixSumPass0CS;

        private D3D11ComputeShader createPrefixSumPass1CS;

        private D3D11PixelShader fillDeepBufferPS;

        private D3D11ComputeShader sortAndRenderCS;

        private D3D11DepthStencilState depthStencilState;

        private D3D11Buffer computeShaderConstantBuffer;

        private D3D11Buffer pixelShaderConstantBuffer;

        // Keeps a count of the number of fragments rendered to each pixel
        private D3D11Texture2D fragmentCountBuffer;

        // Count of total fragments in the frame buffer preceding each pixel
        private D3D11Buffer prefixSum;

        // Buffer that holds the depth of each fragment
        private D3D11Buffer deepBuffer;

        // Buffer that holds the color of each fragment
        private D3D11Buffer deepBufferColor;

        private D3D11ShaderResourceView fragmentCountRV;

        private D3D11UnorderedAccessView fragmentCountUAV;

        private D3D11UnorderedAccessView prefixSumUAV;

        private D3D11UnorderedAccessView deepBufferUAV;

        private D3D11UnorderedAccessView deepBufferColorUAV;

        private D3D11Texture2D screenTexture;

        private D3D11RenderTargetView screenTextureRTV;

        private D3D11UnorderedAccessView screenTextureUAV;

        private IGameComponent scene;

        private D3D11RenderTargetView renderTargetView;

        private D3D11DepthStencilView depthStencilView;

        public Oit()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel110;
            }
        }

        public void SetScene(IGameComponent scene)
        {
            this.scene = scene;
        }

        public void SetRenderTarget(D3D11RenderTargetView rtv, D3D11DepthStencilView dsv)
        {
            this.renderTargetView = rtv;
            this.depthStencilView = dsv;
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            this.fragmentCountPS = this.deviceResources.D3DDevice.CreatePixelShader(
                File.ReadAllBytes("FragmentCountPS.cso"),
                null);

            this.createPrefixSumPass0CS = this.deviceResources.D3DDevice.CreateComputeShader(
                File.ReadAllBytes("CreatePrefixSumPass0CS.cso"),
                null);

            this.createPrefixSumPass1CS = this.deviceResources.D3DDevice.CreateComputeShader(
                File.ReadAllBytes("CreatePrefixSumPass1CS.cso"),
                null);

            this.fillDeepBufferPS = this.deviceResources.D3DDevice.CreatePixelShader(
                File.ReadAllBytes("FillDeepBufferPS.cso"),
                null);

            this.sortAndRenderCS = this.deviceResources.D3DDevice.CreateComputeShader(
                File.ReadAllBytes("SortAndRenderCS.cso"),
                null);

            var depthStencilDesc = new D3D11DepthStencilDesc
            {
                IsDepthEnabled = false,
                IsStencilEnabled = false
            };

            this.depthStencilState = this.deviceResources.D3DDevice.CreateDepthStencilState(depthStencilDesc);

            this.computeShaderConstantBuffer = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(OitComputeShaderConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));

            this.pixelShaderConstantBuffer = this.deviceResources.D3DDevice.CreateBuffer(
                new D3D11BufferDesc(OitPixelShaderConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.fragmentCountPS);
            D3D11Utils.DisposeAndNull(ref this.createPrefixSumPass0CS);
            D3D11Utils.DisposeAndNull(ref this.createPrefixSumPass1CS);
            D3D11Utils.DisposeAndNull(ref this.fillDeepBufferPS);
            D3D11Utils.DisposeAndNull(ref this.sortAndRenderCS);
            D3D11Utils.DisposeAndNull(ref this.depthStencilState);
            D3D11Utils.DisposeAndNull(ref this.computeShaderConstantBuffer);
            D3D11Utils.DisposeAndNull(ref this.pixelShaderConstantBuffer);
        }

        public void CreateWindowSizeDependentResources()
        {
            uint width = this.deviceResources.BackBufferWidth;
            uint height = this.deviceResources.BackBufferHeight;

            // Create fragment count buffer
            this.fragmentCountBuffer = this.deviceResources.D3DDevice.CreateTexture2D(
                new D3D11Texture2DDesc(
                    DxgiFormat.R32UInt,
                    width,
                    height,
                    1,
                    1,
                    D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource));

            // Create prefix sum buffer
            this.prefixSum = this.deviceResources.D3DDevice.CreateBuffer(new D3D11BufferDesc(
                width * height * 4,
                D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.None,
                4));

            // Create the deep frame buffer.
            // This simple allocation scheme for the deep frame buffer allocates space for 8 times the size of the
            // frame buffer, which means that it can hold an average of 8 fragments per pixel.  This will usually waste some
            // space, and in some cases of high overdraw the buffer could run into problems with overflow.  It 
            // may be useful to make the buffer size more intelligent to avoid these problems.
            this.deepBuffer = this.deviceResources.D3DDevice.CreateBuffer(new D3D11BufferDesc(
                width * height * 8 * 4,
                D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.None,
                4));

            // Create deep frame buffer for color
            this.deepBufferColor = this.deviceResources.D3DDevice.CreateBuffer(new D3D11BufferDesc(
                width * height * 8 * 4 * 1,
                D3D11BindOptions.UnorderedAccess | D3D11BindOptions.ShaderResource,
                D3D11Usage.Default,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.None,
                4 * 1));

            this.fragmentCountRV = this.deviceResources.D3DDevice.CreateShaderResourceView(
                this.fragmentCountBuffer,
                new D3D11ShaderResourceViewDesc(
                    D3D11SrvDimension.Texture2D,
                    DxgiFormat.R32UInt,
                    0,
                    1));

            this.fragmentCountUAV = this.deviceResources.D3DDevice.CreateUnorderedAccessView(
                this.fragmentCountBuffer,
                new D3D11UnorderedAccessViewDesc(
                    D3D11UavDimension.Texture2D,
                    DxgiFormat.R32UInt,
                    0));

            this.prefixSumUAV = this.deviceResources.D3DDevice.CreateUnorderedAccessView(
                this.prefixSum,
                new D3D11UnorderedAccessViewDesc(
                    D3D11UavDimension.Buffer,
                    DxgiFormat.R32UInt,
                    0,
                    width * height));

            this.deepBufferUAV = this.deviceResources.D3DDevice.CreateUnorderedAccessView(
                this.deepBuffer,
                new D3D11UnorderedAccessViewDesc(
                    D3D11UavDimension.Buffer,
                    DxgiFormat.R32Float,
                    0,
                    width * height * 8));

            this.deepBufferColorUAV = this.deviceResources.D3DDevice.CreateUnorderedAccessView(
                this.deepBufferColor,
                new D3D11UnorderedAccessViewDesc(
                    D3D11UavDimension.Buffer,
                    DxgiFormat.B8G8R8A8UNorm,
                    0,
                    width * height * 8));

            this.screenTexture = this.deviceResources.D3DDevice.CreateTexture2D(
                    new D3D11Texture2DDesc(
                        DxgiFormat.B8G8R8A8UNorm,
                        width,
                        height,
                        1,
                        1,
                        D3D11BindOptions.RenderTarget | D3D11BindOptions.UnorderedAccess));

            this.screenTextureRTV = this.deviceResources.D3DDevice.CreateRenderTargetView(this.screenTexture, null);
            this.screenTextureUAV = this.deviceResources.D3DDevice.CreateUnorderedAccessView(this.screenTexture, new D3D11UnorderedAccessViewDesc(D3D11UavDimension.Texture2D, DxgiFormat.B8G8R8A8UNorm, 0));
        }

        public void ReleaseWindowSizeDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.fragmentCountBuffer);
            D3D11Utils.DisposeAndNull(ref this.prefixSum);
            D3D11Utils.DisposeAndNull(ref this.deepBuffer);
            D3D11Utils.DisposeAndNull(ref this.deepBufferColor);
            D3D11Utils.DisposeAndNull(ref this.fragmentCountRV);
            D3D11Utils.DisposeAndNull(ref this.fragmentCountUAV);
            D3D11Utils.DisposeAndNull(ref this.prefixSumUAV);
            D3D11Utils.DisposeAndNull(ref this.deepBufferUAV);
            D3D11Utils.DisposeAndNull(ref this.deepBufferColorUAV);
            D3D11Utils.DisposeAndNull(ref this.screenTexture);
            D3D11Utils.DisposeAndNull(ref this.screenTextureRTV);
            D3D11Utils.DisposeAndNull(ref this.screenTextureUAV);
        }

        public void Update(StepTimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerGetDepthStencilState(out D3D11DepthStencilState depthStencilStateStored, out uint stencilRef);

            this.CreateFragmentCount();
            this.CreatePrefixSum();
            this.FillDeepBuffer();
            this.SortAndRenderFragments();

            context.OutputMergerSetDepthStencilState(depthStencilStateStored, stencilRef);
        }

        private void CreateFragmentCount()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the render target & depth/stencil
            context.ClearRenderTargetView(this.renderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });
            context.ClearDepthStencilView(this.depthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Clear the fragment count buffer
            context.ClearUnorderedAccessViewUInt(this.fragmentCountUAV, new uint[] { 0, 0, 0, 0 });

            // Draw the transparent geometry
            context.OutputMergerSetRenderTargetsAndUnorderedAccessViews(
                new D3D11RenderTargetView[] { this.renderTargetView },
                this.depthStencilView,
                1,
                new D3D11UnorderedAccessView[] { this.fragmentCountUAV },
                new uint[] { 0 });
            context.OutputMergerSetDepthStencilState(this.depthStencilState, 0);
            context.PixelShaderSetShader(this.fragmentCountPS, null);

            this.scene.Render();

            // Set render target and depth/stencil views to NULL,
            //   we'll need to read the RTV in a shader later
            context.OutputMergerSetRenderTargets(new D3D11RenderTargetView[] { null }, null);
        }

        private void CreatePrefixSum()
        {
            var context = this.deviceResources.D3DContext;

            // prepare the constant buffer
            OitComputeShaderConstantBufferData cb;
            cb.FrameWidth = this.deviceResources.BackBufferWidth;
            cb.FrameHeight = this.deviceResources.BackBufferHeight;
            cb.PassSize = 0;
            cb.Reserved = 0;
            context.UpdateSubresource(this.computeShaderConstantBuffer, 0, null, cb, 0, 0);
            context.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { this.computeShaderConstantBuffer });

            // First pass : convert the 2D frame buffer to a 1D array.  We could simply 
            //   copy the contents over, but while we're at it, we may as well do 
            //   some work and save a pass later, so we do the first summation pass;  
            //   add the values at the even indices to the values at the odd indices.
            context.ComputeShaderSetShader(this.createPrefixSumPass0CS, null);

            context.ComputeShaderSetUnorderedAccessViews(3, new D3D11UnorderedAccessView[] { this.prefixSumUAV }, new uint[] { 0 });

            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { this.fragmentCountRV });
            context.Dispatch(this.deviceResources.BackBufferWidth, this.deviceResources.BackBufferHeight, 1);

            // Second and following passes : each pass distributes the sum of the first half of the group
            //   to the second half of the group.  There are n/groupsize groups in each pass.
            //   Each pass doubles the group size until it is the size of the buffer.
            //   The resulting buffer holds the prefix sum of all preceding values in each
            //   position 
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetUnorderedAccessViews(3, new D3D11UnorderedAccessView[] { null }, new uint[] { 0 });

            // Perform the passes.  The first pass would have been i = 2, but it was performed earlier
            for (uint i = 4; i < this.deviceResources.BackBufferWidth * this.deviceResources.BackBufferHeight * 2; i *= 2)
            {
                cb.FrameWidth = this.deviceResources.BackBufferWidth;
                cb.FrameHeight = this.deviceResources.BackBufferHeight;
                cb.PassSize = i;
                cb.Reserved = 0;
                context.UpdateSubresource(this.computeShaderConstantBuffer, 0, null, cb, 0, 0);
                context.ComputeShaderSetConstantBuffers(0, new D3D11Buffer[] { this.computeShaderConstantBuffer });

                context.ComputeShaderSetShader(this.createPrefixSumPass1CS, null);

                context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { this.fragmentCountRV });
                context.ComputeShaderSetUnorderedAccessViews(3, new D3D11UnorderedAccessView[] { this.prefixSumUAV }, new uint[] { 0 });

                // the "ceil((float) m_nFrameWidth*m_nFrameHeight/i)" calculation ensures that 
                //    we dispatch enough threads to cover the entire range.
                uint countX = (uint)Math.Ceiling((float)this.deviceResources.BackBufferWidth * this.deviceResources.BackBufferHeight / i);
                context.Dispatch(countX, 1, 1);
            }

            // Clear out the resource and unordered access views
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetUnorderedAccessViews(3, new D3D11UnorderedAccessView[] { null }, new uint[] { 0 });
        }

        private void FillDeepBuffer()
        {
            var context = this.deviceResources.D3DContext;

            context.ClearUnorderedAccessViewFloat(this.deepBufferUAV, new float[] { 1.0f, 0.0f, 0.0f, 0.0f });
            context.ClearUnorderedAccessViewFloat(this.deepBufferColorUAV, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });
            context.ClearUnorderedAccessViewUInt(this.fragmentCountUAV, new uint[] { 0, 0, 0, 0 });

            context.ClearRenderTargetView(this.renderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });
            context.ClearDepthStencilView(this.depthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            context.OutputMergerSetRenderTargetsAndUnorderedAccessViews(
                new D3D11RenderTargetView[] { this.renderTargetView },
                this.depthStencilView,
                1,
                new D3D11UnorderedAccessView[] { this.fragmentCountUAV, this.deepBufferUAV, this.deepBufferColorUAV, this.prefixSumUAV },
                new uint[] { 0, 0, 0, 0 });

            context.PixelShaderSetShader(this.fillDeepBufferPS, null);

            OitPixelShaderConstantBufferData cb;
            cb.FrameWidth = this.deviceResources.BackBufferWidth;
            cb.FrameHeight = this.deviceResources.BackBufferHeight;
            cb.Reserved0 = 0;
            cb.Reserved1 = 0;
            context.UpdateSubresource(this.pixelShaderConstantBuffer, 0, null, cb, 0, 0);
            context.PixelShaderSetConstantBuffers(0, new D3D11Buffer[] { this.pixelShaderConstantBuffer });

            this.scene.Render();

            context.OutputMergerSetRenderTargets(new D3D11RenderTargetView[] { null }, this.depthStencilView);
        }

        private void SortAndRenderFragments()
        {
            var context = this.deviceResources.D3DContext;

            context.ClearRenderTargetView(this.renderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });
            context.ClearRenderTargetView(this.screenTextureRTV, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });

            context.ComputeShaderSetUnorderedAccessViews(
                0,
                new D3D11UnorderedAccessView[] { this.deepBufferUAV, this.deepBufferColorUAV, this.screenTextureUAV, this.prefixSumUAV },
                new uint[] { 0, 0, 0, 0 });

            context.ComputeShaderSetShader(this.sortAndRenderCS, null);
            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { this.fragmentCountRV });

            context.Dispatch(this.deviceResources.BackBufferWidth, this.deviceResources.BackBufferHeight, 1);

            context.CopyResource(this.deviceResources.BackBuffer, this.screenTexture);

            context.ComputeShaderSetShaderResources(0, new D3D11ShaderResourceView[] { null });
            context.ComputeShaderSetUnorderedAccessViews(
                0,
                new D3D11UnorderedAccessView[] { null, null, null, null },
                new uint[] { 0, 0, 0, 0 });
        }
    }
}
