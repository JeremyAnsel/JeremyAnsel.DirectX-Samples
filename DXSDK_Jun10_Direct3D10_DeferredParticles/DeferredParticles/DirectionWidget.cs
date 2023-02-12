using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace DeferredParticles
{
    class DirectionWidget
    {
        private XMMatrix m_mRot;

        private XMMatrix m_mRotSnapshot;

        private float m_fRadius;

        private SdkCameraMouseKeys m_nRotateMask;

        private readonly SdkArcBall m_ArcBall;

        private XMFloat3 m_vDefaultDir;

        private XMFloat3 m_vCurrentDir;

        private XMMatrix m_mView;

        public DirectionWidget()
        {
            m_fRadius = 1.0f;
            m_vDefaultDir = new XMFloat3(0, 1, 0);
            m_vCurrentDir = m_vDefaultDir;
            m_nRotateMask = SdkCameraMouseKeys.RightButton;

            m_mView = XMMatrix.Identity;
            m_mRot = XMMatrix.Identity;
            m_mRotSnapshot = XMMatrix.Identity;

            m_ArcBall = new SdkArcBall();
        }

        public XMFloat3 GetLightDirection()
        {
            return m_vCurrentDir;
        }

        public void SetLightDirection(XMFloat3 vDir)
        {
            m_vDefaultDir = vDir;
            m_vCurrentDir = vDir;
        }

        public void SetButtonMask(SdkCameraMouseKeys nRotate = SdkCameraMouseKeys.RightButton)
        {
            m_nRotateMask = nRotate;
        }

        public float GetRadius()
        {
            return m_fRadius;
        }

        public void SetRadius(float fRadius)
        {
            m_fRadius = fRadius;
        }

        public bool IsBeingDragged()
        {
            return m_ArcBall.IsBeingDragged();
        }

        public void HandleMessages(IntPtr hWnd, WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            // Current mouse position
            int iMouseX = (short)((ulong)lParam & 0xffffU);
            int iMouseY = (short)((ulong)lParam >> 16);

            switch (msg)
            {
                case WindowMessageType.LeftButtonDown:
                case WindowMessageType.MiddleButtonDown:
                case WindowMessageType.RightButtonDown:
                    {
                        if ((m_nRotateMask.HasFlag(SdkCameraMouseKeys.LeftButton) && msg == WindowMessageType.LeftButtonDown)
                            || (m_nRotateMask.HasFlag(SdkCameraMouseKeys.MiddleButton) && msg == WindowMessageType.MiddleButtonDown)
                            || (m_nRotateMask.HasFlag(SdkCameraMouseKeys.RightButton) && msg == WindowMessageType.RightButtonDown))
                        {
                            m_ArcBall.OnBegin(iMouseX, iMouseY);
                            NativeMethods.SetCapture(hWnd);
                        }

                        break;
                    }

                case WindowMessageType.MouseMove:
                    {
                        if (m_ArcBall.IsBeingDragged())
                        {
                            m_ArcBall.OnMove(iMouseX, iMouseY);
                            UpdateLightDir();
                        }

                        break;
                    }

                case WindowMessageType.LeftButtonUp:
                case WindowMessageType.MiddleButtonUp:
                case WindowMessageType.RightButtonUp:
                    {
                        if ((m_nRotateMask.HasFlag(SdkCameraMouseKeys.LeftButton) && msg == WindowMessageType.LeftButtonDown)
                            || (m_nRotateMask.HasFlag(SdkCameraMouseKeys.MiddleButton) && msg == WindowMessageType.MiddleButtonDown)
                            || (m_nRotateMask.HasFlag(SdkCameraMouseKeys.RightButton) && msg == WindowMessageType.RightButtonDown))
                        {
                            m_ArcBall.OnEnd();
                            NativeMethods.ReleaseCapture();
                        }

                        UpdateLightDir();
                        break;
                    }

                case WindowMessageType.CaptureChanged:
                    {
                        if (lParam != hWnd)
                        {
                            if (m_nRotateMask.HasFlag(SdkCameraMouseKeys.LeftButton)
                                || m_nRotateMask.HasFlag(SdkCameraMouseKeys.MiddleButton)
                                || m_nRotateMask.HasFlag(SdkCameraMouseKeys.RightButton))
                            {
                                m_ArcBall.OnEnd();
                                NativeMethods.ReleaseCapture();
                            }
                        }

                        break;
                    }
            }
        }

        private void UpdateLightDir()
        {
            XMMatrix mInvView = m_mView.Inverse();
            mInvView.M41 = 0;
            mInvView.M42 = 0;
            mInvView.M43 = 0;

            XMMatrix mLastRotInv = m_mRotSnapshot.Inverse();

            XMMatrix mRot = m_ArcBall.GetRotationMatrix();
            m_mRotSnapshot = mRot;

            // Accumulate the delta of the arcball's rotation in view space.
            // Note that per-frame delta rotations could be problematic over long periods of time.
            m_mRot *= m_mView * mLastRotInv * mRot * mInvView;

            // Since we're accumulating delta rotations, we need to orthonormalize 
            // the matrix to prevent eventual matrix skew
            XMVector pXBasis = XMVector.FromFloat(m_mRot.M11, m_mRot.M12, m_mRot.M13, 0);
            XMVector pYBasis = XMVector.FromFloat(m_mRot.M21, m_mRot.M22, m_mRot.M23, 0);
            XMVector pZBasis = XMVector.FromFloat(m_mRot.M31, m_mRot.M32, m_mRot.M33, 0);
            pXBasis = XMVector3.Normalize(pXBasis);
            pYBasis = XMVector3.Cross(pZBasis, pXBasis);
            pYBasis = XMVector3.Normalize(pYBasis);
            pZBasis = XMVector3.Cross(pXBasis, pYBasis);
            pXBasis.W = m_mRot.M14;
            pYBasis.W = m_mRot.M24;
            pZBasis.W = m_mRot.M34;
            XMVector pWBasis = XMVector.FromFloat(m_mRot.M41, m_mRot.M42, m_mRot.M43, m_mRot.M44);
            m_mRot = new XMMatrix(pXBasis, pYBasis, pZBasis, pWBasis);

            // Transform the default direction vector by the light's rotation matrix
            m_vCurrentDir = XMVector3.TransformNormal(m_vDefaultDir, m_mRot);
        }
    }
}
