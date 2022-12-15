using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FixedFuncEmu
{
    [StructLayout(LayoutKind.Sequential)]
    struct SceneVertex
    {
        public SceneVertex(XMFloat3 pos, XMFloat3 norm, XMFloat2 tex)
        {
            this.pos = pos;
            this.norm = norm;
            this.tex = tex;
        }

        public XMFloat3 pos;

        public XMFloat3 norm;

        public XMFloat2 tex;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SceneVertex));
    }
}
