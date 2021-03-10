using JeremyAnsel.DirectX.DXMath.Collision;

namespace Collision
{
    struct CollisionSphere
    {
        public BoundingSphere Sphere { get; set; }

        public ContainmentType Collision { get; set; }
    }
}
