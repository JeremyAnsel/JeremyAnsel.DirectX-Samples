using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;
using JeremyAnsel.DirectX.SdkMesh;
using System.Collections.Generic;

namespace DeferredParticles
{
    class Building
    {
        private uint m_WidthWalls;

        private uint m_HeightWalls;

        private uint m_DepthWalls;

        private uint m_NumWalls;

        private BreakableWall[] m_pWallList;

        private BoundingSphere m_BS;

        private float m_fWallScale;

        public Building()
        {
            m_WidthWalls = 0;
            m_HeightWalls = 0;
            m_DepthWalls = 0;

            m_NumWalls = 0;
            m_pWallList = null;

            m_fWallScale = 1.0f;
        }

        public bool CreateBuilding(in XMFloat3 vCenter, float fWallScale, uint WidthWalls, uint HeightWalls, uint DepthWalls)
        {
            m_WidthWalls = WidthWalls;
            m_HeightWalls = HeightWalls;
            m_DepthWalls = DepthWalls;
            m_fWallScale = fWallScale;

            uint NumNSWalls = WidthWalls * HeightWalls; // +z -z
            uint NumEWWalls = DepthWalls * HeightWalls; // +x -x
            uint NumRoofWalls = WidthWalls * DepthWalls;// +y

            m_NumWalls = NumNSWalls * 2 + NumEWWalls * 2 + NumRoofWalls;

            m_pWallList = new BreakableWall[m_NumWalls];
            for (int i = 0; i < m_NumWalls; i++)
            {
                m_pWallList[i] = new BreakableWall();
            }

            float f90Degrees = XMMath.PI / 2.0f;
            float f180Degrees = XMMath.PI;
            float fWallSize = 2.0f * m_fWallScale;

            float X, Y, Z;

            XMFloat3 vRotations = new(0, 0, 0);

            uint index = 0;

            // North wall
            Z = (fWallSize * m_DepthWalls) / 2.0f;
            Y = fWallSize / 2.0f;
            for (uint h = 0; h < m_HeightWalls; h++)
            {
                X = -(fWallSize * m_WidthWalls - fWallSize) / 2.0f;
                for (uint w = 0; w < m_WidthWalls; w++)
                {
                    m_pWallList[index].SetPosition(new XMFloat3(X, Y, Z).ToVector() + vCenter);
                    m_pWallList[index].SetRotation(vRotations);
                    index++;

                    X += fWallSize;
                }

                Y += fWallSize;
            }

            // South wall
            vRotations.Y = f180Degrees;
            Z = -(fWallSize * m_DepthWalls) / 2.0f;
            Y = fWallSize / 2.0f;
            for (uint h = 0; h < m_HeightWalls; h++)
            {
                X = -(fWallSize * m_WidthWalls - fWallSize) / 2.0f;
                for (uint w = 0; w < m_WidthWalls; w++)
                {
                    m_pWallList[index].SetPosition(new XMFloat3(X, Y, Z).ToVector() + vCenter);
                    m_pWallList[index].SetRotation(vRotations);
                    index++;

                    X += fWallSize;
                }
                Y += fWallSize;
            }

            // East wall
            vRotations.Y = -f90Degrees;
            X = (fWallSize * m_WidthWalls) / 2.0f;
            Y = fWallSize / 2.0f;
            for (uint h = 0; h < m_HeightWalls; h++)
            {
                Z = -(fWallSize * m_DepthWalls - fWallSize) / 2.0f;
                for (uint w = 0; w < m_DepthWalls; w++)
                {
                    m_pWallList[index].SetPosition(new XMFloat3(X, Y, Z).ToVector() + vCenter);
                    m_pWallList[index].SetRotation(vRotations);
                    index++;

                    Z += fWallSize;
                }

                Y += fWallSize;
            }

            // West wall
            vRotations.Y = f90Degrees;
            X = -(fWallSize * m_WidthWalls) / 2.0f;
            Y = fWallSize / 2.0f;
            for (uint h = 0; h < m_HeightWalls; h++)
            {
                Z = -(fWallSize * m_DepthWalls - fWallSize) / 2.0f;
                for (uint w = 0; w < m_DepthWalls; w++)
                {
                    m_pWallList[index].SetPosition(new XMFloat3(X, Y, Z).ToVector() + vCenter);
                    m_pWallList[index].SetRotation(vRotations);
                    index++;

                    Z += fWallSize;
                }

                Y += fWallSize;
            }

            // Roof wall
            vRotations.Y = 0;
            vRotations.X = -f90Degrees;
            Y = (fWallSize * m_HeightWalls);
            Z = -(fWallSize * m_DepthWalls - fWallSize) / 2.0f;
            for (uint h = 0; h < m_DepthWalls; h++)
            {
                X = -(fWallSize * m_WidthWalls - fWallSize) / 2.0f;
                for (uint w = 0; w < m_WidthWalls; w++)
                {
                    m_pWallList[index].SetPosition(new XMFloat3(X, Y, Z).ToVector() + vCenter);
                    m_pWallList[index].SetRotation(vRotations);
                    index++;

                    X += fWallSize;
                }

                Z += fWallSize;
            }

            // Bounding sphere
            m_BS.Center = vCenter.ToVector() + new XMFloat3(0, (fWallSize * m_HeightWalls) / 2.0f, 0);

            XMFloat3 vCorner = default;
            vCorner.X = (fWallSize * m_WidthWalls + fWallSize) / 2.0f;
            vCorner.Y = (fWallSize * m_HeightWalls + fWallSize) / 2.0f;
            vCorner.Z = (fWallSize * m_DepthWalls + fWallSize) / 2.0f;

            m_BS.Radius = XMVector3.Length(vCorner).X;

            return true;
        }

