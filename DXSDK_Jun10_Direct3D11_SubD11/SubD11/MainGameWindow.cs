using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SubD11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        private SdkModelViewerCamera camera;

        private float g_fFieldOfView = 65.0f;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public int Subdivs
        {
            get
            {
                return this.mainGameComponent?.Subdivs ?? 2;
            }

            set
            {
                if (value != this.mainGameComponent.Subdivs)
                {
                    this.mainGameComponent.Subdivs = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public float DisplacementHeight
        {
            get
            {
                return this.mainGameComponent?.DisplacementHeight ?? 0.0f;
            }

            set
            {
                if (value != this.mainGameComponent.DisplacementHeight)
                {
                    this.mainGameComponent.DisplacementHeight = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool DrawWires
        {
            get
            {
                return this.mainGameComponent?.DrawWires ?? true;
            }

            set
            {
                if (value != this.mainGameComponent.DrawWires)
                {
                    this.mainGameComponent.DrawWires = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool UseMaterials
        {
            get
            {
                return this.mainGameComponent?.UseMaterials ?? true;
            }

            set
            {
                if (value != this.mainGameComponent.UseMaterials)
                {
                    this.mainGameComponent.UseMaterials = value;
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

            // Setup the camera's view parameters
            this.mainGameComponent.g_SubDMesh.GetBounds(out XMFloat3 vCenter, out XMFloat3 vExtents);
            XMFloat3 vEye;

            if (this.mainGameComponent.CloseupCamera)
            {
                float fRadius = XMVector3.Length(vExtents).X;
                vCenter.Y += fRadius * 0.63f;
                vEye = vCenter;
                vEye.Z -= fRadius * 0.3f;
                vEye.X += fRadius * 0.3f;
            }
            else
            {
                vEye = vCenter;
                float fRadius = XMVector3.Length(vExtents).X;
                float fTheta = XMMath.PI * 0.125f;
                float fDistance = fRadius / (float)Math.Tan(fTheta);
                vEye.Z -= fDistance;
            }

            this.camera.SetViewParams(vEye, vCenter);
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
            float fFOV = XMMath.ConvertToRadians(this.g_fFieldOfView);
            this.camera.SetProjParams(fFOV * 0.5f, fAspectRatio, 0.1f, 4000.0f);
            this.camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.camera.SetButtonMasks(SdkCameraMouseKeys.MiddleButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.LeftButton);
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
        }

        protected override void Render()
        {
            // WVP
            this.mainGameComponent.g_projectionMatrix = this.camera.GetProjMatrix();
            this.mainGameComponent.g_viewMatrix = this.camera.GetViewMatrix();
            this.mainGameComponent.g_eyePoint = this.camera.GetEyePt();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
