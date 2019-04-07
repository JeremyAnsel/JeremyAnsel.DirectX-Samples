using BasicMaths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lesson5.Components
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public Float4X4 Model;

        public Float4X4 View;

        public Float4X4 Projection;

        public static uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferData));
    }
}
