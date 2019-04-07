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
    struct BasicVertex
    {
        public Float3 Position;

        public Float3 Normal;

        public Float2 TextureCoordinates;

        public static uint Size = (uint)Marshal.SizeOf(typeof(BasicVertex));

        public BasicVertex(Float3 position, Float3 normal, Float2 textureCoordinates)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinates = textureCoordinates;
        }
    }
}
