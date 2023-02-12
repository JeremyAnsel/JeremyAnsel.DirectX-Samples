using JeremyAnsel.DirectX.DXMath;
using System;

namespace DeferredParticles
{
    static class Particles
    {
        private static Random random = new Random(0);

        public static void SetDefaultSeed()
        {
            random = new Random();
        }

        public static int Rand()
        {
            return random.Next();
        }

        public static float RPercent()
        {
            float ret = random.Next(20000) - 10000;
            return ret / 10000.0f;
        }

        private static void QuickDepthSort<T>(T[] indices, float[] depths, int lo, int hi)
        {
            //  lo is the lower index, hi is the upper index
            //  of the region of array a that is to be sorted
            int i = lo, j = hi;
            float h;
            T index;
            float x = depths[(lo + hi) / 2];

            //  partition
            do
            {
                while (depths[i] < x) i++;
                while (depths[j] > x) j--;

                if (i <= j)
                {
                    h = depths[i];
                    depths[i] = depths[j];
                    depths[j] = h;

                    index = indices[i];
                    indices[i] = indices[j];
                    indices[j] = index;

                    i++;
                    j--;
                }
            } while (i <= j);

            //  recursion
            if (lo < j) QuickDepthSort(indices, depths, lo, j);
            if (i < hi) QuickDepthSort(indices, depths, i, hi);
        }

        private static int g_NumUsedParticles = 0;

        private static Particle[] g_pParticleArray;

        private static uint[] g_pParticleIndices;

        private static float[] g_pParticleDepths;

        private static int g_NumActiveParticles = 0;

        public static void CreateParticleArray(int MaxParticles)
        {
            g_NumUsedParticles = 0;
            g_pParticleArray = new Particle[MaxParticles];
            g_pParticleIndices = new uint[MaxParticles];
            g_pParticleDepths = new float[MaxParticles];
        }

        public static void DestroyParticleArray()
        {
            g_NumUsedParticles = 0;
            g_pParticleArray = null;
            g_pParticleIndices = null;
            g_pParticleDepths = null;
        }

        private static void SortParticles(in XMFloat3 vEye)
        {
            for (uint i = 0; i < g_NumUsedParticles; i++)
            {
                g_pParticleIndices[i] = i;
                XMVector vDelta = vEye.ToVector() - g_pParticleArray[i].vPos.ToVector();
                g_pParticleDepths[i] = XMVector3.LengthSquare(vDelta).X;
            }

            QuickDepthSort(g_pParticleIndices, g_pParticleDepths, 0, g_NumUsedParticles - 1);
        }

        private static readonly XMFloat2[] g_sQuad = new XMFloat2[4]
        {
            new XMFloat2( -1, -1 ),
            new XMFloat2( 1, -1 ),
            new XMFloat2( 1, 1 ),
            new XMFloat2( -1, 1 )
        };

        public static void CopyParticlesToVertexBuffer(ParticleVertex[] pVB, in XMFloat3 vEye, in XMFloat3 vRight, in XMFloat3 vUp)
        {
            SortParticles(vEye);

            g_NumActiveParticles = 0;
            uint iVBIndex = 0;

            for (int i = g_NumUsedParticles - 1; i >= 0; i--)
            {
                uint index = g_pParticleIndices[i];

                XMVector vPos = g_pParticleArray[index].vPos;
                float fRadius = g_pParticleArray[index].Radius;
                float fRot = g_pParticleArray[index].Rot;
                float fFade = g_pParticleArray[index].Fade;
                uint vColor = g_pParticleArray[index].Color;

                if (!g_pParticleArray[index].Visible)
                {
                    continue;
                }

                // rotate
                XMScalar.SinCos(out float fSinTheta, out float fCosTheta, fRot);

                XMFloat2[] New = new XMFloat2[4];
                for (int v = 0; v < 4; v++)
                {
                    New[v].X = fCosTheta * g_sQuad[v].X - fSinTheta * g_sQuad[v].Y;
                    New[v].Y = fSinTheta * g_sQuad[v].X + fCosTheta * g_sQuad[v].Y;

                    New[v].X *= fRadius;
                    New[v].Y *= fRadius;
                }

                // Tri 0 (0,1,3)
                pVB[iVBIndex + 2].vPos = vPos + vRight.ToVector() * New[0].X + vUp.ToVector() * New[0].Y;
                pVB[iVBIndex + 2].vUV = new XMFloat2(0, 1);
                pVB[iVBIndex + 2].Life = fFade;
                pVB[iVBIndex + 2].Rot = fRot;
                pVB[iVBIndex + 2].Color = vColor;
                pVB[iVBIndex + 1].vPos = vPos + vRight.ToVector() * New[1].X + vUp.ToVector() * New[1].Y;
                pVB[iVBIndex + 1].vUV = new XMFloat2(1, 1);
                pVB[iVBIndex + 1].Life = fFade;
                pVB[iVBIndex + 1].Rot = fRot;
                pVB[iVBIndex + 1].Color = vColor;
                pVB[iVBIndex + 0].vPos = vPos + vRight.ToVector() * New[3].X + vUp.ToVector() * New[3].Y;
                pVB[iVBIndex + 0].vUV = new XMFloat2(0, 0);
                pVB[iVBIndex + 0].Life = fFade;
                pVB[iVBIndex + 0].Rot = fRot;
                pVB[iVBIndex + 0].Color = vColor;

                // Tri 1 (3,1,2)
                pVB[iVBIndex + 5].vPos = vPos + vRight.ToVector() * New[3].X + vUp.ToVector() * New[3].Y;
                pVB[iVBIndex + 5].vUV = new XMFloat2(0, 0);
                pVB[iVBIndex + 5].Life = fFade;
                pVB[iVBIndex + 5].Rot = fRot;
                pVB[iVBIndex + 5].Color = vColor;
                pVB[iVBIndex + 4].vPos = vPos + vRight.ToVector() * New[1].X + vUp.ToVector() * New[1].Y;
                pVB[iVBIndex + 4].vUV = new XMFloat2(1, 1);
                pVB[iVBIndex + 4].Life = fFade;
                pVB[iVBIndex + 4].Rot = fRot;
                pVB[iVBIndex + 4].Color = vColor;
                pVB[iVBIndex + 3].vPos = vPos + vRight.ToVector() * New[2].X + vUp.ToVector() * New[2].Y;
                pVB[iVBIndex + 3].vUV = new XMFloat2(1, 0);
                pVB[iVBIndex + 3].Life = fFade;
                pVB[iVBIndex + 3].Rot = fRot;
                pVB[iVBIndex + 3].Color = vColor;

                iVBIndex += 6;

                g_NumActiveParticles++;
            }
        }

        public static void ReserveParticleArray(int numParticles, out Particle[] array, out int arrayStartIndex)
        {
            array = g_pParticleArray;
            arrayStartIndex = g_NumUsedParticles;
            g_NumUsedParticles += numParticles;
        }

        public static int GetNumActiveParticles()
        {
            return g_NumActiveParticles;
        }
    }
}
