using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfHost
{
    class Game : GameWindowBase
    {
        public float[] clearColor = new float[] { 0.071f, 0.04f, 0.561f, 1.0f };

        public Game()
        {
        }

        protected override void Render()
        {
            this.DeviceResources.D3DContext.OutputMergerSetRenderTargets(new[] { this.DeviceResources.D3DRenderTargetView }, null);
            this.DeviceResources.D3DContext.ClearRenderTargetView(this.DeviceResources.D3DRenderTargetView, this.clearColor);
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
