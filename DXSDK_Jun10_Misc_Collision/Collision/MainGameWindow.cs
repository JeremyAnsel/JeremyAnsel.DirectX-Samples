using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace Collision
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

        public CollisionGroup CollisionGroup
        {
            get
            {
                return this.mainGameComponent?.CollisionGroup ?? CollisionGroup.Frustum;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.CollisionGroup = value;
                this.NotifyPropertyChanged();

                this.SetViewForGroup();
            }
        }

        private void SetViewForGroup()
        {
            this.camera.Reset();

            XMVector vecAt = this.mainGameComponent.cameraOrigins[(int)this.CollisionGroup];
            XMVector vecEye = new(vecAt.X, vecAt.Y + 20.0f, (this.CollisionGroup == CollisionGroup.Frustum) ? (vecAt.Z + 20.0f) : (vecAt.Z - 20.0f), 0.0f);

            this.camera.SetViewParams(vecEye, vecAt);
            this.camera.SetModelCenter(vecAt);
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

            this.SetViewForGroup();
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
            this.mainGameComponent.worldMatrix = this.camera.GetWorldMatrix();
            this.mainGameComponent.viewMatrix = this.camera.GetViewMatrix();
            this.mainGameComponent.projectionMatrix = this.camera.GetProjMatrix();

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
                    case VirtualKey.NumPad1:
                    case VirtualKey.NumPad2:
                    case VirtualKey.NumPad3:
                    case VirtualKey.NumPad4:
                        this.CollisionGroup = (CollisionGroup)(key - VirtualKey.NumPad1);
                        break;

                    case VirtualKey.D1:
                    case VirtualKey.D2:
                    case VirtualKey.D3:
                    case VirtualKey.D4:
                        this.CollisionGroup = (CollisionGroup)(key - VirtualKey.D1);
                        break;
                }
            }
        }
    }
}
