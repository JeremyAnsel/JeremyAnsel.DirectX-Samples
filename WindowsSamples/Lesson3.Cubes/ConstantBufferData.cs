using BasicMaths;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lesson3.Cubes
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public Float4X4 Model;

        public Float4X4 View;

        public Float4X4 Projection;

        public static uint Size = (uint)Marshal.SizeOf<ConstantBufferData>();
    }
}
