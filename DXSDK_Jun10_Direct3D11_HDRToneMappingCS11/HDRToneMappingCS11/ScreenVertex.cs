using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Stuff used for drawing the "full screen quad"
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct ScreenVertex
    {
        public XMFloat4 pos;

        public XMFloat2 tex;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ScreenVertex));

        public ScreenVertex(XMFloat4 pos, XMFloat2 tex)
        {
            this.pos = pos;
            this.tex = tex;
        }
    }
}
