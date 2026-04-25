using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    unsafe struct InstancedGlobalsConstantBuffer
    {
        public struct WorldInstBuffer
        {
            public fixed byte Buffer[TotalSize];
            public const int Length = MainGameComponent.MaxInstances;
            public const int TotalSize = sizeof(float) * 16 * Length;
        }

        public WorldInstBuffer g_mWorldInst;
    }
}
