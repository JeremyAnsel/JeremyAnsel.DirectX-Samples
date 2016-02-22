using System.Runtime.InteropServices;
using BasicMaths;

namespace Lesson3.Cubes
{
    [StructLayout(LayoutKind.Sequential)]
    struct BasicVertex
    {
        public Float3 Position;

        public Float3 Color;

        public static uint Size = (uint)Marshal.SizeOf(typeof(BasicVertex));

        public BasicVertex(Float3 position, Float3 color)
        {
            this.Position = position;
            this.Color = color;
        }
    }
}
