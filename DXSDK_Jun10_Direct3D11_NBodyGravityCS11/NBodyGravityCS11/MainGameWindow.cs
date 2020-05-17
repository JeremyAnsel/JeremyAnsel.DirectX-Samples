using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBodyGravityCS11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private SdkModelViewerCamera camera;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            this.camera = new SdkModelViewerCamera();

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            // Setup the camera's view parameters
            XMFloat3 vecEye = new XMFloat3(-MainGameComponent.Spread * 2, MainGameComponent.Spread * 4, -MainGameComponent.Spread * 3);
            XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);
            this.camera.SetViewParams(vecEye, vecAt);
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
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 10.0f, 500000.0f);
            this.camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.camera.SetButtonMasks(0, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.LeftButton | SdkCameraMouseKeys.MiddleButton | SdkCameraMouseKeys.RightButton);
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

            // Update the camera's position based on user input 
            this.camera.FrameMove(this.Timer.ElapsedSeconds);
        }

        protected override void Render()
        {
            // Get the projection & view matrix from the camera class
            this.mainGameComponent.ViewMatrix = this.camera.GetViewMatrix();
            this.mainGameComponent.ProjectionMatrix = this.camera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            // Pass all windows messages to camera so it can respond to user input
            this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown)
            {
                switch (key)
                {
                    case VirtualKey.Space:
                        this.mainGameComponent.DiskGalaxyFormationType = this.mainGameComponent.DiskGalaxyFormationType == 0 ? 1 : 0;
                        this.DeviceResources.HandleDeviceLost();
                        break;
                }
            }
        }
    }
}
