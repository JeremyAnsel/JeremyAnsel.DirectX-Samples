using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;

namespace Collision
{
    struct CollisionTriangle
    {
        public XMVector PointA { get; set; }

        public XMVector PointB { get; set; }

        public XMVector PointC { get; set; }

        public ContainmentType Collision { get; set; }
    }
}
