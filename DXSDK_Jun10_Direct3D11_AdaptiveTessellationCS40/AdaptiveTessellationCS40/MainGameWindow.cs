using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdaptiveTessellationCS40
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private bool showTessellated = true;

        private PartitioningMode partitioningMode = PartitioningMode.FractionalEven;

        private SdkFirstPersonCamera camera;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public bool ShowTessellated
        {
            get
            {
                return this.showTessellated;
            }

            set
            {
                if (value != this.showTessellated)
                {
                    this.showTessellated = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public PartitioningMode PartitioningMode
        {
            get
            {
                return this.partitioningMode;
            }

            set
            {
                if (value != this.partitioningMode)
                {
                    this.partitioningMode = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            this.camera = new SdkFirstPersonCamera();

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            // Setup the camera's view parameters
            XMFloat3 vecEye = new XMFloat3(0.0f, 0.0f, -300.0f);
            XMFloat3 vecAt = new XMFloat3(10.0f, 20.0f, 0.0f);
            this.camera.SetViewParams(vecEye, vecAt);
            this.camera.SetScalers(0.005f, 50.0f);
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
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 1.0f, 500000.0f);
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
            this.mainGameComponent.ShowTessellated = this.ShowTessellated;
            this.mainGameComponent.PartitioningMode = this.PartitioningMode;

            // Get the projection & view matrix from the camera class
            XMMatrix mWorld = XMMatrix.Identity;
            XMMatrix mView = this.camera.GetViewMatrix();
            XMMatrix mProj = this.camera.GetProjMatrix();
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;
            this.mainGameComponent.WorldViewProjectionMatrix = mWorldViewProjection;

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
        }
    }
}
