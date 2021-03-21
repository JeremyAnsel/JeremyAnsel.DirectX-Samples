using JeremyAnsel.DirectX.DXMath;

namespace VarianceShadows11
{
    // Used to compute an intersection of the orthographic projection and the Scene AABB
    class Triangle
    {
        public readonly XMVector[] pt = new XMVector[3];

        public bool culled;
    }
}
