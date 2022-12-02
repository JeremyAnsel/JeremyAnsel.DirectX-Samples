using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace HDRToneMappingCS11
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

        public bool IsPostProcess
        {
            get
            {
                return this.mainGameComponent?.IsPostProcessRequested ?? this.mainGameComponent?.IsPostProcess ?? true;
            }

            set
            {
                if (value != this.mainGameComponent.IsPostProcess)
                {
                    this.mainGameComponent.IsPostProcessRequested = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public PostProcessMode PostProcessMode
        {
            get
            {
                return this.mainGameComponent?.PostProcessModeRequested ?? this.mainGameComponent?.PostProcessMode ?? PostProcessMode.ComputeShader;
            }

            set
            {
                if (value != this.mainGameComponent.PostProcessMode)
                {
                    this.mainGameComponent.PostProcessModeRequested = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsBloom
        {
            get
            {
                return this.mainGameComponent?.IsBloomRequested ?? this.mainGameComponent?.IsBloom ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsBloom)
                {
                    this.mainGameComponent.IsBloomRequested = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsFullScrBlur
        {
            get
            {
                return this.mainGameComponent?.IsFullScrBlurRequested ?? this.mainGameComponent?.IsFullScrBlur ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsFullScrBlur)
                {
                    this.mainGameComponent.IsFullScrBlurRequested = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsCPUReduction
        {
            get
            {
                return this.mainGameComponent?.IsCPUReductionRequested ?? this.mainGameComponent?.IsCPUReduction ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsCPUReduction)
                {
                    this.mainGameComponent.IsCPUReductionRequested = value;
                    this.NotifyPropertyChanged();
                }
            }
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

            // Setup the camera   
            XMVector vecEye = new(0.0f, -10.5f, -3.0f, 0.0f);
            XMVector vecAt = new(0.0f, 0.0f, 0.0f, 0.0f);
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
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 0.1f, 5000.0f);
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
    }
}
