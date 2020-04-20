using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.D3D11;

namespace Tutorial01
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
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, null);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });
        }
    }
}
