using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct InstancedGlobalsConstantBuffer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MainGameComponent.MaxInstances)]
        public XMMatrix[] g_mWorldInst;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(InstancedGlobalsConstantBuffer));
    }
}
