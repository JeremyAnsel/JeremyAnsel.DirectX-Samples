using System.Runtime.InteropServices;
using BasicMaths;

namespace Lesson3.Cubes
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
