using JeremyAnsel.DirectX.DXMath.Collision;

namespace Collision
{
    struct CollisionAABox
    {
        public BoundingBox Box { get; set; }

        public ContainmentType Collision { get; set; }
    }
}
