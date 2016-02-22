using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BasicMaths
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Float4X4
    {
        private float m11;

        private float m12;

        private float m13;

        private float m14;

        private float m21;

        private float m22;

        private float m23;

        private float m24;

        private float m31;

        private float m32;

        private float m33;

        private float m34;

        private float m41;

        private float m42;

        private float m43;

        private float m44;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float4X4(float value)
        {
            this.m11 = value;
            this.m12 = value;
            this.m13 = value;
            this.m14 = value;
            this.m21 = value;
            this.m22 = value;
            this.m23 = value;
            this.m24 = value;
            this.m31 = value;
            this.m32 = value;
            this.m33 = value;
            this.m34 = value;
            this.m41 = value;
            this.m42 = value;
            this.m43 = value;
            this.m44 = value;
        }

        [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray", Justification = "Reviewed")]
        public Float4X4(
            float i11,
            float i12,
            float i13,
            float i14,
            float i21,
            float i22,
            float i23,
            float i24,
            float i31,
            float i32,
            float i33,
            float i34,
            float i41,
            float i42,
            float i43,
            float i44)
        {
            this.m11 = i11;
            this.m12 = i12;
            this.m13 = i13;
            this.m14 = i14;
            this.m21 = i21;
            this.m22 = i22;
            this.m23 = i23;
            this.m24 = i24;
            this.m31 = i31;
            this.m32 = i32;
            this.m33 = i33;
            this.m34 = i34;
            this.m41 = i41;
            this.m42 = i42;
            this.m43 = i43;
            this.m44 = i44;
        }

        public static Float4X4 Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Float4X4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1); }
        }

        [SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Justification = "Reviewed")]
        public float this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (row < 0 || row >= 4)
                {
                    throw new ArgumentOutOfRangeException("row");
                }

                if (column < 0 || column >= 4)
                {
                    throw new ArgumentOutOfRangeException("column");
                }

                unsafe
                {
                    fixed (Float4X4* v = &this)
                    {
                        return ((float*)v)[row * 4 + column];
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (row < 0 || row >= 4)
                {
                    throw new ArgumentOutOfRangeException("row");
                }

                if (column < 0 || column >= 4)
                {
                    throw new ArgumentOutOfRangeException("column");
                }

                unsafe
                {
                    fixed (Float4X4* v = &this)
                    {
                        ((float*)v)[row * 4 + column] = value;
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b", Justification = "Reviewed")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 operator *(Float4X4 a, Float4X4 b)
        {
            return Float4X4.Multiply(a, b);
        }

        /// <summary>
        /// Compares two <see cref="Float4X4"/> objects. The result specifies whether the values of the two objects are equal.
        /// </summary>
        /// <param name="left">The left <see cref="Float4X4"/> to compare.</param>
        /// <param name="right">The right <see cref="Float4X4"/> to compare.</param>
        /// <returns><value>true</value> if the values of left and right are equal; otherwise, <value>false</value>.</returns>
        public static bool operator ==(Float4X4 left, Float4X4 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Float4X4"/> objects. The result specifies whether the values of the two objects are unequal.
        /// </summary>
        /// <param name="left">The left <see cref="Float4X4"/> to compare.</param>
        /// <param name="right">The right <see cref="Float4X4"/> to compare.</param>
        /// <returns><value>true</value> if the values of left and right differ; otherwise, <value>false</value>.</returns>
        public static bool operator !=(Float4X4 left, Float4X4 right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><value>true</value> if the specified object is equal to the current object; otherwise, <value>false</value>.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Float4X4))
            {
                return false;
            }

            return this.Equals((Float4X4)obj);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns><value>true</value> if the specified object is equal to the current object; otherwise, <value>false</value>.</returns>
        public bool Equals(Float4X4 other)
        {
            return this.m11 == other.m11
                && this.m12 == other.m12
                && this.m13 == other.m13
                && this.m14 == other.m14
                && this.m21 == other.m21
                && this.m22 == other.m22
                && this.m23 == other.m23
                && this.m24 == other.m24
                && this.m31 == other.m31
                && this.m32 == other.m32
                && this.m33 == other.m33
                && this.m34 == other.m34
                && this.m41 == other.m41
                && this.m42 == other.m42
                && this.m43 == other.m43
                && this.m44 == other.m44;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return new
            {
                this.m11,
                this.m12,
                this.m13,
                this.m14,
                this.m21,
                this.m22,
                this.m23,
                this.m24,
                this.m31,
                this.m32,
                this.m33,
                this.m34,
                this.m41,
                this.m42,
                this.m43,
                this.m44
            }
            .GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float4X4 Transpose()
        {
            return new Float4X4(
                this.m11,
                this.m21,
                this.m31,
                this.m41,
                this.m12,
                this.m22,
                this.m32,
                this.m42,
                this.m13,
                this.m23,
                this.m33,
                this.m43,
                this.m14,
                this.m24,
                this.m34,
                this.m44);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b", Justification = "Reviewed")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 Multiply(Float4X4 a, Float4X4 b)
        {
            Float4X4 mOut = new Float4X4();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        mOut[i, j] += a[i, k] * b[k, j];
                    }
                }
            }

            return mOut;
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "z", Justification = "Reviewed")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 Translation(float x, float y, float z)
        {
            return new Float4X4(1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "z", Justification = "Reviewed")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 Scale(float x, float y, float z)
        {
            return new Float4X4(x, 0, 0, 0, 0, y, 0, 0, 0, 0, z, 0, 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 RotationX(float degreeX)
        {
            float angleInRadians = degreeX * (BasicMath.PI / 180.0f);

            float sinAngle = (float)Math.Sin(angleInRadians);
            float cosAngle = (float)Math.Cos(angleInRadians);

            return new Float4X4(1, 0, 0, 0, 0, cosAngle, -sinAngle, 0, 0, sinAngle, cosAngle, 0, 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 RotationY(float degreeY)
        {
            float angleInRadians = degreeY * (BasicMath.PI / 180.0f);

            float sinAngle = (float)Math.Sin(angleInRadians);
            float cosAngle = (float)Math.Cos(angleInRadians);

            return new Float4X4(cosAngle, 0, sinAngle, 0, 0, 1, 0, 0, -sinAngle, 0, cosAngle, 0, 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 RotationZ(float degreeZ)
        {
            float angleInRadians = degreeZ * (BasicMath.PI / 180.0f);

            float sinAngle = (float)Math.Sin(angleInRadians);
            float cosAngle = (float)Math.Cos(angleInRadians);

            return new Float4X4(cosAngle, -sinAngle, 0, 0, sinAngle, cosAngle, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4X4 RotationArbitrary(Float3 axis, float degree)
        {
            axis = axis.Normalize();

            float angleInRadians = degree * (BasicMath.PI / 180.0f);

            float sinAngle = (float)Math.Sin(angleInRadians);
            float cosAngle = (float)Math.Cos(angleInRadians);
            float oneMinusCosAngle = 1 - cosAngle;

            Float4X4 mOut;

            mOut.m11 = 1.0f + oneMinusCosAngle * (axis.X * axis.X - 1.0f);
            mOut.m12 = axis.Z * sinAngle + oneMinusCosAngle * axis.X * axis.Y;
            mOut.m13 = -axis.Y * sinAngle + oneMinusCosAngle * axis.X * axis.Z;
            mOut.m14 = 0.0f;

            mOut.m21 = -axis.Z * sinAngle + oneMinusCosAngle * axis.Y * axis.X;
            mOut.m22 = 1.0f + oneMinusCosAngle * (axis.Y * axis.Y - 1.0f);
            mOut.m23 = axis.X * sinAngle + oneMinusCosAngle * axis.Y * axis.Z;
            mOut.m24 = 0.0f;

            mOut.m31 = axis.Y * sinAngle + oneMinusCosAngle * axis.Z * axis.X;
            mOut.m32 = -axis.X * sinAngle + oneMinusCosAngle * axis.Z * axis.Y;
            mOut.m33 = 1.0f + oneMinusCosAngle * (axis.Z * axis.Z - 1.0f);
            mOut.m34 = 0.0f;

            mOut.m41 = 0.0f;
            mOut.m42 = 0.0f;
            mOut.m43 = 0.0f;
            mOut.m44 = 1.0f;

            return mOut;
        }
    }
}
