using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OIT11
{
    [StructLayout(LayoutKind.Sequential)]
    struct SceneVertex
    {
        public XMFloat4 Pos;

        public XMFloat4 Color;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SceneVertex));

        public SceneVertex(XMFloat4 pos, XMFloat4 color)
        {
            this.Pos = pos;
            this.Color = color;
        }
    }
}
