using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ComputeShaderSort11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public uint iLevel;

        public uint iLevelMask;

        public uint iWidth;

        public uint iHeight;

        public static readonly uint Size = (uint)Marshal.SizeOf<ConstantBufferData>();
    }
}
