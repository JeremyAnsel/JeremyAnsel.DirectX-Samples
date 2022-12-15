using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FixedFuncEmu
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPerViewData
    {
        public ConstantBufferPerViewData()
        {
        }

        // viewport params

        public float g_viewportHeight = 0.0f;

        public float g_viewportWidth = 0.0f;

        public float g_nearPlane = 0.0f;

        private float padding2 = 0.0f;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPerViewData));
    }
}
