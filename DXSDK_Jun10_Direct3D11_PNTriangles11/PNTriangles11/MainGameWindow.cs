using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNTriangles11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private static readonly int MeshTypeMax = Enum.GetValues<MeshType>().Length;

        // A model viewing camera for each mesh scene
        private readonly SdkModelViewerCamera[] g_Camera = new SdkModelViewerCamera[MeshTypeMax];

        // A model viewing camera for the light
        private SdkModelViewerCamera g_LightCamera;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public MeshType MeshType
        {
            get
            {
                return this.mainGameComponent?.MeshType ?? MeshType.Tiny;
            }

            set
            {
                if (value != this.mainGameComponent.MeshType)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.MeshType = value;
                    this.NotifyPropertyChanged();

                    if (value == MeshType.Teapot)
                    {
                        this.IsTextured = false;
                    }
                    else
                    {
                        this.IsTextured = true;
                    }
                }
            }
        }

        public bool IsWireframe
        {
            get
            {
                return this.mainGameComponent?.IsWireframe ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsWireframe)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsWireframe = value;
                    this.NotifyPropertyChanged();
                }
            }
        }


        public bool IsTextured
        {
            get
            {
                return this.mainGameComponent?.IsTextured ?? true;
            }

            set
            {
                if (value != this.mainGameComponent.IsTextured)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsTextured = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsTessellation
        {
            get
            {
                return this.mainGameComponent?.IsTessellation ?? true;
            }

            set
            {
                if (value != this.mainGameComponent.IsTessellation)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsTessellation = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int TessFactor
        {
            get
            {
                return this.mainGameComponent?.TessFactor ?? 5;
            }

            set
            {
                if (value != this.mainGameComponent.TessFactor)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.TessFactor = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsBackFaceCull
        {
            get
            {
                return this.mainGameComponent?.IsBackFaceCull ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsBackFaceCull)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsBackFaceCull = value;
                    this.NotifyPropertyChanged();
                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public float BackFaceCullEpsilon
        {
            get
            {
                return this.mainGameComponent?.BackFaceCullEpsilon ?? 0.05f;
            }

            set
            {
                if (value != this.mainGameComponent.BackFaceCullEpsilon)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.BackFaceCullEpsilon = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsViewFrustumCull
        {
            get
            {
                return this.mainGameComponent?.IsViewFrustumCull ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsViewFrustumCull)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsViewFrustumCull = value;
                    this.NotifyPropertyChanged();
                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public float ViewFrustumCullEpsilon
        {
            get
            {
                return this.mainGameComponent?.ViewFrustumCullEpsilon ?? 0.5f;
            }

            set
            {
                if (value != this.mainGameComponent.ViewFrustumCullEpsilon)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.ViewFrustumCullEpsilon = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsScreenSpaceAdaptive
        {
            get
            {
                return this.mainGameComponent?.IsScreenSpaceAdaptive ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsScreenSpaceAdaptive)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsScreenSpaceAdaptive = value;
                    this.NotifyPropertyChanged();

                    if (value)
                    {
                        this.IsDistanceAdaptive = false;
                        this.IsScreenResolutionAdaptive = false;
                    }

                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public int EdgeSize
        {
            get
            {
                return this.mainGameComponent?.EdgeSize ?? 16;
            }

            set
            {
                if (value != this.mainGameComponent.EdgeSize)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.EdgeSize = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsDistanceAdaptive
        {
            get
            {
                return this.mainGameComponent?.IsDistanceAdaptive ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsDistanceAdaptive)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsDistanceAdaptive = value;
                    this.NotifyPropertyChanged();

                    if (value)
                    {
                        this.IsScreenSpaceAdaptive = false;
                        this.IsScreenResolutionAdaptive = false;
                    }

                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public float RangeScale
        {
            get
            {
                return this.mainGameComponent?.RangeScale ?? 1.0f;
            }

            set
            {
                if (value != this.mainGameComponent.RangeScale)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.RangeScale = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsScreenResolutionAdaptive
        {
            get
            {
                return this.mainGameComponent?.IsScreenResolutionAdaptive ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsScreenResolutionAdaptive)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsScreenResolutionAdaptive = value;
                    this.NotifyPropertyChanged();

                    if (value)
                    {
                        this.IsScreenSpaceAdaptive = false;
                        this.IsDistanceAdaptive = false;
                    }

                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public float ResolutionScale
        {
            get
            {
                return this.mainGameComponent?.ResolutionScale ?? 1.0f;
            }

            set
            {
                if (value != this.mainGameComponent.ResolutionScale)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.ResolutionScale = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsOrientationAdaptive
        {
            get
            {
                return this.mainGameComponent?.IsOrientationAdaptive ?? false;
            }

            set
            {
                if (value != this.mainGameComponent.IsOrientationAdaptive)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.IsOrientationAdaptive = value;
                    this.NotifyPropertyChanged();
                    this.mainGameComponent.RecreateHullShader = true;
                }
            }
        }

        public float SilhoutteEpsilon
        {
            get
            {
                return this.mainGameComponent?.SilhoutteEpsilon ?? 0.25f;
            }

            set
            {
                if (value != this.mainGameComponent.SilhoutteEpsilon)
                {
                    this.mainGameComponent.SettingsChanged = true;
                    this.mainGameComponent.SilhoutteEpsilon = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            // Setup the camera for each scene
            XMFloat3 vecAt = new(0.0f, 0.0f, 0.0f);
            XMFloat3 vecEye;

            // Tiny
            vecEye = new(0.0f, 0.0f, -700.0f);
            this.g_Camera[(int)MeshType.Tiny] = new();
            this.g_Camera[(int)MeshType.Tiny].SetViewParams(vecEye, vecAt);

            // Tiger
            vecEye = new(0.0f, 0.0f, -4.0f);
            this.g_Camera[(int)MeshType.Tiger] = new();
            this.g_Camera[(int)MeshType.Tiger].SetViewParams(vecEye, vecAt);

            // Teapot
            vecEye = new(0.0f, 0.0f, -4.0f);
            this.g_Camera[(int)MeshType.Teapot] = new();
            this.g_Camera[(int)MeshType.Teapot].SetViewParams(vecEye, vecAt);

            // Setup the light camera
            vecEye = new(0.0f, -1.0f, -1.0f);
            this.g_LightCamera = new();
            this.g_LightCamera.SetViewParams(vecEye, vecAt);

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

            float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / (float)this.DeviceResources.BackBufferHeight;

            // Setup the camera's projection parameters    
            for (int iMeshType = 0; iMeshType < this.g_Camera.Length; iMeshType++)
            {
                this.g_Camera[iMeshType].SetProjParams(XMMath.PIDivFour, fAspectRatio, 1.0f, 5000.0f);
                this.g_Camera[iMeshType].SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
                this.g_Camera[iMeshType].SetButtonMasks(SdkCameraMouseKeys.MiddleButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.LeftButton);
            }

            // Setup the light camera's projection params
            this.g_LightCamera.SetProjParams(XMMath.PIDivFour, fAspectRatio, 1.0f, 5000.0f);
            this.g_LightCamera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.g_LightCamera.SetButtonMasks(SdkCameraMouseKeys.RightButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.RightButton);
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

            this.g_Camera[(int)this.MeshType]?.FrameMove(this.Timer.ElapsedSeconds);
            this.g_LightCamera.FrameMove(this.Timer.ElapsedSeconds);
        }

        protected override void Render()
        {
            this.mainGameComponent.LightCameraEye = this.g_LightCamera.GetEyePt();
            this.mainGameComponent.LightCameraAt = this.g_LightCamera.GetLookAtPt();

            for (int i = 0; i < this.g_Camera.Length; i++)
            {
                if (this.g_Camera[i] is null)
                {
                    continue;
                }

                this.mainGameComponent.CameraEye[i] = this.g_Camera[i].GetEyePt();
                this.mainGameComponent.CameraAt[i] = this.g_Camera[i].GetLookAtPt();
                this.mainGameComponent.WorldMatrix[i] = this.g_Camera[i].GetWorldMatrix();
                this.mainGameComponent.ViewMatrix[i] = this.g_Camera[i].GetViewMatrix();
                this.mainGameComponent.ProjectionMatrix[i] = this.g_Camera[i].GetProjMatrix();
            }

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            this.g_Camera[(int)this.MeshType]?.HandleMessages(this.Handle, msg, wParam, lParam);
            this.g_LightCamera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
