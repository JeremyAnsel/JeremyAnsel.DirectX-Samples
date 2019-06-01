using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Direct2DAndDWrite
{
    class Game : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        public Game()
        {
        }

        protected override void Init()
        {
            this.mainGameComponent = new MainGameComponent();

            if (this.mainGameComponent.MinimalFeatureLevel > this.RequestedD3DFeatureLevel)
            {
                this.RequestedD3DFeatureLevel = this.mainGameComponent.MinimalFeatureLevel;
            }

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();

            this.mainGameComponent.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            this.mainGameComponent.CreateWindowSizeDependentResources();
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            this.mainGameComponent.Update(this.Timer);
        }

        protected override void Render()
        {
            this.mainGameComponent.Render();
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown && key == VirtualKey.F12)
            {
                this.FpsTextRenderer.IsEnabled = !this.FpsTextRenderer.IsEnabled;
            }
        }
    }
}
