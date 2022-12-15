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
    struct SceneLight
    {
        public XMFloat4 Position;

        public XMFloat4 Diffuse;

        public XMFloat4 Specular;

        public XMFloat4 Ambient;

        public XMFloat4 Atten;
    }
}
