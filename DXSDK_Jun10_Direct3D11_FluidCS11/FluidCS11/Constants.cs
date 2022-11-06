using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    static class Constants
    {
        // Compute Shader Constants
        // Grid cell key size for sorting, 8-bits for x and y
        public const uint NUM_GRID_INDICES = 65536;

        // Numthreads size for the simulation
        public const uint SIMULATION_BLOCK_SIZE = 256;

        // Numthreads size for the sort
        public const uint BITONIC_BLOCK_SIZE = 512;
        public const uint TRANSPOSE_BLOCK_SIZE = 16;

        // For this sample, only use power-of-2 numbers >= 8K and <= 64K
        // The algorithm can be extended to support any number of particles
        // But to keep the sample simple, we do not implement boundary conditions to handle it
        public const uint NUM_PARTICLES_8K = 8 * 1024;
        public const uint NUM_PARTICLES_16K = 16 * 1024;
        public const uint NUM_PARTICLES_32K = 32 * 1024;
        public const uint NUM_PARTICLES_64K = 64 * 1024;

        // Particle Properties
        // These will control how the fluid behaves
        public const float g_fInitialParticleSpacing = 0.0045f;
        public const float g_fSmoothlen = 0.012f;
        public const float g_fPressureStiffness = 200.0f;
        public const float g_fRestDensity = 1000.0f;
        public const float g_fParticleMass = 0.0002f;
        public const float g_fViscosity = 0.1f;
        public const float g_fMaxAllowableTimeStep = 0.005f;
        public const float g_fParticleRenderSize = 0.003f;

        // Gravity Directions
        public static readonly XMFloat2 GRAVITY_DOWN = new(0, -0.5f);
        public static readonly XMFloat2 GRAVITY_UP = new(0, 0.5f);
        public static readonly XMFloat2 GRAVITY_LEFT = new(-0.5f, 0);
        public static readonly XMFloat2 GRAVITY_RIGHT = new(0.5f, 0);

        // Map Size
        // These values should not be larger than 256 * fSmoothlen
        // Since the map must be divided up into fSmoothlen sized grid cells
        // And the grid cell is used as a 16-bit sort key, 8-bits for x and y
        public const float g_fMapHeight = 1.2f;
        public const float g_fMapWidth = (4.0f / 3.0f) * g_fMapHeight;

        // Map Wall Collision Planes
        public const float g_fWallStiffness = 3000.0f;

        public static readonly XMFloat4[] g_vPlanes = new XMFloat4[4]
        {
            new(1, 0, 0, 0),
            new(0, 1, 0, 0),
            new(-1, 0, g_fMapWidth, 0),
            new(0, -1, g_fMapHeight, 0)
        };
    }
}
