using JeremyAnsel.DirectX.DXMath.Collision;

namespace Collision
{
    struct CollisionBox
    {
        public BoundingOrientedBox Box { get; set; }

        public ContainmentType Collision { get; set; }
    }
}