        public void SetBaseMesh(SdkMeshFile pMesh)
        {
            for (uint i = 0; i < m_NumWalls; i++)
            {
                m_pWallList[i].SetBaseMesh(pMesh);
            }
        }

        public void SetChunkMesh(uint iChunk, SdkMeshFile pMesh, in XMFloat3 vOffset)
        {
            for (uint i = 0; i < m_NumWalls; i++)
            {
                m_pWallList[i].SetChunkMesh(iChunk, pMesh, vOffset, m_fWallScale);
            }
        }

        public void CollectBaseMeshMatrices(List<XMMatrix> pMatrixArray)
        {
            for (uint i = 0; i < m_NumWalls; i++)
            {
                if (m_pWallList[i].IsBaseMeshVisible())
                {
                    XMMatrix m = m_pWallList[i].GetBaseMeshMatrix(m_fWallScale);
                    pMatrixArray.Add(m);
                }
            }
        }

        public void CollectChunkMeshMatrices(uint iChunk, List<XMMatrix> pMatrixArray)
        {
            for (uint i = 0; i < m_NumWalls; i++)
            {
                if (!m_pWallList[i].IsBaseMeshVisible() && m_pWallList[i].IsChunkMeshVisible(iChunk))
                {
                    XMMatrix m = m_pWallList[i].GetChunkMeshMatrix(iChunk, m_fWallScale);
                    pMatrixArray.Add(m);
                }
            }
        }

        public void CreateExplosion(in XMFloat3 vCenter, in XMFloat3 vDirMul, float fRadius, float fMinPower, float fMaxPower)
        {
            XMVector vDelta = vCenter.ToVector() - m_BS.Center.ToVector();
            float fDist = XMVector3.LengthSquare(vDelta).X;

            float f2Rad = fRadius + m_BS.Radius;

            if (fDist > f2Rad * f2Rad)
            {
                return;
            }

            for (uint i = 0; i < m_NumWalls; i++)
            {
                m_pWallList[i].CreateExplosion(vCenter, vDirMul, fRadius, fMinPower, fMaxPower, m_fWallScale);
            }
        }

        public void AdvancePieces(double fElapsedTime, in XMFloat3 vGravity)
        {
            for (uint i = 0; i < m_NumWalls; i++)
            {
                m_pWallList[i].AdvancePieces(fElapsedTime, vGravity);
            }
        }
    }
}
