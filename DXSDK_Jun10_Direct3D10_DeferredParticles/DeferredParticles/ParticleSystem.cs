using JeremyAnsel.DirectX.DXMath;
using System;

namespace DeferredParticles
{
    abstract class ParticleSystem
    {
        public delegate void NewExplosionDelegate(in XMFloat3 vCenter, float fSize);
        public NewExplosionDelegate NewExplosion;

        protected int m_NumParticles;
        protected Particle[] m_pParticles;
        protected int m_pParticlesStartIndex;

        protected float m_fSpread;
        protected float m_fLifeSpan;
        protected float m_fStartSize;
        protected float m_fEndSize;
        protected float m_fSizeExponent;
        protected float m_fStartSpeed;
        protected float m_fEndSpeed;
        protected float m_fSpeedExponent;
        protected float m_fFadeExponent;
        protected float m_fRollAmount;
        protected float m_fWindFalloff;

        protected uint m_NumStreamers;
        protected float m_fSpeedVariance;
        protected XMFloat3 m_vDirection;
        protected XMFloat3 m_vDirVariance;

        protected XMVector m_vColor0;
        protected XMVector m_vColor1;

        protected XMFloat3 m_vPosMul;
        protected XMFloat3 m_vDirMul;

        protected float m_fCurrentTime;
        protected XMFloat3 m_vCenter;
        protected float m_fStartTime;

        protected XMVector m_vFlashColor;

        protected bool m_bStarted;

        protected ParticleSystemType m_PST;

        public ParticleSystem()
        {
            m_NumParticles = 0;
            m_pParticles = null;
            m_pParticlesStartIndex = 0;

            m_fSpread = 0.0f;
            m_fLifeSpan = 0.0f;
            m_fStartSize = 0.0f;
            m_fEndSize = 0.0f;
            m_fSizeExponent = 0.0f;
            m_fStartSpeed = 0.0f;
            m_fEndSpeed = 0.0f;
            m_fSpeedExponent = 0.0f;
            m_fFadeExponent = 0.0f;
            m_fRollAmount = 0.0f;
            m_fWindFalloff = 0.0f;
            m_vPosMul = new XMFloat3(0, 0, 0);
            m_vDirMul = new XMFloat3(0, 0, 0);

            m_fCurrentTime = 0.0f;
            m_vCenter = new XMFloat3(0, 0, 0);
            m_fStartTime = 0.0f;

            m_vFlashColor = new XMVector(0, 0, 0, 0);

            m_PST = ParticleSystemType.Default;
        }

        public void CreateParticleSystem(int NumParticles)
        {
            m_NumParticles = NumParticles;
            Particles.ReserveParticleArray(NumParticles, out m_pParticles, out m_pParticlesStartIndex);
        }

        public void SetSystemAttributes(
            in XMFloat3 vCenter,
            float fSpread, float fLifeSpan, float fFadeExponent,
            float fStartSize, float fEndSize, float fSizeExponent,
            float fStartSpeed, float fEndSpeed, float fSpeedExponent,
            float fRollAmount, float fWindFalloff,
            uint NumStreamers, float fSpeedVariance,
            in XMFloat3 vDirection, in XMFloat3 vDirVariance,
            in XMVector vColor0, in XMVector vColor1,
            in XMFloat3 vPosMul, in XMFloat3 vDirMul)
        {
            m_vCenter = vCenter;
            m_fSpread = fSpread;
            m_fLifeSpan = fLifeSpan;
            m_fStartSize = fStartSize;
            m_fEndSize = fEndSize;
            m_fSizeExponent = fSizeExponent;
            m_fStartSpeed = fStartSpeed;
            m_fEndSpeed = fEndSpeed;
            m_fSpeedExponent = fSpeedExponent;
            m_fFadeExponent = fFadeExponent;

            m_fRollAmount = fRollAmount;
            m_fWindFalloff = fWindFalloff;
            m_vPosMul = vPosMul;
            m_vDirMul = vDirMul;

            m_NumStreamers = NumStreamers;
            m_fSpeedVariance = fSpeedVariance;
            m_vDirection = vDirection;
            m_vDirVariance = vDirVariance;

            m_vColor0 = vColor0;
            m_vColor1 = vColor1;

            Init();
        }

        public void SetCenter(in XMFloat3 vCenter)
        {
            m_vCenter = vCenter;
        }

        public void SetStartTime(float fStartTime)
        {
            m_fStartTime = fStartTime;
        }

        public void SetStartSpeed(float fStartSpeed)
        {
            m_fStartSpeed = fStartSpeed;
        }

        public void SetFlashColor(in XMVector vFlashColor)
        {
            m_vFlashColor = vFlashColor;
        }

