using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContactHardeningShadows11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private float sunWidth = 2.0f;

        // The first person viewer camera
        private SdkFirstPersonCamera camera;

        // A model viewing camera for the light
        private SdkModelViewerCamera lightCamera;

        private bool g_bLeftButtonDown = false;

        private bool g_bRightButtonDown = false;

        private bool g_bMiddleButtonDown = false;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public float SunWidth
        {
            get
            {
                return this.sunWidth;
            }

            set
            {
                if (value != this.sunWidth)
                {
                    this.sunWidth = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            this.camera = new SdkFirstPersonCamera();
            this.camera.SetRotateButtons(true, false, false);

            this.lightCamera = new SdkModelViewerCamera();
            this.lightCamera.SetButtonMasks(SdkCameraMouseKeys.RightButton, 0, 0);

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            // Setup the camera
            this.camera.Reset();
            XMFloat3 vecEye = new XMFloat3(0.95f, 5.83f, -14.48f);
            XMFloat3 vecAt = new XMFloat3(0.90f, 5.44f, -13.56f);
            this.camera.SetViewParams(vecEye, vecAt);

            this.lightCamera.Reset();
            XMFloat3 vecEyeL = new XMFloat3(0, 0, 0);
            XMFloat3 vecAtL = new XMFloat3(0, -0.5f, 1);
            this.lightCamera.SetViewParams(vecEyeL, vecAtL);
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
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 0.5f, 100.0f);
            this.lightCamera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 10.0f, 100.0f);
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

            this.camera.FrameMove(this.Timer.ElapsedSeconds);
            this.lightCamera.FrameMove(this.Timer.ElapsedSeconds);
        }

        protected override void Render()
        {
            this.mainGameComponent.SunWidth = this.SunWidth;
            this.mainGameComponent.WorldMatrix = this.camera.GetWorldMatrix();
            this.mainGameComponent.ViewMatrix = this.camera.GetViewMatrix();
            this.mainGameComponent.ProjectionMatrix = this.camera.GetProjMatrix();
            this.mainGameComponent.LightWorldMatrix = this.lightCamera.GetWorldMatrix();
            this.mainGameComponent.LightViewMatrix = this.lightCamera.GetViewMatrix();
            this.mainGameComponent.LightProjectionMatrix = this.lightCamera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            // Pass all windows messages to camera so it can respond to user input
            this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
            this.lightCamera?.HandleMessages(this.Handle, msg, wParam, lParam);

            switch (msg)
            {
                case WindowMessageType.LeftButtonDown:
                case WindowMessageType.MiddleButtonDown:
                case WindowMessageType.RightButtonDown:
                    {
                        bool bLeftButtonDown = ((MouseKeys)wParam & MouseKeys.LeftButton) != 0;
                        bool bRightButtonDown = ((MouseKeys)wParam & MouseKeys.RightButton) != 0;
                        bool bMiddleButtonDown = ((MouseKeys)wParam & MouseKeys.MiddleButton) != 0;

                        bool bOldLeftButtonDown = g_bLeftButtonDown;
                        bool bOldRightButtonDown = g_bRightButtonDown;
                        bool bOldMiddleButtonDown = g_bMiddleButtonDown;
                        g_bLeftButtonDown = bLeftButtonDown;
                        g_bMiddleButtonDown = bMiddleButtonDown;
                        g_bRightButtonDown = bRightButtonDown;

                        if (bOldLeftButtonDown && !g_bLeftButtonDown)
                        {
                            this.camera.SetEnablePositionMovement(false);
                        }
                        else if (!bOldLeftButtonDown && g_bLeftButtonDown)
                        {
                            this.camera.SetEnablePositionMovement(true);
                        }

                        if (!bOldRightButtonDown && g_bRightButtonDown)
                        {
                            this.camera.SetEnablePositionMovement(false);
                        }

                        if (bOldMiddleButtonDown && !g_bMiddleButtonDown)
                        {
                            this.lightCamera.SetEnablePositionMovement(false);
                        }
                        else if (!bOldMiddleButtonDown && g_bMiddleButtonDown)
                        {
                            this.lightCamera.SetEnablePositionMovement(true);
                            this.camera.SetEnablePositionMovement(false);
                        }

                        // If no mouse button is down at all, enable camera movement.
                        if (!g_bLeftButtonDown && !g_bRightButtonDown && !g_bMiddleButtonDown)
                        {
                            this.camera.SetEnablePositionMovement(true);
                        }

                        break;
                    }
            }
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
