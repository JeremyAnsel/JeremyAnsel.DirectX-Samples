using BasicMaths;
using JeremyAnsel.DirectX.D3D11;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lesson5.Components
{
    class BasicShapes
    {
        private readonly D3D11Device d3dDevice;

        public BasicShapes(D3D11Device d3dDevice)
        {
            this.d3dDevice = d3dDevice ?? throw new ArgumentNullException(nameof(d3dDevice));
        }

        public void CreateCube(out D3D11Buffer vertexBuffer, out D3D11Buffer indexBuffer, out int vertexCount, out int indexCount)
        {
            BasicVertex[] cubeVertices = new BasicVertex[]
            {
                new BasicVertex( new Float3(-0.5f, 0.5f, -0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 0.0f) ), // +Y (top face)
                new BasicVertex( new Float3( 0.5f, 0.5f, -0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex(new Float3( 0.5f, 0.5f,  0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3(-0.5f, 0.5f,  0.5f), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 1.0f) ),

                new BasicVertex( new Float3(-0.5f, -0.5f,  0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(0.0f, 0.0f) ), // -Y (bottom face)
                new BasicVertex( new Float3( 0.5f, -0.5f,  0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex( new Float3(0.5f, -0.5f, -0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(0.0f, -1.0f, 0.0f), new Float2(0.0f, 1.0f) ),

                new BasicVertex( new Float3(0.5f,  0.5f,  0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f) ), // +X (right face)
                new BasicVertex( new Float3(0.5f,  0.5f, -0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex( new Float3(0.5f, -0.5f, -0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3(0.5f, -0.5f,  0.5f), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.0f) ),

                new BasicVertex( new Float3(-0.5f,  0.5f, -0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f) ), // -X (left face)
                new BasicVertex( new Float3(-0.5f,  0.5f,  0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex( new Float3(-0.5f, -0.5f,  0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.0f) ),

                new BasicVertex( new Float3(-0.5f,  0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 0.0f) ), // +Z (front face)
                new BasicVertex( new Float3( 0.5f,  0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex( new Float3( 0.5f, -0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3(-0.5f, -0.5f, 0.5f), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 1.0f) ),

                new BasicVertex( new Float3( 0.5f,  0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 0.0f) ), // -Z (back face)
                new BasicVertex( new Float3(-0.5f,  0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(1.0f, 0.0f) ),
                new BasicVertex( new Float3(-0.5f, -0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(1.0f, 1.0f) ),
                new BasicVertex( new Float3( 0.5f, -0.5f, -0.5f), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 1.0f) ),
            };

            ushort[] cubeIndices = new ushort[]
            {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                8, 10, 11,

                12, 13, 14,
                12, 14, 15,

                16, 17, 18,
                16, 18, 19,

                20, 21, 22,
                20, 22, 23
            };

            this.CreateVertexBuffer(cubeVertices, out vertexBuffer);

            try
            {
                this.CreateIndexBuffer(cubeIndices, out indexBuffer);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref vertexBuffer);
                throw;
            }

            vertexCount = cubeVertices.Length;
            indexCount = cubeIndices.Length;
        }

        public void CreateBox(Float3 radii, out D3D11Buffer vertexBuffer, out D3D11Buffer indexBuffer, out int vertexCount, out int indexCount)
        {
            BasicVertex[] boxVertices = new BasicVertex[]
            {
                // FLOOR
                new BasicVertex( new Float3(-radii.X, -radii.Y,  radii.Z), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 0.0f)),
                new BasicVertex( new Float3( radii.X, -radii.Y,  radii.Z), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 0.0f)),
                new BasicVertex( new Float3(-radii.X, -radii.Y, -radii.Z), new Float3(0.0f, 1.0f, 0.0f), new Float2(0.0f, 1.5f)),
                new BasicVertex( new Float3( radii.X, -radii.Y, -radii.Z), new Float3(0.0f, 1.0f, 0.0f), new Float2(1.0f, 1.5f)),
                // WALL
                new BasicVertex( new Float3(-radii.X,  radii.Y, radii.Z), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 0.0f)),
                new BasicVertex( new Float3( radii.X,  radii.Y, radii.Z), new Float3(0.0f, 0.0f, -1.0f), new Float2(2.0f, 0.0f)),
                new BasicVertex( new Float3(-radii.X, -radii.Y, radii.Z), new Float3(0.0f, 0.0f, -1.0f), new Float2(0.0f, 1.5f)),
                new BasicVertex( new Float3( radii.X, -radii.Y, radii.Z), new Float3(0.0f, 0.0f, -1.0f), new Float2(2.0f, 1.5f)),
                // WALL
                new BasicVertex( new Float3(radii.X,  radii.Y,  radii.Z), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f)),
                new BasicVertex( new Float3(radii.X,  radii.Y, -radii.Z), new Float3(-1.0f, 0.0f, 0.0f), new Float2(radii.Y,  0.0f)),
                new BasicVertex( new Float3(radii.X, -radii.Y,  radii.Z), new Float3(-1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.5f)),
                new BasicVertex( new Float3(radii.X, -radii.Y, -radii.Z), new Float3(-1.0f, 0.0f, 0.0f), new Float2(radii.Y,  1.5f)),
                // WALL
                new BasicVertex( new Float3( radii.X,  radii.Y, -radii.Z), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 0.0f)),
                new BasicVertex( new Float3(-radii.X,  radii.Y, -radii.Z), new Float3(0.0f, 0.0f, 1.0f), new Float2(2.0f, 0.0f)),
                new BasicVertex( new Float3( radii.X, -radii.Y, -radii.Z), new Float3(0.0f, 0.0f, 1.0f), new Float2(0.0f, 1.5f)),
                new BasicVertex( new Float3(-radii.X, -radii.Y, -radii.Z), new Float3(0.0f, 0.0f, 1.0f), new Float2(2.0f, 1.5f)),
                // WALL
                new BasicVertex( new Float3(-radii.X,  radii.Y, -radii.Z), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 0.0f)),
                new BasicVertex( new Float3(-radii.X,  radii.Y,  radii.Z), new Float3(1.0f, 0.0f, 0.0f), new Float2(radii.Y,  0.0f)),
                new BasicVertex( new Float3(-radii.X, -radii.Y, -radii.Z), new Float3(1.0f, 0.0f, 0.0f), new Float2(0.0f, 1.5f)),
                new BasicVertex( new Float3(-radii.X, -radii.Y,  radii.Z), new Float3(1.0f, 0.0f, 0.0f), new Float2(radii.Y,  1.5f)),
                // CEILING
                new BasicVertex( new Float3(-radii.X, radii.Y, -radii.Z), new Float3(0.0f, -1.0f, 0.0f), new Float2(-0.15f, 0.0f)),
                new BasicVertex( new Float3( radii.X, radii.Y, -radii.Z), new Float3(0.0f, -1.0f, 0.0f), new Float2( 1.25f, 0.0f)),
                new BasicVertex( new Float3(-radii.X, radii.Y,  radii.Z), new Float3(0.0f, -1.0f, 0.0f), new Float2(-0.15f, 2.1f)),
                new BasicVertex( new Float3( radii.X, radii.Y,  radii.Z), new Float3(0.0f, -1.0f, 0.0f), new Float2( 1.25f, 2.1f)),
            };

            ushort[] boxIndices = new ushort[]
            {
                0, 2, 1,
                1, 2, 3,

                4, 6, 5,
                5, 6, 7,

                8, 10, 9,
                9, 10, 11,

                12, 14, 13,
                13, 14, 15,

                16, 18, 17,
                17, 18, 19,

                20, 22, 21,
                21, 22, 23,
            };

            this.CreateVertexBuffer(boxVertices, out vertexBuffer);

            try
            {
                this.CreateIndexBuffer(boxIndices, out indexBuffer);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref vertexBuffer);
                throw;
            }

            vertexCount = boxVertices.Length;
            indexCount = boxIndices.Length;
        }

        public void CreateSphere(out D3D11Buffer vertexBuffer, out D3D11Buffer indexBuffer, out int vertexCount, out int indexCount)
        {
            const int numSegments = 64;
            const int numSlices = numSegments / 2;

            const int numVertices = (numSlices + 1) * (numSegments + 1);
            var sphereVertices = new BasicVertex[numVertices];

            for (int slice = 0; slice <= numSlices; slice++)
            {
                float v = (float)slice / (float)numSlices;
                float inclination = v * BasicMath.PI;
                float y = (float)Math.Cos(inclination);
                float r = (float)Math.Sin(inclination);
                for (int segment = 0; segment <= numSegments; segment++)
                {
                    float u = (float)segment / (float)numSegments;
                    float azimuth = u * BasicMath.PI * 2.0f;
                    int vetexIndex = slice * (numSegments + 1) + segment;
                    sphereVertices[vetexIndex].Position = new Float3(r * (float)Math.Sin(azimuth), y, r * (float)Math.Cos(azimuth));
                    sphereVertices[vetexIndex].Normal = sphereVertices[vetexIndex].Position;
                    sphereVertices[vetexIndex].TextureCoordinates = new Float2(u, v);
                }
            }

            const int numIndices = numSlices * (numSegments - 2) * 6;
            var sphereIndices = new ushort[numIndices];

            uint index = 0;
            for (int slice = 0; slice < numSlices; slice++)
            {
                ushort sliceBase0 = (ushort)((slice) * (numSegments + 1));
                ushort sliceBase1 = (ushort)((slice + 1) * (numSegments + 1));
                for (int segment = 0; segment < numSegments; segment++)
                {
                    if (slice > 0)
                    {
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment);
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment + 1);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment + 1);
                    }
                    if (slice < numSlices - 1)
                    {
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment + 1);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment);
                    }
                }
            }

            this.CreateVertexBuffer(sphereVertices, out vertexBuffer);

            try
            {
                this.CreateIndexBuffer(sphereIndices, out indexBuffer);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref vertexBuffer);
                throw;
            }

            vertexCount = numVertices;
            indexCount = numIndices;
        }

        public void CreateTangentSphere(out D3D11Buffer vertexBuffer, out D3D11Buffer indexBuffer, out int vertexCount, out int indexCount)
        {
            const int numSegments = 64;
            const int numSlices = numSegments / 2;

            const int numVertices = (numSlices + 1) * (numSegments + 1);
            var sphereVertices = new TangentVertex[numVertices];

            for (int slice = 0; slice <= numSlices; slice++)
            {
                float v = (float)slice / (float)numSlices;
                float inclination = v * BasicMath.PI;
                float y = (float)Math.Cos(inclination);
                float r = (float)Math.Sin(inclination);
                for (int segment = 0; segment <= numSegments; segment++)
                {
                    float u = (float)segment / (float)numSegments;
                    float azimuth = u * BasicMath.PI * 2.0f;
                    int vertexIndex = slice * (numSegments + 1) + segment;
                    sphereVertices[vertexIndex].Position = new Float3(r * (float)Math.Sin(azimuth), y, r * (float)Math.Cos(azimuth));
                    sphereVertices[vertexIndex].TextureCoordinates = new Float2(u, v);
                    sphereVertices[vertexIndex].UTangent = new Float3((float)Math.Cos(azimuth), 0, -(float)Math.Sin(azimuth));
                    sphereVertices[vertexIndex].VTangent = new Float3((float)Math.Cos(inclination) * (float)Math.Sin(azimuth), -(float)Math.Sin(inclination), (float)Math.Cos(inclination) * (float)Math.Cos(azimuth));

                }
            }

            const int numIndices = numSlices * (numSegments - 2) * 6;
            var sphereIndices = new ushort[numIndices];

            uint index = 0;
            for (int slice = 0; slice < numSlices; slice++)
            {
                ushort sliceBase0 = (ushort)((slice) * (numSegments + 1));
                ushort sliceBase1 = (ushort)((slice + 1) * (numSegments + 1));
                for (int segment = 0; segment < numSegments; segment++)
                {
                    if (slice > 0)
                    {
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment);
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment + 1);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment + 1);
                    }
                    if (slice < numSlices - 1)
                    {
                        sphereIndices[index++] = (ushort)(sliceBase0 + segment);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment + 1);
                        sphereIndices[index++] = (ushort)(sliceBase1 + segment);
                    }
                }
            }

            this.CreateTangentVertexBuffer(sphereVertices, out vertexBuffer);

            try
            {
                this.CreateIndexBuffer(sphereIndices, out indexBuffer);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref vertexBuffer);
                throw;
            }

            vertexCount = numVertices;
            indexCount = numIndices;
        }

        public void CreateReferenceAxis(out D3D11Buffer vertexBuffer, out D3D11Buffer indexBuffer, out int vertexCount, out int indexCount)
        {
            BasicVertex[] axisVertices = new BasicVertex[]
            {
                new BasicVertex( new Float3( 0.500f, 0.000f, 0.000f), new Float3( 0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.500f, 0.000f, 0.000f), new Float3( 0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.500f, 0.000f, 0.000f), new Float3( 0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.500f, 0.000f, 0.000f), new Float3( 0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3(-0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3(-0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3(-0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3(-0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.250f) ),
                new BasicVertex( new Float3(-0.500f, 0.000f, 0.000f), new Float3(-0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3(-0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3(-0.125f, 0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3(-0.500f, 0.000f, 0.000f), new Float3(-0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3(-0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3(-0.125f, 0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3(-0.500f, 0.000f, 0.000f), new Float3(-0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3(-0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3(-0.125f,-0.500f,-0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3(-0.500f, 0.000f, 0.000f), new Float3(-0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3(-0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3(-0.125f,-0.500f, 0.500f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.125f, 0.000f, 0.000f), new Float2(0.250f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.500f, 0.000f), new Float3( 0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.500f, 0.000f), new Float3( 0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.500f, 0.000f), new Float3(-0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3(-0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f, 0.125f,-0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.500f, 0.000f), new Float3(-0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3(-0.500f, 0.125f, 0.500f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.000f,-0.125f, 0.000f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.000f,-0.125f, 0.000f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3( 0.000f,-0.125f, 0.000f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.000f,-0.125f, 0.000f), new Float2(0.500f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.500f, 0.000f), new Float3( 0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.500f, 0.000f), new Float3(-0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3(-0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f,-0.125f, 0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.500f, 0.000f), new Float3(-0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3(-0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.500f, 0.000f), new Float3( 0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f,-0.125f,-0.500f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.125f), new Float3( 0.000f, 0.125f, 0.000f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.125f, 0.000f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.125f), new Float3( 0.000f, 0.125f, 0.000f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.125f, 0.000f), new Float2(0.500f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.500f), new Float3( 0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.500f), new Float3(-0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3(-0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f, 0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.500f), new Float3(-0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3(-0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f, 0.500f), new Float3( 0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f,-0.500f, 0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.000f,-0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.000f, 0.000f,-0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.000f,-0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.000f, 0.000f,-0.125f), new Float2(0.750f, 0.250f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.500f), new Float3( 0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.500f), new Float3( 0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.500f), new Float3(-0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3(-0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f,-0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.000f,-0.500f), new Float3(-0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3(-0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3(-0.500f, 0.500f,-0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f, 0.125f, 0.000f), new Float3( 0.000f, 0.000f, 0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3(-0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.000f, 0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.000f,-0.125f, 0.000f), new Float3( 0.000f, 0.000f, 0.125f), new Float2(0.750f, 0.500f) ),
                new BasicVertex( new Float3( 0.125f, 0.000f, 0.000f), new Float3( 0.000f, 0.000f, 0.125f), new Float2(0.750f, 0.500f) ),
            };

            ushort[] axisIndices = new ushort[]
            {
                 0,  2,  1,
                 3,  5,  4,
                 6,  8,  7,
                 9, 11, 10,
                12, 14, 13,
                12, 15, 14,
                16, 18, 17,
                19, 21, 20,
                22, 24, 23,
                25, 27, 26,
                28, 30, 29,
                28, 31, 30,
                32, 34, 33,
                35, 37, 36,
                38, 40, 39,
                41, 43, 42,
                44, 46, 45,
                44, 47, 46,
                48, 50, 49,
                51, 53, 52,
                54, 56, 55,
                57, 59, 58,
                60, 62, 61,
                60, 63, 62,
                64, 66, 65,
                67, 69, 68,
                70, 72, 71,
                73, 75, 74,
                76, 78, 77,
                76, 79, 78,
                80, 82, 81,
                83, 85, 84,
                86, 88, 87,
                89, 91, 90,
                92, 94, 93,
                92, 95, 94,
            };

            this.CreateVertexBuffer(axisVertices, out vertexBuffer);

            try
            {
                this.CreateIndexBuffer(axisIndices, out indexBuffer);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref vertexBuffer);
                throw;
            }

            vertexCount = axisVertices.Length;
            indexCount = axisIndices.Length;
        }

        private void CreateVertexBuffer(BasicVertex[] vertexData, out D3D11Buffer vertexBuffer)
        {
            D3D11BufferDesc vertexBufferDesc = D3D11BufferDesc.From(vertexData, D3D11BindOptions.VertexBuffer);
            vertexBuffer = this.d3dDevice.CreateBuffer(vertexBufferDesc, vertexData, 0, 0);
        }

        private void CreateIndexBuffer(ushort[] indexData, out D3D11Buffer indexBuffer)
        {
            D3D11BufferDesc indexBufferDesc = D3D11BufferDesc.From(indexData, D3D11BindOptions.IndexBuffer);
            indexBuffer = this.d3dDevice.CreateBuffer(indexBufferDesc, indexData, 0, 0);
        }

        private void CreateTangentVertexBuffer(TangentVertex[] vertexData, out D3D11Buffer vertexBuffer)
        {
            D3D11BufferDesc vertexBufferDesc = D3D11BufferDesc.From(vertexData, D3D11BindOptions.VertexBuffer);
            vertexBuffer = this.d3dDevice.CreateBuffer(vertexBufferDesc, vertexData, 0, 0);
        }
    }
}
