using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;

namespace OIT11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private readonly Scene scene = new Scene();

        private readonly Oit oit = new Oit();

        private XMMatrix worldMatrix;

        private XMMatrix viewMatrix;

        private XMMatrix projectionMatrix;

        public MainGameComponent()
        {
        }

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

            this.scene.CreateDeviceDependentResources(this.deviceResources);
            this.oit.CreateDeviceDependentResources(this.deviceResources);

            this.worldMatrix = XMMatrix.Identity;

            XMVector eye = new XMVector(0.0f, 0.5f, -3.0f, 0.0f);
            XMVector at = new XMVector(0.0f, 0.0f, 0.0f, 0.0f);
            XMVector up = new XMVector(0.0f, 1.0f, 0.0f, 0.0f);
            this.viewMatrix = XMMatrix.LookAtLH(eye, at, up);
        }

        public void ReleaseDeviceDependentResources()
        {
            this.scene.ReleaseDeviceDependentResources();
            this.oit.ReleaseDeviceDependentResources();
        }

        public void CreateWindowSizeDependentResources()
        {
            this.scene.CreateWindowSizeDependentResources();
            this.oit.CreateWindowSizeDependentResources();

            this.projectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight, 0.1f, 5000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
            this.scene.ReleaseWindowSizeDependentResources();
            this.oit.ReleaseWindowSizeDependentResources();
        }

        public void Update(StepTimer timer)
        {
            this.scene.Update(timer);
            this.oit.Update(timer);
        }

        public void Render()
        {
            XMMatrix worldViewProjection = this.worldMatrix * this.viewMatrix * this.projectionMatrix;
            this.scene.SetWorldViewProj(worldViewProjection);

            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.125f, 0.3f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            //this.scene.Render();

            context.OutputMergerGetRenderTargets(1, out D3D11RenderTargetView[] origRTV, out D3D11DepthStencilView origDSV);

            this.oit.SetScene(this.scene);
            this.oit.SetRenderTarget(origRTV[0], origDSV);

            this.oit.Render();

            this.oit.SetScene(null);
            this.oit.SetRenderTarget(null, null);

            context.OutputMergerSetRenderTargets(origRTV, origDSV);

            D3D11Utils.DisposeAndNull(ref origRTV[0]);
            D3D11Utils.DisposeAndNull(ref origDSV);
        }
    }
}
