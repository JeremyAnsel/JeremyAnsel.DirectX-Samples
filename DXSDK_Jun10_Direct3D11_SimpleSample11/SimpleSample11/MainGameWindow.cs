﻿using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace SimpleSample11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        // A model viewing camera
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
            XMFloat3 vecEye = new(0.0f, 0.0f, -5.0f);
            XMFloat3 vecAt = new(0.0f, 0.0f, -0.0f);
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
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 0.1f, 1000.0f);
            this.camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.camera.SetButtonMasks(SdkCameraMouseKeys.LeftButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.MiddleButton);
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
            this.mainGameComponent.WorldMatrix = this.camera.GetWorldMatrix();
            this.mainGameComponent.ViewMatrix = this.camera.GetViewMatrix();
            this.mainGameComponent.ProjectionMatrix = this.camera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            // Pass all remaining windows messages to camera so it can respond to user input
            this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
