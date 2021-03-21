using System;
using System.Collections.Generic;

namespace VarianceShadows11
{
    // when these paramters change, we must reallocate the shadow resources.
    class CascadeConfig : IEquatable<CascadeConfig>
    {
        public const int MaxCascades = 8;

        public const int MaxBlurLevels = 7;

        public int CascadeLevels { get; set; }

        public DepthBufferFormat ShadowBufferFormat { get; set; }

        public int BufferSize { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as CascadeConfig);
        }

        public bool Equals(CascadeConfig other)
        {
            return other != null &&
                   CascadeLevels == other.CascadeLevels &&
                   ShadowBufferFormat == other.ShadowBufferFormat &&
                   BufferSize == other.BufferSize;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CascadeLevels, ShadowBufferFormat, BufferSize);
        }

        public CascadeConfig ShallowCopy()
        {
            return (CascadeConfig)this.MemberwiseClone();
        }

        public static bool operator ==(CascadeConfig left, CascadeConfig right)
        {
            return EqualityComparer<CascadeConfig>.Default.Equals(left, right);
        }

        public static bool operator !=(CascadeConfig left, CascadeConfig right)
        {
            return !(left == right);
        }
    }
}
