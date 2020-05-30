using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComputeShaderSort11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel91;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;
        }

        public void ReleaseDeviceDependentResources()
        {
        }

        public void CreateWindowSizeDependentResources()
        {
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, null);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
        }
    }
}
