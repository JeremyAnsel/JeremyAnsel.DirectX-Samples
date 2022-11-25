using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    static class Constants
    {
        /// <summary>
        /// Maximum bump amount * 1000 (for UI slider)
        /// </summary>
        public const int MaxBump = 3000;

        /// <summary>
        /// Maximum divisions of a patch per side (about 2048 triangles)
        /// </summary>
        public const int MaxDivs = 32;

        public const int MaxBoneMatrices = 80;

        /// <summary>
        /// Maximum number of points that can be part of a subd quad.
        /// This includes the 4 interior points of the quad, plus the 1-ring neighborhood.
        /// </summary>
        public const int MaxExtraordinaryPoints = 32;

        /// <summary>
        /// Maximum valence we expect to encounter for extraordinary vertices.
        /// </summary>
        public const int MaxValence = 16;
    }
}
