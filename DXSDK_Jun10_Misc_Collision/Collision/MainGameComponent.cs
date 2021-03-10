using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.DXMath.Collision;
using JeremyAnsel.DirectX.DXMath.PackedVector;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Diagnostics;
using System.IO;

namespace Collision
{
    class MainGameComponent : IGameComponent
    {
        private static readonly int GroupCount = Enum.GetValues<CollisionGroup>().Length;
        private const int CameraCount = 4;
        private const float CameraSpacing = 50.0f;
        private const int MaxVertices = 100;
        private const int MaxIndices = 24;

        private DeviceResources deviceResources;

        private D3D11VertexShader vertexShader;

        private D3D11InputLayout inputLayout;

        private D3D11PixelShader pixelShader;

        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        private D3D11Buffer constantBuffer;

        public XMMatrix worldMatrix;

        public XMMatrix viewMatrix;

        public XMMatrix projectionMatrix;

        // Primary collision objects
        private BoundingFrustum primaryFrustum;
        private BoundingOrientedBox primaryOrientedBox;
        private BoundingBox primaryAABox;
        private CollisionRay primaryRay;

        // Secondary collision objects
        private readonly CollisionSphere[] secondarySpheres = new CollisionSphere[GroupCount];
        private readonly CollisionBox[] secondaryOrientedBoxes = new CollisionBox[GroupCount];
        private readonly CollisionAABox[] secondaryAABoxes = new CollisionAABox[GroupCount];
        private readonly CollisionTriangle[] secondaryTriangles = new CollisionTriangle[GroupCount];

        // Ray testing results display object
        private CollisionAABox rayHitResultBox;

        // Camera preset locations
        public readonly XMVector[] cameraOrigins = new XMVector[CameraCount];

