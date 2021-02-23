using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CascadedShadowMaps11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private SdkFirstPersonCamera viewerCamera;
        private SdkFirstPersonCamera lightCamera;
        private SdkFirstPersonCamera activeCamera;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public bool VisualizeCascades
        {
            get
            {
                return this.mainGameComponent?.VisualizeCascades ?? false;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.VisualizeCascades = value;
                this.NotifyPropertyChanged();
            }
        }

        public ShadowTextureFormat ShadowBufferFormat
        {
            get
            {
                return this.mainGameComponent?.ShadowBufferFormat ?? ShadowTextureFormat.R32;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.ShadowBufferFormat = value;
                this.NotifyPropertyChanged();
            }
        }

        public int BufferSize
        {
            get
            {
                return this.mainGameComponent?.BufferSize ?? 1024;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                int max = 8192 / this.CascadeLevels;

                if (value > max)
                {
                    value = max;
                }

                this.mainGameComponent.BufferSize = value;
                this.NotifyPropertyChanged();
            }
        }

        public int CascadeLevels
        {
            get
            {
                return this.mainGameComponent?.CascadeLevels ?? 3;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.CascadeLevels = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(SelectedCascadeLevels));

                int bufferSize = this.BufferSize;
                this.BufferSize = bufferSize;

                if ((int)this.SelectedCamera - 2 >= value - 1)
                {
                    this.SelectedCamera = (CameraSelection)(value + 1);
                }
            }
        }

        public int SelectedCascadeLevels
        {
            get
            {
                return this.CascadeLevels - 1;
            }

            set
            {
                this.CascadeLevels = value + 1;
            }
        }

        public int PCFBlurSize
        {
            get
            {
                return this.mainGameComponent?.PCFBlurSize ?? 3;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.PCFBlurSize = value;
                this.NotifyPropertyChanged();
            }
        }

        public float PCFOffset
        {
            get
            {
                return this.mainGameComponent?.PCFOffset ?? 0.002f;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.PCFOffset = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool BlurBetweenCascades
        {
            get
            {
                return this.mainGameComponent?.BlurBetweenCascades ?? false;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.BlurBetweenCascades = value;
                this.NotifyPropertyChanged();
            }
        }

        public float BlurBetweenCascadesAmount
        {
            get
            {
                return this.mainGameComponent?.BlurBetweenCascadesAmount ?? 0.005f;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.BlurBetweenCascadesAmount = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool DerivativeBasedOffset
        {
            get
            {
                return this.mainGameComponent?.DerivativeBasedOffset ?? false;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.DerivativeBasedOffset = value;
                this.NotifyPropertyChanged();
            }
        }

        public SceneSelection SceneSelection
        {
            get
            {
                return this.mainGameComponent?.SceneSelection ?? SceneSelection.PowerPlantScene;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.SceneSelection = value;
                this.NotifyPropertyChanged();
                this.SelectedCamera = CameraSelection.EyeCamera;
            }
        }

        public CameraSelection SelectedCamera
        {
            get
            {
                return this.mainGameComponent?.SelectedCamera ?? CameraSelection.EyeCamera;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.InitCamera();
                this.mainGameComponent.SelectedCamera = value;
                this.NotifyPropertyChanged();

                if (value == CameraSelection.EyeCamera)
                {
                    this.activeCamera = this.viewerCamera;
                }
                else
                {
                    this.activeCamera = this.lightCamera;
                }
            }
        }

        public bool MoveLightTexelSize
        {
            get
            {
                return this.mainGameComponent?.MoveLightTexelSize ?? true;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.MoveLightTexelSize = value;
                this.NotifyPropertyChanged();
            }
        }

        public FitProjection SelectedProjectionFit
        {
            get
            {
                return this.mainGameComponent?.SelectedProjectionFit ?? FitProjection.ToScene;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.SelectedProjectionFit = value;
                this.NotifyPropertyChanged();
            }
        }

        public FitNearFar SelectedNearFarFit
        {
            get
            {
                return this.mainGameComponent?.SelectedNearFarFit ?? FitNearFar.SceneAABB;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.SelectedNearFarFit = value;
                this.NotifyPropertyChanged();

                if (value == FitNearFar.Pancaking)
                {
                    this.SelectedCascadeSelection = CascadeSelection.Interval;
                }
            }
        }

        private int iSaveLastCascadeValue = 100;

        public CascadeSelection SelectedCascadeSelection
        {
            get
            {
                return this.mainGameComponent?.SelectedCascadeSelection ?? CascadeSelection.Map;
            }

            set
            {
                if (this.mainGameComponent == null)
                {
                    return;
                }

                this.mainGameComponent.SelectedCascadeSelection = value;
                this.NotifyPropertyChanged();

                if (value == CascadeSelection.Map)
                {
                    if (this.SelectedNearFarFit == FitNearFar.Pancaking)
                    {
                        this.SelectedNearFarFit = FitNearFar.SceneAABB;
                    }

                    this.CascadePartitionsZeroToOne[this.CascadeLevels - 1] = this.iSaveLastCascadeValue;
                }
                else
                {
                    this.iSaveLastCascadeValue = this.CascadePartitionsZeroToOne[this.CascadeLevels - 1];
                    this.CascadePartitionsZeroToOne[this.CascadeLevels - 1] = 100;
                }
            }
        }

        private ObservableCollection<int> cascadePartitionsZeroToOne;

        public ObservableCollection<int> CascadePartitionsZeroToOne
        {
            get
            {
                if (this.cascadePartitionsZeroToOne == null && this.mainGameComponent != null)
                {
                    var collection = new ObservableCollection<int>();

                    for (int i = 0; i < this.mainGameComponent.CascadePartitionsZeroToOne.Length; i++)
                    {
                        collection.Add(this.mainGameComponent.CascadePartitionsZeroToOne[i]);
                    }

                    collection.CollectionChanged += (sender, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Replace)
                        {
                            int index = e.NewStartingIndex;
                            int move = (int)e.NewItems[0];

                            for (int i = 0; i < index; i++)
                            {
                                int val = this.cascadePartitionsZeroToOne[i];

                                if (move < val)
                                {
                                    this.cascadePartitionsZeroToOne[i] = move;
                                }
                            }

                            for (int i = index; i < 8; i++)
                            {
                                int val = this.cascadePartitionsZeroToOne[i];

                                if (move > val)
                                {
                                    this.cascadePartitionsZeroToOne[i] = move;
                                }
                            }

                            collection.CopyTo(this.mainGameComponent.CascadePartitionsZeroToOne, 0);
                        }
                    };

                    this.cascadePartitionsZeroToOne = collection;
                }

                return this.cascadePartitionsZeroToOne;
            }
        }

        private void InitCamera()
        {
            XMFloat3 vMin = new XMFloat3(-1000.0f, -1000.0f, -1000.0f);
            XMFloat3 vMax = new XMFloat3(1000.0f, 1000.0f, 1000.0f);

            {
                XMFloat3 vecEye = new XMFloat3(100.0f, 5.0f, 5.0f);
                XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);

                this.viewerCamera.SetViewParams(vecEye, vecAt);
                this.viewerCamera.SetRotateButtons(true, false, false);
                this.viewerCamera.SetScalers(0.01f, 10.0f);
                this.viewerCamera.SetDrag(true);
                this.viewerCamera.SetEnableYAxisMovement(true);
                this.viewerCamera.SetClipToBoundary(true, vMin, vMax);
                this.viewerCamera.FrameMove(0.0);
            }

            {
                XMFloat3 vecEye = new XMFloat3(-320.0f, 300.0f, -220.3f);
                XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);

                this.lightCamera.SetViewParams(vecEye, vecAt);
                this.lightCamera.SetRotateButtons(true, false, false);
                this.lightCamera.SetScalers(0.01f, 50.0f);
                this.lightCamera.SetDrag(true);
                this.lightCamera.SetEnableYAxisMovement(true);
                this.lightCamera.SetClipToBoundary(true, vMin, vMax);
                this.lightCamera.SetProjParams(XMMath.PIDivFour, 1.0f, 0.1f, 1000.0f);
                this.lightCamera.FrameMove(0.0);
            }
        }

        protected override void Init()
        {
            this.viewerCamera = new SdkFirstPersonCamera();
            this.lightCamera = new SdkFirstPersonCamera();

            this.activeCamera = this.viewerCamera;

            this.InitCamera();

            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);
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

            float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / this.DeviceResources.BackBufferHeight;
            this.viewerCamera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 0.05f, this.mainGameComponent.Settings.MeshLength);

            this.NotifyPropertyChanged(string.Empty);
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            // Update the camera's position based on user input 
            this.lightCamera.FrameMove(this.Timer.ElapsedSeconds);
            this.viewerCamera.FrameMove(this.Timer.ElapsedSeconds);

            this.mainGameComponent.Update(this.Timer);
        }

        protected override void Render()
        {
            var settings = this.mainGameComponent.Settings;

            settings.ViewerCameraView = this.viewerCamera.GetViewMatrix();
            settings.ViewerCameraProjection = this.viewerCamera.GetProjMatrix();
            settings.ViewerCameraNearClip = this.viewerCamera.GetNearClip();
            settings.ViewerCameraFarClip = this.viewerCamera.GetFarClip();
            settings.LightCameraWorld = this.lightCamera.GetWorldMatrix();
            settings.LightCameraView = this.lightCamera.GetViewMatrix();
            settings.LightCameraProjection = this.lightCamera.GetProjMatrix();
            settings.LightCameraEyePoint = this.lightCamera.GetEyePt();
            settings.LightCameraLookAtPoint = this.lightCamera.GetLookAtPt();
            settings.ActiveCameraView = this.activeCamera.GetViewMatrix();
            settings.ActiveCameraProjection = this.activeCamera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            this.activeCamera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
