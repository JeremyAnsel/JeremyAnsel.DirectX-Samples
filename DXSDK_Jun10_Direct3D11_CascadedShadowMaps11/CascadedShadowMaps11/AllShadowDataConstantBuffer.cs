using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace CascadedShadowMaps11
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
        public int m_iPCFBlurForLoopStart; // For loop begin value. For a 5x5 kernal this would be -2.
        public int m_iPCFBlurForLoopEnd; // For loop end value. For a 5x5 kernel this would be 3.

        // For Map based selection scheme, this keeps the pixels inside of the the valid range.
        // When there is no boarder, these values are 0 and 1 respectivley.
        public float m_fMinBorderPadding;
        public float m_fMaxBorderPadding;
        public float m_fShadowBiasFromGUI;  // A shadow map offset to deal with self shadow artifacts. These artifacts are aggravated by PCF.
        public float m_fShadowPartitionSize;
        public float m_fCascadeBlendArea; // Amount to overlap when blending between cascades.
        public float m_fTexelSize; // Shadow map texel size.
        public float m_fNativeTexelSizeInX; // Texel size in native map ( textures are packed ).
        public float m_fPaddingForCB3;// Padding variables CBs must be a multiple of 16 bytes.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public float[] m_fCascadeFrustumsEyeSpaceDepths; // The values along Z that seperate the cascades.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public XMVector[] m_fCascadeFrustumsEyeSpaceDepthsFloat4;// the values along Z that separte the cascades. Wastefully stored in float4 so they are array indexable :(
        public XMVector m_vLightDir;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(AllShadowDataConstantBuffer));
    }
}