        public MainGameComponent()
        {
            this.InitializeObjects();
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel91;

        public CollisionGroup CollisionGroup { get; set; } = CollisionGroup.Frustum;

        private void InitializeObjects()
        {
            // Set up the primary frustum object from a D3D projection matrix
            // NOTE: This can also be done on your camera's projection matrix.  The projection
            // matrix built here is somewhat contrived so it renders well.
            XMMatrix xmProj = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, 1.77778f, 0.5f, 10.0f);
            this.primaryFrustum = BoundingFrustum.CreateFromMatrix(xmProj);
            this.primaryFrustum.Origin = new XMFloat3(this.primaryFrustum.Origin.X, this.primaryFrustum.Origin.Y, -7.0f);
            this.cameraOrigins[0] = new XMVector(0, 0, 0, 0);

            // Set up the primary axis aligned box
            this.primaryAABox.Center = new XMFloat3(CameraSpacing, 0, 0);
            this.primaryAABox.Extents = new XMFloat3(5, 5, 5);
            this.cameraOrigins[1] = new XMVector(CameraSpacing, 0, 0, 0);

            // Set up the primary oriented box with some rotation
            this.primaryOrientedBox.Center = new XMFloat3(-CameraSpacing, 0, 0);
            this.primaryOrientedBox.Extents = new XMFloat3(5, 5, 5);
            this.primaryOrientedBox.Orientation = XMQuaternion.RotationRollPitchYaw(XMMath.PIDivFour, XMMath.PIDivFour, 0);
            this.cameraOrigins[2] = new XMVector(-CameraSpacing, 0, 0, 0);

            // Set up the primary ray
            this.primaryRay.Origin = new XMVector(0, 0, CameraSpacing, 0);
            this.primaryRay.Direction = new XMVector(0, 0, 1, 0);
            this.cameraOrigins[3] = new XMVector(0, 0, CameraSpacing, 0);

            // Initialize all of the secondary objects with default values
            for (int i = 0; i < GroupCount; i++)
            {
                this.secondarySpheres[i].Sphere = new BoundingSphere(new XMFloat3(0, 0, 0), 1.0f);
                this.secondarySpheres[i].Collision = ContainmentType.Disjoint;

                this.secondaryOrientedBoxes[i].Box = new BoundingOrientedBox(new XMFloat3(0, 0, 0), new XMFloat3(0.5f, 0.5f, 0.5f), new XMFloat4(0, 0, 0, 1));
                this.secondaryOrientedBoxes[i].Collision = ContainmentType.Disjoint;

                this.secondaryAABoxes[i].Box = new BoundingBox(new XMFloat3(0, 0, 0), new XMFloat3(0.5f, 0.5f, 0.5f));
                this.secondaryAABoxes[i].Collision = ContainmentType.Disjoint;

                this.secondaryTriangles[i].PointA = XMVector.Zero;
                this.secondaryTriangles[i].PointB = XMVector.Zero;
                this.secondaryTriangles[i].PointC = XMVector.Zero;
                this.secondaryTriangles[i].Collision = ContainmentType.Disjoint;
            }

            // Set up ray hit result box
            this.rayHitResultBox.Box = new BoundingBox(new XMFloat3(0, 0, 0), new XMFloat3(0.05f, 0.05f, 0.05f));
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            byte[] vertexShaderBytecode = File.ReadAllBytes("VertexShader.cso");
            this.vertexShader = this.deviceResources.D3DDevice.CreateVertexShader(vertexShaderBytecode, null);

            D3D11InputElementDesc[] layoutDesc = new D3D11InputElementDesc[]
            {
                new D3D11InputElementDesc
                {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = DxgiFormat.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = D3D11InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            this.inputLayout = this.deviceResources.D3DDevice.CreateInputLayout(layoutDesc, vertexShaderBytecode);

            byte[] pixelShaderBytecode = File.ReadAllBytes("PixelShader.cso");
            this.pixelShader = this.deviceResources.D3DDevice.CreatePixelShader(pixelShaderBytecode, null);

            var vertexBufferDesc = new D3D11BufferDesc(MaxVertices * 12, D3D11BindOptions.VertexBuffer);
            this.vertexBuffer = this.deviceResources.D3DDevice.CreateBuffer(vertexBufferDesc);

            var indexBufferDesc = new D3D11BufferDesc(MaxIndices * 2, D3D11BindOptions.IndexBuffer);
            this.indexBuffer = this.deviceResources.D3DDevice.CreateBuffer(indexBufferDesc);

            var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.constantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

            this.worldMatrix = XMMatrix.Identity;

            XMVector vecAt = this.cameraOrigins[(int)this.CollisionGroup];
            XMVector vecEye = new(vecAt.X, vecAt.Y + 20.0f, (this.CollisionGroup == CollisionGroup.Frustum) ? (vecAt.Z + 20.0f) : (vecAt.Z - 20.0f), 0.0f);
            XMVector vecUp = new(0.0f, 1.0f, 0.0f, 0.0f);
            this.viewMatrix = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexShader);
            D3D11Utils.DisposeAndNull(ref this.inputLayout);
            D3D11Utils.DisposeAndNull(ref this.pixelShader);
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
            D3D11Utils.DisposeAndNull(ref this.constantBuffer);
        }

        public void CreateWindowSizeDependentResources()
        {
            // Setup the camera's projection parameters
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.projectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.1f, 1000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            this.Animate(timer?.TotalSeconds ?? 0.0);
            this.Collide();
        }

        private void Animate(double fTime)
        {
            float t = (float)(fTime * 0.2);

            float camera0OriginX = this.cameraOrigins[0].X;
            float camera1OriginX = this.cameraOrigins[1].X;
            float camera2OriginX = this.cameraOrigins[2].X;
            float camera3OriginX = this.cameraOrigins[3].X;
            float camera3OriginZ = this.cameraOrigins[3].Z;

            // animate sphere 0 around the frustum
            {
                BoundingSphere sphere = this.secondarySpheres[0].Sphere;

                sphere.Center = new XMFloat3
                {
                    X = 10 * XMScalar.Sin(3 * t),
                    Y = 7 * XMScalar.Cos(5 * t),
                    Z = sphere.Center.Z
                };

                this.secondarySpheres[0].Sphere = sphere;
            }

            // animate oriented box 0 around the frustum
            {
                BoundingOrientedBox box = this.secondaryOrientedBoxes[0].Box;

                box.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(3.5f * t),
                    Y = 5 * XMScalar.Cos(5.1f * t),
                    Z = box.Center.Z
                };

                box.Orientation = XMQuaternion.RotationRollPitchYaw(t * 1.4f, t * 0.2f, t);

                this.secondaryOrientedBoxes[0].Box = box;
            }

            // animate aligned box 0 around the frustum
            {
                BoundingBox box = this.secondaryAABoxes[0].Box;

                box.Center = new XMFloat3
                {
                    X = 10 * XMScalar.Sin(2.1f * t),
                    Y = 7 * XMScalar.Cos(3.8f * t),
                    Z = box.Center.Z
                };

                this.secondaryAABoxes[0].Box = box;
            }

            // animate sphere 1 around the aligned box
            {
                BoundingSphere sphere = this.secondarySpheres[1].Sphere;

                sphere.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(2.9f * t) + camera1OriginX,
                    Y = 8 * XMScalar.Cos(4.6f * t),
                    Z = 8 * XMScalar.Cos(1.6f * t)
                };

                this.secondarySpheres[1].Sphere = sphere;
            }

