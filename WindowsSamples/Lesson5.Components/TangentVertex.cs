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
    struct TangentVertex
    {
        public Float3 Position;

        public Float2 TextureCoordinates;

        public Float3 UTangent;

        public Float3 VTangent;

        public static uint Size = (uint)Marshal.SizeOf(typeof(TangentVertex));

        public TangentVertex(Float3 position, Float2 textureCoordinates, Float3 uTangent, Float3 vTangent)
        {
            this.Position = position;
            this.TextureCoordinates = textureCoordinates;
            this.UTangent = uTangent;
            this.VTangent = vTangent;
        }
    }
}
