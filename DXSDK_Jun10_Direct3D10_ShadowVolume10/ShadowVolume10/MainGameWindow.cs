using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace ShadowVolume10
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        // A model viewing camera
        private SdkFirstPersonCamera g_Camera;
        // Camera for mesh control
        private SdkModelViewerCamera g_MeshCamera;
        // Camera for easy light movement control
        private SdkModelViewerCamera g_LightCamera;

        private bool g_bLeftButtonDown = false;
        private bool g_bRightButtonDown = false;
        private bool g_bMiddleButtonDown = false;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            this.g_Camera = new SdkFirstPersonCamera();
            this.g_MeshCamera = new SdkModelViewerCamera();
            this.g_LightCamera = new SdkModelViewerCamera();

            this.g_Camera.SetRotateButtons(true, false, false);
            this.g_MeshCamera.SetButtonMasks(SdkCameraMouseKeys.RightButton, 0, 0);
            this.g_LightCamera.SetButtonMasks(SdkCameraMouseKeys.MiddleButton, 0, 0);

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            // Setup the camera's view parameters
            XMVector vecEye = new(0.0f, 1.0f, -5.0f, 0.0f);
            XMVector vecAt = new(0.0f, 0.0f, -0.0f, 0.0f);
            this.g_Camera.SetViewParams(vecEye, vecAt);
            this.g_MeshCamera.SetViewParams(vecEye, vecAt);
            this.g_LightCamera.SetViewParams(vecEye, vecAt);
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
            this.g_Camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 0.1f, 500.0f);
            this.g_MeshCamera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.g_LightCamera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
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

            this.g_Camera.FrameMove(this.Timer.ElapsedSeconds);
            this.g_MeshCamera.FrameMove(this.Timer.ElapsedSeconds);
            this.g_LightCamera.FrameMove(this.Timer.ElapsedSeconds);
        }

        protected override void Render()
        {
            this.mainGameComponent.LightTransform = this.g_LightCamera.GetWorldMatrix();
            this.mainGameComponent.WorldTransform = this.g_MeshCamera.GetWorldMatrix();
            this.mainGameComponent.ViewTransform = this.g_Camera.GetViewMatrix();
            this.mainGameComponent.ProjectionTransform = this.g_Camera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            // Pass all remaining windows messages to camera so it can respond to user input
            this.g_Camera?.HandleMessages(this.Handle, msg, wParam, lParam);
            this.g_MeshCamera?.HandleMessages(this.Handle, msg, wParam, lParam);
            this.g_LightCamera?.HandleMessages(this.Handle, msg, wParam, lParam);

            switch (msg)
            {
                case WindowMessageType.LeftButtonDown:
                case WindowMessageType.RightButtonDown:
                case WindowMessageType.MiddleButtonDown:
                case WindowMessageType.XButtonDown:
                case WindowMessageType.MouseWheel:
                    {
                        int mouseX = (short)((ulong)lParam & 0xffffU);
                        int mouseY = (short)((ulong)lParam >> 16);
                        ushort keys = (ushort)((ulong)wParam & 0xffffU);
                        bool isLeftKey = (keys & 0x0001) != 0;
                        bool isRightKey = (keys & 0x0002) != 0;
                        bool isMiddleKey = (keys & 0x0010) != 0;
                        bool isX1Key = (keys & 0x0020) != 0;
                        bool isX2Key = (keys & 0x0040) != 0;
                        int wheelDelta = msg == WindowMessageType.MouseWheel ? (short)((ulong)wParam >> 16) : 0;

                        this.MouseProc(isLeftKey, isRightKey, isMiddleKey, isX1Key, isX2Key, wheelDelta, mouseX, mouseY);
                        break;
                    }
            }
        }

        private void MouseProc(
            bool bLeftButtonDown,
            bool bRightButtonDown,
            bool bMiddleButtonDown,
            bool bSideButton1Down,
            bool bSideButton2Down,
            int nMouseWheelDelta,
            int xPos,
            int yPos)
        {
            bool bOldLeftButtonDown = this.g_bLeftButtonDown;
            bool bOldRightButtonDown = this.g_bRightButtonDown;
            bool bOldMiddleButtonDown = this.g_bMiddleButtonDown;
            this.g_bLeftButtonDown = bLeftButtonDown;
            this.g_bMiddleButtonDown = bMiddleButtonDown;
            this.g_bRightButtonDown = bRightButtonDown;

            if (bOldLeftButtonDown && !this.g_bLeftButtonDown)
            {
                this.g_Camera?.SetEnablePositionMovement(false);
            }
            else if (!bOldLeftButtonDown && this.g_bLeftButtonDown)
            {
                this.g_Camera?.SetEnablePositionMovement(true);
            }

            if (bOldRightButtonDown && !this.g_bRightButtonDown)
            {
                this.g_MeshCamera?.SetEnablePositionMovement(false);
            }
            else if (!bOldRightButtonDown && this.g_bRightButtonDown)
            {
                this.g_MeshCamera?.SetEnablePositionMovement(true);
                this.g_Camera?.SetEnablePositionMovement(false);
            }

            if (bOldMiddleButtonDown && !this.g_bMiddleButtonDown)
            {
                this.g_LightCamera?.SetEnablePositionMovement(false);
            }
            else if (!bOldMiddleButtonDown && this.g_bMiddleButtonDown)
            {
                this.g_LightCamera?.SetEnablePositionMovement(true);
                this.g_Camera?.SetEnablePositionMovement(false);
            }

            // If no mouse button is down at all, enable camera movement.
            if (!this.g_bLeftButtonDown && !this.g_bRightButtonDown && !this.g_bMiddleButtonDown)
            {
                this.g_Camera?.SetEnablePositionMovement(true);
            }
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown)
            {
                switch (key)
                {
                    case VirtualKey.Space:
                        {
                            int value = this.mainGameComponent.CurrentBackground;
                            value++;

                            if (value >= MainGameComponent.MaxNumBackgrounds)
                            {
                                value = 0;
                            }

                            this.mainGameComponent.CurrentBackground = value;
                            break;
                        }

                    case VirtualKey.V:
                        this.mainGameComponent.RenderType =
                            this.mainGameComponent.RenderType == RenderType.Scene
                            ? RenderType.ShadowVolume
                            : RenderType.Scene;
                        break;

                    case VirtualKey.D1:
                    case VirtualKey.D2:
                    case VirtualKey.D3:
                    case VirtualKey.D4:
                    case VirtualKey.D5:
                    case VirtualKey.D6:
                        this.mainGameComponent.NumLights = 1 + (int)key - (int)VirtualKey.D1;
                        break;
                }
            }
        }
    }
}
