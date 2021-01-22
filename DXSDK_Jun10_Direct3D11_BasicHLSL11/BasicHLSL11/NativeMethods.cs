using System;
using System.Runtime.InteropServices;
using System.Security;

namespace BasicHLSL11
{
    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "SetCapture")]
        public static extern IntPtr SetCapture(IntPtr hWnd);
    }
}
