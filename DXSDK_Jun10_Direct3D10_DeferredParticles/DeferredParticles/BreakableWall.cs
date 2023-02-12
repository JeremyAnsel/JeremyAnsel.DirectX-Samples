using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;
using JeremyAnsel.DirectX.SdkMesh;
using System;

namespace DeferredParticles
{
    class BreakableWall
    {
        public const int NumChunks = 9;

        private MeshInstance m_BaseMesh;

        private readonly MeshInstance[] m_ChunkMesh;

        private bool m_bBroken;

        public BreakableWall()
        {
            m_ChunkMesh = new MeshInstance[NumChunks];
            for (int i = 0; i < NumChunks; i++)
            {
                m_ChunkMesh[i] = new MeshInstance();
            }

            m_bBroken = false;
        }

        public void SetPosition(in XMFloat3 vPosition)
        {
            m_BaseMesh.Position = vPosition;

            for (uint i = 0; i < NumChunks; i++)
            {
                m_ChunkMesh[i].Position = vPosition;
            }
        }

        public void SetRotation(in XMFloat3 vRotation)
        {
            m_BaseMesh.Rotation = vRotation;
            m_BaseMesh.RotationOrig = vRotation;

            for (uint i = 0; i < NumChunks; i++)
            {
                m_ChunkMesh[i].Rotation = vRotation;
                m_ChunkMesh[i].RotationOrig = vRotation;
            }
        }

        public void SetBaseMesh(SdkMeshFile pMesh)
        {
            m_BaseMesh.Mesh = pMesh;

            BoundingSphere bs = default;

            for (int i = 0; i < pMesh.Meshes.Count; i++)
            {
                XMFloat3 center = pMesh.Meshes[i].BoundingBoxCenter;
                float radius = XMVector3.Length(pMesh.Meshes[i].BoundingBoxExtents).X;

                bs = BoundingSphere.CreateMerged(bs, new BoundingSphere(center, radius));
            }

            m_BaseMesh.BS = bs;

            m_BaseMesh.Visible = true;
        }

        public void SetChunkMesh(uint iChunk, SdkMeshFile pMesh, in XMFloat3 vOffset, float fWallScale)
        {
            m_ChunkMesh[iChunk].Mesh = pMesh;

            m_ChunkMesh[iChunk].Rotation = m_ChunkMesh[iChunk].RotationOrig;
            XMMatrix mRot = XMMatrix.RotationRollPitchYawFromVector(m_ChunkMesh[iChunk].Rotation);

            XMVector vRotOffset = XMVector3.TransformNormal(vOffset.ToVector().Scale(fWallScale), mRot);

            m_ChunkMesh[iChunk].Position = m_BaseMesh.Position + vRotOffset;


            BoundingSphere bs = default;

            for (int i = 0; i < pMesh.Meshes.Count; i++)
            {
                XMFloat3 center = pMesh.Meshes[i].BoundingBoxCenter;
                float radius = XMVector3.Length(pMesh.Meshes[i].BoundingBoxExtents).X;

                bs = BoundingSphere.CreateMerged(bs, new BoundingSphere(center, radius));
            }

            m_ChunkMesh[iChunk].BS = bs;

            m_ChunkMesh[iChunk].Dynamic = false;
            m_ChunkMesh[iChunk].Visible = true;
        }

        public XMMatrix GetMeshMatrix(in MeshInstance pInstance, float fWallScale)
        {
            XMMatrix mScale = XMMatrix.Scaling(fWallScale, fWallScale, fWallScale);
            XMMatrix mRot = XMMatrix.RotationRollPitchYawFromVector(pInstance.Rotation);
            XMMatrix mTrans = XMMatrix.TranslationFromVector(pInstance.Position);

            XMMatrix mWorld = mScale * mRot * mTrans;

            return mWorld;
        }

        public XMMatrix GetBaseMeshMatrix(float fWallScale)
        {
            return GetMeshMatrix(m_BaseMesh, fWallScale);
        }

        public XMMatrix GetChunkMeshMatrix(uint iChunk, float fWallScale)
        {
            return GetMeshMatrix(m_ChunkMesh[iChunk], fWallScale);
        }

        public bool IsBaseMeshVisible()
        {
            return !m_bBroken;
        }

        public bool IsChunkMeshVisible(uint iChunk)
        {
            return m_ChunkMesh[iChunk].Visible;
        }

        public void CreateExplosion(in XMFloat3 vCenter, in XMFloat3 vDirMul, float fRadius, float fMinPower, float fMaxPower, float fWallScale)
        {
            XMVector vDelta = vCenter - (m_BaseMesh.Position.ToVector() + m_BaseMesh.BS.Center.ToVector() * fWallScale);
            float fDist = XMVector3.LengthSquare(vDelta).X;

            float f2Rad = fRadius + m_BaseMesh.BS.Radius * fWallScale;

            if (fDist > f2Rad * f2Rad)
            {
                return;
            }

            // We're broken
            m_bBroken = true;

            for (uint i = 0; i < NumChunks; i++)
            {
                // center of gravity
                XMVector vCOG = m_ChunkMesh[i].Position;// + m_ChunkMesh[i].BS.Center*fWallScale;
                vDelta = vCOG - vCenter;
                fDist = XMVector3.LengthSquare(vDelta).X;
                float fChunkRad = m_ChunkMesh[i].BS.Radius * fWallScale;
                f2Rad = fRadius + fChunkRad;

                if (fDist < f2Rad * f2Rad)
                {
                    // We're in motion
                    m_ChunkMesh[i].Dynamic = true;

                    // Set velocity
                    vDelta = XMVector3.Normalize(vDelta);

                    fDist -= fChunkRad * fChunkRad;

                    float fPowerLerp = Math.Abs(Particles.RPercent());
                    float fPower = (fMaxPower * fPowerLerp + fMinPower * (1.0f - fPowerLerp));// / sqrt(fDist);

                    m_ChunkMesh[i].Velocity = vDelta * fPower;
                    m_ChunkMesh[i].Velocity.X *= vDirMul.X;
                    m_ChunkMesh[i].Velocity.Y *= vDirMul.Y;
                    m_ChunkMesh[i].Velocity.Z *= vDirMul.Z;

                    // Set rotation speed
                    float fRotationScalar = 3.0f;
                    m_ChunkMesh[i].RotationSpeed.X = Particles.RPercent() * fRotationScalar;
                    m_ChunkMesh[i].RotationSpeed.Y = Particles.RPercent() * fRotationScalar;
                    m_ChunkMesh[i].RotationSpeed.Z = Particles.RPercent() * fRotationScalar;
                }
            }
        }

        public void AdvancePieces(double fElapsedTime, in XMFloat3 vGravity)
        {
            if (!m_bBroken)
            {
                return;
            }

            for (uint i = 0; i < NumChunks; i++)
            {
                if (m_ChunkMesh[i].Dynamic)
                {
                    m_ChunkMesh[i].Velocity += vGravity.ToVector().Scale((float)fElapsedTime);
                    m_ChunkMesh[i].Position += m_ChunkMesh[i].Velocity.ToVector().Scale((float)fElapsedTime);
                    m_ChunkMesh[i].Rotation += m_ChunkMesh[i].RotationSpeed.ToVector().Scale((float)fElapsedTime);

                    if (m_ChunkMesh[i].Position.Y < -10.0f)
                    {
                        m_ChunkMesh[i].Visible = false;
                    }
                }
            }
        }
    }
}