        public XMVector GetFlashColor()
        {
            return m_vFlashColor;
        }

        public float GetCurrentTime()
        {
            return m_fCurrentTime;
        }

        public float GetLifeSpan()
        {
            return m_fLifeSpan;
        }

        public int GetNumParticles()
        {
            return m_NumParticles;
        }

        public XMFloat3 GetCenter()
        {
            return m_vCenter;
        }

        public virtual void Init()
        {
            for (int i = m_pParticlesStartIndex; i < m_pParticlesStartIndex + m_NumParticles; i++)
            {
                m_pParticles[i].vPos.X = Particles.RPercent() * m_fSpread;
                m_pParticles[i].vPos.Y = Particles.RPercent() * m_fSpread;
                m_pParticles[i].vPos.Z = Particles.RPercent() * m_fSpread;
                m_pParticles[i].vPos.X *= m_vPosMul.X;
                m_pParticles[i].vPos.Y *= m_vPosMul.Y;
                m_pParticles[i].vPos.Z *= m_vPosMul.Z;
                m_pParticles[i].vPos = m_pParticles[i].vPos.ToVector() + m_vCenter;

                m_pParticles[i].vDir.X = Particles.RPercent();
                m_pParticles[i].vDir.Y = Math.Abs(Particles.RPercent());
                m_pParticles[i].vDir.Z = Particles.RPercent();
                m_pParticles[i].vDir.X *= m_vDirMul.X;
                m_pParticles[i].vDir.Y *= m_vDirMul.Y;
                m_pParticles[i].vDir.Z *= m_vDirMul.Z;

                m_pParticles[i].vDir = XMVector3.Normalize(m_pParticles[i].vDir);
                m_pParticles[i].Radius = m_fStartSize;
                m_pParticles[i].Life = m_fStartTime;
                m_pParticles[i].Fade = 0.0f;

                m_pParticles[i].Rot = Particles.RPercent() * 3.14159f * 2.0f;

                float fLerp = Particles.RPercent();
                XMVector vColor = m_vColor0 * fLerp + m_vColor1 * (1.0f - fLerp);

                m_pParticles[i].Color = (uint)(vColor.W * 255.0f) << 24;
                m_pParticles[i].Color |= ((uint)(vColor.Z * 255.0f) & 255) << 16;
                m_pParticles[i].Color |= ((uint)(vColor.Y * 255.0f) & 255) << 8;
                m_pParticles[i].Color |= ((uint)(vColor.X * 255.0f) & 255);
            }

            m_bStarted = false;
            m_fCurrentTime = m_fStartTime;
        }

        public virtual void AdvanceSystem(
            double fTime,
            double fElapsedTime,
            in XMFloat3 vRight,
            in XMFloat3 vUp,
            in XMFloat3 vWindVel,
            in XMFloat3 vGravity)
        {
            if (m_fCurrentTime > 0)
            {
                for (int i = m_pParticlesStartIndex; i < m_pParticlesStartIndex + m_NumParticles; i++)
                {
                    float t = m_pParticles[i].Life / m_fLifeSpan;
                    float tm1 = t - 1.0f;
                    float fSizeLerp = 1.0f - (float)Math.Pow(tm1, m_fSizeExponent);
                    float fSpeedLerp = 1.0f - (float)Math.Pow(tm1, m_fSpeedExponent);
                    float fFadeLerp = 1.0f - (float)Math.Pow(tm1, m_fFadeExponent);

                    float fSize = fSizeLerp * m_fEndSize + (1.0f - fSizeLerp) * m_fStartSize;
                    float fSpeed = fSpeedLerp * m_fEndSpeed + (1.0f - fSpeedLerp) * m_fStartSpeed;
                    float fFade = fFadeLerp;

                    XMVector vVel = m_pParticles[i].vDir.ToVector() * fSpeed;
                    float fRot = 0.0f;
                    float fWindAmt = 1.0f;

                    vVel += vWindVel.ToVector() * fWindAmt;

                    m_pParticles[i].vPos += (float)fElapsedTime * vVel;

                    m_pParticles[i].Radius = fSize;

                    m_pParticles[i].Life += (float)fElapsedTime;
                    m_pParticles[i].Fade = fFade;

                    m_pParticles[i].Rot += fRot;

                    m_pParticles[i].Visible = true;
                }

                if (!m_bStarted)
                {
                    m_bStarted = true;
                }
            }
            else
            {
                for (int i = m_pParticlesStartIndex; i < m_pParticlesStartIndex + m_NumParticles; i++)
                {
                    m_pParticles[i].Visible = false;
                    m_pParticles[i].Life += (float)fElapsedTime;
                }
            }

            m_fCurrentTime += (float)fElapsedTime;
        }
    }
}
