using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;

namespace Lesson1.Basics
{
    class Lesson1Game : GameWindowBase
    {
        public Lesson1Game()
        {
        }

        protected override void Init()
        {
            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Render()
        {
            this.DeviceResources.D3DContext.OutputMergerSetRenderTargets(new[] { this.DeviceResources.D3DRenderTargetView }, null);
            this.DeviceResources.D3DContext.ClearRenderTargetView(this.DeviceResources.D3DRenderTargetView, new float[] { 0.071f, 0.04f, 0.561f, 1.0f });
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
