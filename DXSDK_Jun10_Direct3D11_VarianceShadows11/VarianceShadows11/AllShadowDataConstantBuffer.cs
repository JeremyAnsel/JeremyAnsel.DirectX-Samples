using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace VarianceShadows11
{
    [StructLayout(LayoutKind.Sequential)]
    struct AllShadowDataConstantBuffer
    {
        public XMMatrix m_WorldViewProj;
        public XMMatrix m_World;
        public XMMatrix m_WorldView;
        public XMMatrix m_Shadow;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public XMVector[] m_vCascadeOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public XMVector[] m_vCascadeScale;

        public int m_nCascadeLevels; // Number of Cascades
        public int m_iVisualizeCascades; // 1 is to visualize the cascades in different colors. 0 is to just draw the scene.

        // For Map based selection scheme, this keeps the pixels inside of the the valid range.
        // When there is no boarder, these values are 0 and 1 respectivley.
        public float m_fMinBorderPadding;
        public float m_fMaxBorderPadding;

        public float m_fCascadeBlendArea; // Amount to overlap when blending between cascades.
        public float m_fTexelSize; // Shadow map texel size.
        public float m_fNativeTexelSizeInX; // Texel size in native map ( textures are packed ).
        public float m_fPaddingForCB3;// Padding variables CBs must be a multiple of 16 bytes.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public float[] m_fCascadeFrustumsEyeSpaceDepths; // The values along Z that seperate the cascades.
        public XMVector m_vLightDir;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(AllShadowDataConstantBuffer));
    }
}
