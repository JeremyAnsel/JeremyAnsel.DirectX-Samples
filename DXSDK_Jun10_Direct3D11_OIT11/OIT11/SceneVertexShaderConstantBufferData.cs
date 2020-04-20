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
    struct SceneVertexShaderConstantBufferData
    {
        public XMFloat4X4 WorldViewProj;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SceneVertexShaderConstantBufferData));
    }
}
