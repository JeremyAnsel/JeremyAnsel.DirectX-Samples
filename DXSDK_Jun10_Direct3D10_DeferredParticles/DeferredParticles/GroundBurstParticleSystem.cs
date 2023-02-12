using JeremyAnsel.DirectX.DXMath;
using System;

namespace DeferredParticles
{
    class GroundBurstParticleSystem : ParticleSystem
    {
        public GroundBurstParticleSystem()
        {
            m_PST = ParticleSystemType.GroundExp;
        }

        public override void Init()
        {
            int index = m_pParticlesStartIndex;

            int ParticlesPerStream = (m_NumParticles / (int)m_NumStreamers) + 1;

            for (int s = 0; s < m_NumStreamers; s++)
            {
                XMVector vStreamerDir = m_vDirection;
                vStreamerDir.X += Particles.RPercent() * m_vDirVariance.X;
                vStreamerDir.Y += Particles.RPercent() * m_vDirVariance.Y;
                vStreamerDir.Z += Particles.RPercent() * m_vDirVariance.Z;
                vStreamerDir = XMVector3.Normalize(vStreamerDir);

                XMVector vStreamerPos = default;
                vStreamerPos.X = Particles.RPercent() * m_fSpread;
                vStreamerPos.Y = Particles.RPercent() * m_fSpread;
                vStreamerPos.Z = Particles.RPercent() * m_fSpread;

                for (int i = 0; i < ParticlesPerStream; i++)
                {
                    if (index < m_pParticlesStartIndex + m_NumParticles)
                    {
                        m_pParticles[index].vPos = vStreamerPos;
                        m_pParticles[index].vPos.X *= m_vPosMul.X;
                        m_pParticles[index].vPos.Y *= m_vPosMul.Y;
                        m_pParticles[index].vPos.Z *= m_vPosMul.Z;
                        m_pParticles[index].vPos = m_pParticles[index].vPos.ToVector() + m_vCenter;

                        float fSpeed = m_fStartSpeed + Particles.RPercent() * m_fSpeedVariance;

                        m_pParticles[index].vDir = vStreamerDir * fSpeed;
                        m_pParticles[index].vDir.X *= m_vDirMul.X;
                        m_pParticles[index].vDir.Y *= m_vDirMul.Y;
                        m_pParticles[index].vDir.Z *= m_vDirMul.Z;

                        float fRadiusLerp = (fSpeed / (m_fStartSpeed + m_fSpeedVariance));

                        m_pParticles[index].Radius = m_fStartSize * fRadiusLerp + m_fEndSize * (1 - fRadiusLerp);
                        m_pParticles[index].Life = m_fStartTime;
                        m_pParticles[index].Fade = 0.0f;

                        m_pParticles[index].Rot = Particles.RPercent() * 3.14159f * 2.0f;

                        m_pParticles[index].RotRate = Particles.RPercent() * 1.5f;

                        float fLerp = Particles.RPercent();
                        XMVector vColor = (m_vColor0 * fLerp) + (m_vColor1 * (1.0f - fLerp));
                        m_pParticles[index].Color = (uint)(vColor.W * 255.0f) << 24;
                        m_pParticles[index].Color |= ((uint)(vColor.Z * 255.0f) & 255) << 16;
                        m_pParticles[index].Color |= ((uint)(vColor.Y * 255.0f) & 255) << 8;
                        m_pParticles[index].Color |= ((uint)(vColor.X * 255.0f) & 255);

                        index++;
                    }
                }
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
                    float fSpeedLerp = (float)Math.Pow(tm1, m_fSpeedExponent);
                    float fFadeLerp = 1.0f - (float)Math.Pow(tm1, m_fFadeExponent);
                    float fFade = fFadeLerp;

                    XMVector vDelta = m_pParticles[i].vPos.ToVector() - m_vCenter;

                    float fRot = m_pParticles[i].RotRate * (float)fElapsedTime;
                    float fWindAmt = 1.0f;

                    fWindAmt = Math.Max(0.0f, Math.Min(1.0f, vDelta.Y / m_fWindFalloff));
                    XMVector vWind = vWindVel.ToVector() * fWindAmt;
                    vWind.Y = 0;

                    m_pParticles[i].vPos += (float)fElapsedTime * (m_pParticles[i].vDir + vWind);

                    if (m_pParticles[i].vPos.Y < 0)
                    {
                        m_pParticles[i].vPos.Y = 0;
                    }

                    m_pParticles[i].vDir += vGravity.ToVector() * (float)fElapsedTime;

                    float fDrag = 8.0f * fSpeedLerp;
                    m_pParticles[i].vDir = m_pParticles[i].vDir.ToVector() * (1.0f - fDrag * (float)fElapsedTime);

                    m_pParticles[i].Radius += fSizeLerp * (float)fElapsedTime;

                    m_pParticles[i].Life += (float)fElapsedTime;
                    m_pParticles[i].Fade = fFade;

                    m_pParticles[i].Rot += fRot;

                    m_pParticles[i].Visible = true;
                }

                if (!m_bStarted)
                {
                    XMFloat3 vCenter = m_vCenter;
                    vCenter.Y = -2.0f;
                    float fSize = 5.0f;

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
