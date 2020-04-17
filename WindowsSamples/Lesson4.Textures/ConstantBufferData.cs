using BasicMaths;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lesson4.Textures
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public Float4X4 Model;

        public Float4X4 View;

        public Float4X4 Projection;

        public static readonly uint Size = (uint)Marshal.SizeOf<ConstantBufferData>();
    }
}
