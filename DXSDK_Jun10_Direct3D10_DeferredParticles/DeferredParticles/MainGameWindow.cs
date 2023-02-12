using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace DeferredParticles
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private SdkModelViewerCamera g_Camera;

        private DirectionWidget g_LightControl;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        protected override void Init()
        {
            Particles.SetDefaultSeed();

            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            this.g_Camera = new SdkModelViewerCamera();
            this.g_LightControl = new DirectionWidget();

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            var vLightDir = new XMFloat3(1, 1, 0);
            vLightDir = XMVector3.Normalize(vLightDir);
            this.g_LightControl.SetLightDirection(vLightDir);

            // Setup the camera's view parameters
            XMFloat3 vecEye = new(0.0f, 150.0f, 336.0f);
            XMFloat3 vecAt = new(0.0f, 0.0f, 0.0f);
            this.g_Camera.SetViewParams(vecEye, vecAt);
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

            // Setup the camera's projection parameters
            float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / (float)this.DeviceResources.BackBufferHeight;
            this.g_Camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 2.0f, 4000.0f);
            this.g_Camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.g_Camera.SetButtonMasks(SdkCameraMouseKeys.MiddleButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.LeftButton);
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            this.g_Camera.FrameMove(this.Timer.ElapsedSeconds);

            this.mainGameComponent.EyePosition = this.g_Camera.GetEyePt();
            this.mainGameComponent.WorldMatrix = this.g_Camera.GetWorldMatrix();
            this.mainGameComponent.ViewMatrix = this.g_Camera.GetViewMatrix();
            this.mainGameComponent.ProjectionMatrix = this.g_Camera.GetProjMatrix();
            this.mainGameComponent.LightDirection = this.g_LightControl.GetLightDirection();
            this.mainGameComponent.Update(this.Timer);
        }

        protected override void Render()
        {
            this.mainGameComponent.EyePosition = this.g_Camera.GetEyePt();
            this.mainGameComponent.WorldMatrix = this.g_Camera.GetWorldMatrix();
            this.mainGameComponent.ViewMatrix = this.g_Camera.GetViewMatrix();
            this.mainGameComponent.ProjectionMatrix = this.g_Camera.GetProjMatrix();
            this.mainGameComponent.LightDirection = this.g_LightControl.GetLightDirection();
            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            this.g_LightControl?.HandleMessages(this.Handle, msg, wParam, lParam);

            // Pass all remaining windows messages to camera so it can respond to user input
            this.g_Camera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown)
            {
                switch (key)
                {
                    case VirtualKey.R:
                        this.mainGameComponent.ResetBuildings();
                        break;

                    case VirtualKey.D:
                        this.mainGameComponent.RenderDeferred = !this.mainGameComponent.RenderDeferred;
                        break;
                }
            }
        }
    }
}
