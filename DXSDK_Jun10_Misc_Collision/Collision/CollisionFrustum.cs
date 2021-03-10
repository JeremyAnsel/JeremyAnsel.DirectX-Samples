using JeremyAnsel.DirectX.DXMath.Collision;

namespace Collision
{
    struct CollisionFrustum
    {
        public BoundingFrustum Frustum { get; set; }

        public ContainmentType Collision { get; set; }
    }
}
