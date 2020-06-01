using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleBezier11
{
    [StructLayout(LayoutKind.Sequential)]
    struct BezierControlPoint
    {
        public XMFloat3 Position;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(BezierControlPoint));

        public BezierControlPoint(XMFloat3 position)
        {
            this.Position = position;
        }
    }
}