            // animate oriented box 1 around the aligned box
            {
                BoundingOrientedBox box = this.secondaryOrientedBoxes[1].Box;

                box.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(3.2f * t) + camera1OriginX,
                    Y = 8 * XMScalar.Cos(2.1f * t),
                    Z = 8 * XMScalar.Sin(1.6f * t)
                };

                box.Orientation = XMQuaternion.RotationRollPitchYaw(t * 0.7f, t * 1.3f, t);

                this.secondaryOrientedBoxes[1].Box = box;
            }

            // animate aligned box 1 around the aligned box
            {
                BoundingBox box = this.secondaryAABoxes[1].Box;

                box.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(1.1f * t) + camera1OriginX,
                    Y = 8 * XMScalar.Cos(5.8f * t),
                    Z = 8 * XMScalar.Cos(3.0f * t)
                };

                this.secondaryAABoxes[1].Box = box;
            }

            // animate sphere 2 around the oriented box
            {
                BoundingSphere sphere = this.secondarySpheres[2].Sphere;

                sphere.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(2.2f * t) + camera2OriginX,
                    Y = 8 * XMScalar.Cos(4.3f * t),
                    Z = 8 * XMScalar.Cos(1.8f * t)
                };

                this.secondarySpheres[2].Sphere = sphere;
            }

            // animate oriented box 2 around the oriented box
            {
                BoundingOrientedBox box = this.secondaryOrientedBoxes[2].Box;

                box.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(3.7f * t) + camera2OriginX,
                    Y = 8 * XMScalar.Cos(2.5f * t),
                    Z = 8 * XMScalar.Sin(1.1f * t)
                };

                box.Orientation = XMQuaternion.RotationRollPitchYaw(t * 0.9f, t * 1.8f, t);

                this.secondaryOrientedBoxes[2].Box = box;
            }

            // animate aligned box 2 around the oriented box
            {
                BoundingBox box = this.secondaryAABoxes[2].Box;

                box.Center = new XMFloat3
                {
                    X = 8 * XMScalar.Sin(1.3f * t) + camera2OriginX,
                    Y = 8 * XMScalar.Cos(5.2f * t),
                    Z = 8 * XMScalar.Cos(3.5f * t)
                };

                this.secondaryAABoxes[2].Box = box;
            }

            // triangle points in local space - equilateral triangle with radius of 2
            XMVector trianglePointA = new(0, 2, 0, 0);
            XMVector trianglePointB = new(1.732f, -1, 0, 0);
            XMVector trianglePointC = new(-1.732f, -1, 0, 0);

            XMMatrix triangleCoords;
            XMMatrix translation;

            // animate triangle 0 around the frustum
            triangleCoords = XMMatrix.RotationRollPitchYaw(t * 1.4f, t * 2.5f, t);
            translation = XMMatrix.Translation(
                5 * XMScalar.Sin(5.3f * t) + camera0OriginX,
                5 * XMScalar.Cos(2.3f * t),
                5 * XMScalar.Sin(3.4f * t));
            triangleCoords = XMMatrix.Multiply(triangleCoords, translation);
            this.secondaryTriangles[0].PointA = XMVector3.Transform(trianglePointA, triangleCoords);
            this.secondaryTriangles[0].PointB = XMVector3.Transform(trianglePointB, triangleCoords);
            this.secondaryTriangles[0].PointC = XMVector3.Transform(trianglePointC, triangleCoords);

            // animate triangle 1 around the aligned box
            triangleCoords = XMMatrix.RotationRollPitchYaw(t * 1.4f, t * 2.5f, t);
            translation = XMMatrix.Translation(
                8 * XMScalar.Sin(5.3f * t) + camera1OriginX,
                8 * XMScalar.Cos(2.3f * t),
                8 * XMScalar.Sin(3.4f * t));
            triangleCoords = XMMatrix.Multiply(triangleCoords, translation);
            this.secondaryTriangles[1].PointA = XMVector3.Transform(trianglePointA, triangleCoords);
            this.secondaryTriangles[1].PointB = XMVector3.Transform(trianglePointB, triangleCoords);
            this.secondaryTriangles[1].PointC = XMVector3.Transform(trianglePointC, triangleCoords);

            // animate triangle 2 around the oriented box
            triangleCoords = XMMatrix.RotationRollPitchYaw(t * 1.4f, t * 2.5f, t);
            translation = XMMatrix.Translation(
                8 * XMScalar.Sin(5.3f * t) + camera2OriginX,
                8 * XMScalar.Cos(2.3f * t),
                8 * XMScalar.Sin(3.4f * t));
            triangleCoords = XMMatrix.Multiply(triangleCoords, translation);
            this.secondaryTriangles[2].PointA = XMVector3.Transform(trianglePointA, triangleCoords);
            this.secondaryTriangles[2].PointB = XMVector3.Transform(trianglePointB, triangleCoords);
            this.secondaryTriangles[2].PointC = XMVector3.Transform(trianglePointC, triangleCoords);

            // animate primary ray (this is the only animated primary object)
            this.primaryRay.Direction = new XMVector(XMScalar.Sin(t * 3), 0, XMScalar.Cos(t * 3), 0);

            // animate sphere 3 around the ray
            {
                BoundingSphere sphere = this.secondarySpheres[3].Sphere;

                sphere.Center = new XMFloat3(camera3OriginX - 3, 0.5f * XMScalar.Sin(t * 5), camera3OriginZ);

                this.secondarySpheres[3].Sphere = sphere;
            }

            // animate aligned box 3 around the ray
            {
                BoundingBox box = this.secondaryAABoxes[3].Box;

                box.Center = new XMFloat3(camera3OriginX + 3, 0.5f * XMScalar.Sin(t * 4), camera3OriginZ);

                this.secondaryAABoxes[3].Box = box;
            }

            // animate oriented box 3 around the ray
            {
                BoundingOrientedBox box = this.secondaryOrientedBoxes[3].Box;

                box.Center = new XMFloat3(camera3OriginX, 0.5f * XMScalar.Sin(t * 4.5f), camera3OriginZ + 3);
                box.Orientation = XMQuaternion.RotationRollPitchYaw(t * 0.9f, t * 1.8f, t);

                this.secondaryOrientedBoxes[3].Box = box;
            }

            // animate triangle 3 around the ray
            triangleCoords = XMMatrix.RotationRollPitchYaw(t * 1.4f, t * 2.5f, t);
            translation = XMMatrix.Translation(
                camera3OriginX,
                0.5f * XMScalar.Cos(4.3f * t),
                camera3OriginZ - 3);
            triangleCoords = XMMatrix.Multiply(triangleCoords, translation);
            this.secondaryTriangles[3].PointA = XMVector3.Transform(trianglePointA, triangleCoords);
            this.secondaryTriangles[3].PointB = XMVector3.Transform(trianglePointB, triangleCoords);
            this.secondaryTriangles[3].PointC = XMVector3.Transform(trianglePointC, triangleCoords);
        }

        private void Collide()
        {
            // test collisions between objects and frustum
            this.secondarySpheres[0].Collision = this.primaryFrustum.Contains(this.secondarySpheres[0].Sphere);
            this.secondaryOrientedBoxes[0].Collision = this.primaryFrustum.Contains(this.secondaryOrientedBoxes[0].Box);
            this.secondaryAABoxes[0].Collision = this.primaryFrustum.Contains(this.secondaryAABoxes[0].Box);
            this.secondaryTriangles[0].Collision = this.primaryFrustum.Contains(this.secondaryTriangles[0].PointA, this.secondaryTriangles[0].PointB, this.secondaryTriangles[0].PointC);

            // test collisions between objects and aligned box
            this.secondarySpheres[1].Collision = this.primaryAABox.Contains(this.secondarySpheres[1].Sphere);
            this.secondaryOrientedBoxes[1].Collision = this.primaryAABox.Contains(this.secondaryOrientedBoxes[1].Box);
            this.secondaryAABoxes[1].Collision = this.primaryAABox.Contains(this.secondaryAABoxes[1].Box);
            this.secondaryTriangles[1].Collision = this.primaryAABox.Contains(this.secondaryTriangles[1].PointA, this.secondaryTriangles[1].PointB, this.secondaryTriangles[1].PointC);

            // test collisions between objects and oriented box
            this.secondarySpheres[2].Collision = this.primaryOrientedBox.Contains(this.secondarySpheres[2].Sphere);
            this.secondaryOrientedBoxes[2].Collision = this.primaryOrientedBox.Contains(this.secondaryOrientedBoxes[2].Box);
            this.secondaryAABoxes[2].Collision = this.primaryOrientedBox.Contains(this.secondaryAABoxes[2].Box);
            this.secondaryTriangles[2].Collision = this.primaryOrientedBox.Contains(this.secondaryTriangles[2].PointA, this.secondaryTriangles[2].PointB, this.secondaryTriangles[2].PointC);

            // test collisions between objects and ray
            float fDistance = -1.0f;
            float d;

            if (this.secondarySpheres[3].Sphere.Intersects(
                this.primaryRay.Origin,
                this.primaryRay.Direction,
                out d))
            {
                this.secondarySpheres[3].Collision = ContainmentType.Intersects;
                fDistance = d;
            }
            else
            {
                this.secondarySpheres[3].Collision = ContainmentType.Disjoint;
            }

            if (this.secondaryOrientedBoxes[3].Box.Intersects(
                this.primaryRay.Origin,
                this.primaryRay.Direction,
                out d))
            {
                this.secondaryOrientedBoxes[3].Collision = ContainmentType.Intersects;
                fDistance = d;
            }
            else
            {
                this.secondaryOrientedBoxes[3].Collision = ContainmentType.Disjoint;
            }

            if (this.secondaryAABoxes[3].Box.Intersects(
                this.primaryRay.Origin,
                this.primaryRay.Direction,
                out d))
            {
                this.secondaryAABoxes[3].Collision = ContainmentType.Intersects;
                fDistance = d;
            }
            else
            {
                this.secondaryAABoxes[3].Collision = ContainmentType.Disjoint;
            }

            if (TriangleTest.Intersects(
                this.primaryRay.Origin,
                this.primaryRay.Direction,
                this.secondaryTriangles[3].PointA,
                this.secondaryTriangles[3].PointB,
                this.secondaryTriangles[3].PointC,
                out d))
            {
                this.secondaryTriangles[3].Collision = ContainmentType.Intersects;
                fDistance = d;
            }
            else
            {
                this.secondaryTriangles[3].Collision = ContainmentType.Disjoint;
            }

            // If one of the ray intersection tests was successful, fDistance will be positive.
            // If so, compute the intersection location and store it in g_RayHitResultBox.
            if (fDistance > 0)
            {
                // The primary ray's direction is assumed to be normalized.
                XMVector hitLocation = XMVector.MultiplyAdd(
                    this.primaryRay.Direction,
                    XMVector.Replicate(fDistance),
                    this.primaryRay.Origin);

                BoundingBox box = this.rayHitResultBox.Box;
                box.Center = hitLocation;
                this.rayHitResultBox.Box = box;

                this.rayHitResultBox.Collision = ContainmentType.Intersects;
            }
            else
            {
                this.rayHitResultBox.Collision = ContainmentType.Disjoint;
            }
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new XMUByteN4(45, 50, 170, 255).ToVector());
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.InputAssemblerSetVertexBuffers(0, new[] { this.vertexBuffer }, new uint[] { 12 }, new uint[] { 0 });
            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R16UInt, 0);
            context.VertexShaderSetShader(this.vertexShader, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBuffer });

            this.RenderObjects();
        }

        private void RenderObjects()
        {
            // Set up some color constants
            XMVector ColorWhite = new XMUByteN4(255, 255, 255, 255);
            XMVector ColorGround = new XMUByteN4(0, 0, 0, 255);
            XMVector ColorYellow = new XMUByteN4(255, 255, 0, 255);

            // Draw ground planes
            for (int i = 0; i < CameraCount; i++)
            {
                XMFloat3 vXAxis = new(20, 0, 0);
                XMFloat3 vYAxis = new(0, 0, 20);
                XMFloat3 vOrigin = new(this.cameraOrigins[i].X, this.cameraOrigins[i].Y - 10.0f, this.cameraOrigins[i].Z);
                int iXDivisions = 20;
                int iYDivisions = 20;
                this.DrawGrid(vXAxis, vYAxis, vOrigin, iXDivisions, iYDivisions, ColorGround);
            }

            // Draw primary collision objects in white
            this.DrawFrustum(this.primaryFrustum, ColorWhite);
            this.DrawAabb(this.primaryAABox, ColorWhite);
            this.DrawObb(this.primaryOrientedBox, ColorWhite);

            {
                XMVector origin = primaryRay.Origin;
                XMVector direction = this.primaryRay.Direction;
                this.DrawRay(origin, direction.Scale(10.0f), false, new XMUByteN4(80, 80, 80, 255));
                this.DrawRay(origin, direction, false, ColorWhite);
            }

            // Draw secondary collision objects in colors based on collision results
            for (int i = 0; i < GroupCount; i++)
            {
                CollisionSphere sphere = this.secondarySpheres[i];
                this.DrawSphere(sphere.Sphere, GetCollisionColor(sphere.Collision));

                CollisionBox obox = this.secondaryOrientedBoxes[i];
                this.DrawObb(obox.Box, GetCollisionColor(obox.Collision));

                CollisionAABox aabox = this.secondaryAABoxes[i];
                this.DrawAabb(aabox.Box, GetCollisionColor(aabox.Collision));

                CollisionTriangle tri = this.secondaryTriangles[i];
                this.DrawTriangle(tri.PointA, tri.PointB, tri.PointC, GetCollisionColor(tri.Collision));
            }

            // Draw results of ray-object intersection, if there was a hit this frame
            if (this.rayHitResultBox.Collision != ContainmentType.Disjoint)
            {
                this.DrawAabb(this.rayHitResultBox.Box, ColorYellow);
            }
        }

        private static XMVector GetCollisionColor(ContainmentType collision)
        {
            XMVector ColorCollide = new XMUByteN4(255, 0, 0, 255);
            XMVector ColorPartialCollide = new XMUByteN4(255, 255, 0, 255);
            XMVector ColorNoCollide = new XMUByteN4(128, 192, 128, 255);

            return collision switch
            {
                ContainmentType.Disjoint => ColorNoCollide,
                ContainmentType.Intersects => ColorPartialCollide,
                _ => ColorCollide,
            };
        }

        private void DrawTriangle(in XMFloat3 pointA, in XMFloat3 pointB, in XMFloat3 pointC, in XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            var vertices = new XMFloat3[4];
            vertices[0] = pointA;
            vertices[1] = pointB;
            vertices[2] = pointC;
            vertices[3] = pointA;

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineStrip);
            context.Draw((uint)vertices.Length, 0);
        }

        private void DrawRay(in XMFloat3 origin, in XMFloat3 direction, bool normalize, in XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            var vertices = new XMFloat3[3];
            vertices[0] = origin;

            XMVector rayOrigin = origin;
            XMVector rayDirection = direction;
            XMVector normDirection = XMVector3.Normalize(rayDirection);

            if (normalize)
            {
                rayDirection = normDirection;
            }

            vertices[1] = XMVector.Add(rayDirection, rayOrigin);

            XMVector crossVector = new(0, 1, 0, 0);
            XMVector perpVector = XMVector3.Cross(normDirection, crossVector);

            if (XMVector3.Equal(XMVector3.LengthSquare(perpVector), XMVector.Zero))
            {
                crossVector = new XMVector(0, 0, 1, 0);
                perpVector = XMVector3.Cross(normDirection, crossVector);
            }

            perpVector = XMVector3.Normalize(perpVector);

            perpVector = perpVector.Scale(0.0625f);
            normDirection = normDirection.Scale(-0.25f);
            rayDirection = XMVector.Add(perpVector, rayDirection);
            rayDirection = XMVector.Add(normDirection, rayDirection);

            vertices[2] = XMVector.Add(rayDirection, rayOrigin);

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineStrip);
            context.Draw((uint)vertices.Length, 0);
        }

        private void DrawRing(in XMFloat3 origin, in XMFloat3 majorAxis, in XMFloat3 minorAxis, in XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            const int RingSegments = 32;
            var vertices = new XMFloat3[RingSegments + 1];

            XMVector vOrigin = origin;
            XMVector vMajor = majorAxis;
            XMVector vMinor = minorAxis;

            float fAngleDelta = XMMath.TwoPI / RingSegments;

            // Instead of calling cos/sin for each segment we calculate
            // the sign of the angle delta and then incrementally calculate sin
            // and cosine from then on.
            XMVector cosDelta = XMVector.Replicate(XMScalar.Cos(fAngleDelta));
            XMVector sinDelta = XMVector.Replicate(XMScalar.Sin(fAngleDelta));
            XMVector incrementalSin = XMVector.Zero;

            XMVector incrementalCos = XMVector.One;

            for (int i = 0; i < RingSegments; i++)
            {
                XMVector pos;
                pos = XMVector.MultiplyAdd(vMajor, incrementalCos, vOrigin);
                pos = XMVector.MultiplyAdd(vMinor, incrementalSin, pos);
                vertices[i] = pos;

                // Standard formula to rotate a vector.
                XMVector newCos = incrementalCos * cosDelta - incrementalSin * sinDelta;
                XMVector newSin = incrementalCos * sinDelta + incrementalSin * cosDelta;
                incrementalCos = newCos;
                incrementalSin = newSin;
            }

            vertices[RingSegments] = vertices[0];

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineStrip);
            context.Draw((uint)vertices.Length, 0);
        }

        private void DrawSphere(in BoundingSphere sphere, in XMVector color)
        {
            XMFloat3 origin = sphere.Center;
            float radius = sphere.Radius;

            this.DrawRing(origin, new XMFloat3(radius, 0, 0), new XMFloat3(0, 0, radius), color);
            this.DrawRing(origin, new XMFloat3(radius, 0, 0), new XMFloat3(0, radius, 0), color);
            this.DrawRing(origin, new XMFloat3(0, radius, 0), new XMFloat3(0, 0, radius), color);
        }

        private void DrawCube(in XMMatrix world, in XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            var cubeVertices = new XMVector[8]
            {
                new XMVector(-1, -1, -1, 0),
                new XMVector(1, -1, -1, 0),
                new XMVector(1, -1, 1, 0),
                new XMVector(-1, -1, 1, 0),
                new XMVector(-1, 1, -1, 0),
                new XMVector(1, 1, -1, 0),
                new XMVector(1, 1, 1, 0),
                new XMVector(-1, 1, 1, 0)
            };

            var indices = new ushort[24]
            {
                0, 1,
                1, 2,
                2, 3,
                3, 0,
                4, 5,
                5, 6,
                6, 7,
                7, 4,
                0, 4,
                1, 5,
                2, 6,
                3, 7
            };

            var vertices = new XMFloat3[cubeVertices.Length];
            for (int i = 0; i < cubeVertices.Length; i++)
            {
                vertices[i] = XMVector3.Transform(cubeVertices[i], world);
            }

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            Debug.Assert(indices.Length <= MaxIndices);
            context.UpdateSubresource(this.indexBuffer, 0, new D3D11Box(0, 0, 0, (uint)indices.Length * 2, 1, 1), indices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineList);
            context.DrawIndexed((uint)indices.Length, 0, 0);
        }

        private void DrawAabb(in BoundingBox box, in XMVector color)
        {
            XMMatrix matWorld = XMMatrix.ScalingFromVector(box.Extents)
                * XMMatrix.TranslationFromVector(box.Center);

            this.DrawCube(matWorld, color);
        }

        private void DrawObb(in BoundingOrientedBox obb, in XMVector color)
        {
            XMMatrix matWorld = XMMatrix.ScalingFromVector(obb.Extents)
                * XMMatrix.RotationQuaternion(obb.Orientation)
                * XMMatrix.TranslationFromVector(obb.Center);

            this.DrawCube(matWorld, color);
        }

        private void DrawFrustum(in BoundingFrustum frustum, in XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            XMVector origin = frustum.Origin;
            float near = frustum.Near;
            float far = frustum.Far;
            float rightSlope = frustum.RightSlope;
            float leftSlope = frustum.LeftSlope;
            float topSlope = frustum.TopSlope;
            float bottomSlope = frustum.BottomSlope;

            var cornerPoints = new XMFloat3[8]
            {
                new XMFloat3(rightSlope * near, topSlope * near, near),
                new XMFloat3(leftSlope * near, topSlope * near, near),
                new XMFloat3(leftSlope * near, bottomSlope * near, near),
                new XMFloat3(rightSlope * near, bottomSlope * near, near),
                new XMFloat3(rightSlope * far, topSlope * far, far),
                new XMFloat3(leftSlope * far, topSlope * far, far),
                new XMFloat3(leftSlope * far, bottomSlope * far, far),
                new XMFloat3(rightSlope * far, bottomSlope * far, far)
            };

            XMVector orientation = frustum.Orientation;
            XMMatrix mat = XMMatrix.RotationQuaternion(orientation);

            for (int i = 0; i < 8; i++)
            {
                cornerPoints[i] = XMVector3.Transform(cornerPoints[i], mat) + origin;
            }

            var vertices = new XMFloat3[12 * 2]
            {
                cornerPoints[0], cornerPoints[1],
                cornerPoints[1], cornerPoints[2],
                cornerPoints[2], cornerPoints[3],
                cornerPoints[3], cornerPoints[0],
                cornerPoints[0], cornerPoints[4],
                cornerPoints[1], cornerPoints[5],
                cornerPoints[2], cornerPoints[6],
                cornerPoints[3], cornerPoints[7],
                cornerPoints[4], cornerPoints[5],
                cornerPoints[5], cornerPoints[6],
                cornerPoints[6], cornerPoints[7],
                cornerPoints[7], cornerPoints[4]
            };

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineList);
            context.Draw((uint)vertices.Length, 0);
        }

        private void DrawGrid(in XMFloat3 xAxis, in XMFloat3 yAxis, in XMFloat3 origin, int iXDivisions, int iYDivisions, XMVector color)
        {
            var context = this.deviceResources.D3DContext;

            iXDivisions = Math.Max(1, iXDivisions);
            iYDivisions = Math.Max(1, iYDivisions);

            int iLineCount = iXDivisions + iYDivisions + 2;

            var vertices = new XMFloat3[iLineCount * 2];

            XMVector vX = xAxis;
            XMVector vY = yAxis;
            XMVector vOrigin = origin;

            for (int i = 0; i <= iXDivisions; i++)
            {
                float fPercent = (float)i / iXDivisions;
                fPercent = (fPercent * 2.0f) - 1.0f;
                XMVector vScale = vX.Scale(fPercent) + vOrigin;
                vertices[i * 2] = vScale - vY;
                vertices[i * 2 + 1] = vScale + vY;
            }

            int iStartIndex = (iXDivisions + 1) * 2;

            for (int i = 0; i <= iYDivisions; i++)
            {
                float fPercent = (float)i / iYDivisions;
                fPercent = (fPercent * 2.0f) - 1.0f;
                XMVector vScale = vY.Scale(fPercent) + vOrigin;
                vertices[i * 2 + iStartIndex] = vScale - vX;
                vertices[i * 2 + 1 + iStartIndex] = vScale + vX;
            }

            Debug.Assert(vertices.Length <= MaxVertices);
            context.UpdateSubresource(this.vertexBuffer, 0, new D3D11Box(0, 0, 0, (uint)vertices.Length * 12, 1, 1), vertices, 0, 0);

            var cb = new ConstantBufferData
            {
                WorldViewProjection = (this.worldMatrix * this.viewMatrix * this.projectionMatrix).Transpose(),
                Color = color
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, cb, 0, 0);

            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.LineList);
            context.Draw((uint)vertices.Length, 0);
        }
    }
}
