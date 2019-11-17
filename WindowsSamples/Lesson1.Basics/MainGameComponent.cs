using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lesson1.Basics
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

        public void Update(StepTimer timer)
        {
        }

        public void Render()
        {
            this.deviceResources.D3DContext.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, null);
            this.deviceResources.D3DContext.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.071f, 0.04f, 0.561f, 1.0f });
        }
    }
}
