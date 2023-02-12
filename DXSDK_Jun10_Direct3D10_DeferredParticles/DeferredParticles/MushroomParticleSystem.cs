using JeremyAnsel.DirectX.DXMath;
using System;

namespace DeferredParticles
{
    class MushroomParticleSystem : ParticleSystem
    {
        public MushroomParticleSystem()
        {
            m_PST = ParticleSystemType.MushRoom;
        }

        public override void Init()
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

        public override void AdvanceSystem(double fTime, double fElapsedTime, in XMFloat3 vRight, in XMFloat3 vUp, in XMFloat3 vWindVel, in XMFloat3 vGravity)
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

                    XMVector vDelta = m_pParticles[i].vPos.ToVector() - m_vCenter;
                    float rightDist = XMVector3.Dot(vDelta, vRight).X;

                    XMVector vVel = m_pParticles[i].vDir.ToVector() * fSpeed;
                    float fRot = 0.0f;
                    float fWindAmt = 1.0f;

                    // Higher level should roll outward
                    fRot = -rightDist * m_fRollAmount * (float)fElapsedTime * (1.0f - t);

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
                    XMFloat3 vCenter = m_vCenter;
                    vCenter.Y = -2.0f;
                    float fSize = 6.0f;

                    NewExplosion?.Invoke(vCenter, fSize);

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
