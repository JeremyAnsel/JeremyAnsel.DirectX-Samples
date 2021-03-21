using JeremyAnsel.DirectX.DXMath;

namespace VarianceShadows11
{
    class MainGameSettings
    {
        public XMMatrix ViewerCameraView;

        public XMMatrix ViewerCameraProjection;

        public float ViewerCameraNearClip;

        public float ViewerCameraFarClip;

        public XMMatrix LightCameraWorld;

        public XMMatrix LightCameraView;

        public XMMatrix LightCameraProjection;

        public XMVector LightCameraEyePoint;

        public XMVector LightCameraLookAtPoint;

        public XMMatrix ActiveCameraView;

        public XMMatrix ActiveCameraProjection;

        public float MeshLength;

        public MainGameSettings()
        {
            this.Init();
        }

        public void Init()
        {
            {
                XMFloat3 vecEye = new XMFloat3(100.0f, 5.0f, 5.0f);
                XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);
                XMFloat3 vecUp = new XMFloat3(0.0f, 1.0f, 0.0f);

                this.ViewerCameraView = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
                this.ViewerCameraProjection = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, 1.0f, 0.05f, 1.0f);
                this.ViewerCameraNearClip = 0.05f;
                this.ViewerCameraFarClip = 1.0f;
            }

            {
                XMFloat3 vecEye = new XMFloat3(-320.0f, 300.0f, -220.3f);
                XMFloat3 vecAt = new XMFloat3(0.0f, 0.0f, 0.0f);
                XMFloat3 vecUp = new XMFloat3(0.0f, 1.0f, 0.0f);

                this.LightCameraWorld = XMMatrix.Identity;
                this.LightCameraView = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
                this.LightCameraProjection = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, 1.0f, 0.1f, 1000.0f);
                this.LightCameraEyePoint = vecEye;
                this.LightCameraLookAtPoint = vecAt;
            }

            this.ActiveCameraView = this.ViewerCameraView;
            this.ActiveCameraProjection = this.ViewerCameraProjection;

            this.MeshLength = 1.0f;
        }
    }
}
