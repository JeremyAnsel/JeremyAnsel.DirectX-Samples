using JeremyAnsel.DirectX.DXMath;

namespace ShadowVolume10
{
    struct LightInitData
    {
        public XMFloat3 Position;

        public XMFloat4 Color;

        public LightInitData(XMFloat3 position, XMFloat4 color)
        {
            this.Position = position;
            this.Color = color;
        }
    }
}
