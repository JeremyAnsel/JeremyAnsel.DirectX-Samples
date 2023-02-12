using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;
using JeremyAnsel.DirectX.SdkMesh;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct MeshInstance
    {
        public SdkMeshFile Mesh;

        public BoundingSphere BS;

        public XMFloat3 Position;

        public XMFloat3 Rotation;

        public XMFloat3 RotationOrig;

        public bool Dynamic;

        public bool Visible;

        public XMFloat3 Velocity;

        public XMFloat3 RotationSpeed;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(MeshInstance));
    }
}
