using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.D3DCompiler;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PNTriangles11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private static readonly int MeshTypeMax = Enum.GetValues<MeshType>().Length;

        private D3D11InputLayout g_pSceneVertexLayout;

        // The scene meshes 
        private readonly SdkMeshFile[] g_SceneMesh = new SdkMeshFile[MeshTypeMax];
        private readonly XMMatrix[] g_m4x4MeshMatrix = new XMMatrix[MeshTypeMax];
        private readonly XMFloat3[] g_v3AdaptiveTessParams = new XMFloat3[MeshTypeMax];

        // Samplers
        private D3D11SamplerState g_pSamplePoint;
        private D3D11SamplerState g_pSampleLinear;

        // Shaders
        private D3D11VertexShader g_pSceneVS;
        private D3D11VertexShader g_pSceneWithTessellationVS;
        private D3D11HullShader g_pPNTrianglesHS;
        private D3D11DomainShader g_pPNTrianglesDS;
        private D3D11PixelShader g_pScenePS;
        private D3D11PixelShader g_pTexturedScenePS;

        private uint g_iPNTRIANGLESCBBind = 0;

        // Various Constant buffers
        private D3D11Buffer g_pcbPNTriangles;

        // State objects
        private D3D11RasterizerState g_pRasterizerStateWireframe;
        private D3D11RasterizerState g_pRasterizerStateSolid;

        private string g_HS_PNTrianglesContent;

        public MainGameComponent()
        {
            // Setup the light camera
            this.LightCameraAt = new(0.0f, 0.0f, 0.0f);
            this.LightCameraEye = new(0.0f, -1.0f, -1.0f);

            // Setup the camera for each scene
            XMFloat3 vecUp = new(0.0f, 1.0f, 0.0f);
            XMFloat3 vecAt = new(0.0f, 0.0f, 0.0f);

            // Tiny
            this.CameraEye[(int)MeshType.Tiny] = new(0.0f, 0.0f, -700.0f);
            this.CameraAt[(int)MeshType.Tiny] = vecAt;

            // Tiger
            this.CameraEye[(int)MeshType.Tiger] = new(0.0f, 0.0f, -4.0f);
            this.CameraAt[(int)MeshType.Tiger] = vecAt;

            // Teapot
            this.CameraEye[(int)MeshType.Teapot] = new(0.0f, 0.0f, -4.0f);
            this.CameraAt[(int)MeshType.Teapot] = vecAt;

            for (int i = 0; i < MeshTypeMax; i++)
            {
                this.ViewMatrix[i] = XMMatrix.LookAtLH(this.CameraEye[i], this.CameraAt[i], vecUp);
                this.WorldMatrix[i] = XMMatrix.Identity;
            }
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel110;

        public bool SettingsChanged { get; set; } = false;

        public bool RecreateHullShader { get; set; } = false;

        public XMFloat3 LightCameraEye { get; set; }

        public XMFloat3 LightCameraAt { get; set; }

        public XMFloat3[] CameraEye { get; } = new XMFloat3[MeshTypeMax];

        public XMFloat3[] CameraAt { get; } = new XMFloat3[MeshTypeMax];

        public XMMatrix[] ViewMatrix { get; } = new XMMatrix[MeshTypeMax];

        public XMMatrix[] WorldMatrix { get; } = new XMMatrix[MeshTypeMax];

        public XMMatrix[] ProjectionMatrix { get; } = new XMMatrix[MeshTypeMax];

        public MeshType MeshType { get; set; } = MeshType.Tiny;

        // Tess factor
        public int TessFactor { get; set; } = 5;

        // Back face culling epsilon
        public float BackFaceCullEpsilon { get; set; } = 0.5f;

        // Silhoutte epsilon
        public float SilhoutteEpsilon { get; set; } = 0.25f;

        // Range scale (for distance adaptive tessellation)
        public float RangeScale { get; set; } = 1.0f;

        // Edge scale (for screen space adaptive tessellation)
        public int EdgeSize { get; set; } = 16;

        // Edge scale (for screen space adaptive tessellation)
        public float ResolutionScale { get; set; } = 1.0f;

        // View frustum culling epsilon
        public float ViewFrustumCullEpsilon { get; set; } = 0.5f;

        public bool IsWireframe { get; set; } = false;

        public bool IsTextured { get; set; } = true;

        public bool IsTessellation { get; set; } = true;

        public bool IsBackFaceCull { get; set; } = false;

        public bool IsViewFrustumCull { get; set; } = false;

        public bool IsScreenSpaceAdaptive { get; set; } = false;

        public bool IsDistanceAdaptive { get; set; } = false;

        public bool IsScreenResolutionAdaptive { get; set; } = false;

        public bool IsOrientationAdaptive { get; set; } = false;

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            // Main scene VS (no tessellation)
            this.g_pSceneVS = device.CreateVertexShader(File.ReadAllBytes("VS_RenderScene.cso"), null);
            this.g_pSceneVS.SetDebugName("VS_RenderScene");

            // Define our scene vertex data layout
            D3D11InputElementDesc[] SceneLayout = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0),
            };

            this.g_pSceneVertexLayout = device.CreateInputLayout(SceneLayout, File.ReadAllBytes("VS_RenderScene.cso"));
            this.g_pSceneVertexLayout.SetDebugName("Primary");

            // Main scene VS (with tessellation)
            this.g_pSceneWithTessellationVS = device.CreateVertexShader(File.ReadAllBytes("VS_RenderSceneWithTessellation.cso"), null);
            this.g_pSceneWithTessellationVS.SetDebugName("VS_RenderSceneWithTessellation");

            // PNTriangles HS
            this.CreateHullShader();

            // PNTriangles DS
            this.g_pPNTrianglesDS = device.CreateDomainShader(File.ReadAllBytes("DS_PNTriangles.cso"), null);
            this.g_pPNTrianglesDS.SetDebugName("DS_PNTriangles");

            // Main scene PS (no textures)
            this.g_pScenePS = device.CreatePixelShader(File.ReadAllBytes("PS_RenderScene.cso"), null);
            this.g_pScenePS.SetDebugName("PS_RenderScene");

            // Main scene PS (textured)
            this.g_pTexturedScenePS = device.CreatePixelShader(File.ReadAllBytes("PS_RenderSceneTextured.cso"), null);
            this.g_pTexturedScenePS.SetDebugName("PS_RenderSceneTextured");

            // Setup constant buffer
            D3D11BufferDesc Desc = new(PNTrianglesConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
            this.g_pcbPNTriangles = device.CreateBuffer(Desc);
            this.g_pcbPNTriangles.SetDebugName("CB_PNTRIANGLES");

            // Setup the mesh params for adaptive tessellation
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiny].X = 1.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiny].Y = 1000.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiny].Z = 700.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].X = 1.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].Y = 10.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].Z = 4.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].X = 1.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].Y = 10.0f;
            this.g_v3AdaptiveTessParams[(int)MeshType.Tiger].Z = 4.0f;

            // Setup the matrix for each mesh
            // Tiny
            this.g_m4x4MeshMatrix[(int)MeshType.Tiny] = XMMatrix.RotationX(-XMMath.PI / 2) * XMMatrix.RotationY(XMMath.PI);
            // Tiger
            this.g_m4x4MeshMatrix[(int)MeshType.Tiger] = XMMatrix.RotationX(-XMMath.PI / 36) * XMMatrix.RotationY(XMMath.PI / 4);
            // Teapot
            this.g_m4x4MeshMatrix[(int)MeshType.Teapot] = XMMatrix.Identity;

            // Load the standard scene meshes
            this.g_SceneMesh[(int)MeshType.Tiny] = SdkMeshFile.FromFile(device, context, "tiny\\tiny.sdkmesh");
            this.g_SceneMesh[(int)MeshType.Tiger] = SdkMeshFile.FromFile(device, context, "tiger\\tiger.sdkmesh");
            this.g_SceneMesh[(int)MeshType.Teapot] = SdkMeshFile.FromFile(device, context, "teapot\\teapot.sdkmesh");

            // Create sampler states for point and linear
            D3D11SamplerDesc SamDesc = new(
                D3D11Filter.MinMagMipPoint,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                D3D11TextureAddressMode.Clamp,
                0.0f,
                1,
                D3D11ComparisonFunction.Always,
                null,
                0.0f,
                float.MaxValue
                );

            // Point
            this.g_pSamplePoint = device.CreateSamplerState(SamDesc);
            this.g_pSamplePoint.SetDebugName("Point");

            // Linear
            SamDesc.Filter = D3D11Filter.MinMagMipLinear;
            SamDesc.AddressU = D3D11TextureAddressMode.Wrap;
            SamDesc.AddressV = D3D11TextureAddressMode.Wrap;
            SamDesc.AddressW = D3D11TextureAddressMode.Wrap;
            this.g_pSampleLinear = device.CreateSamplerState(SamDesc);
            this.g_pSampleLinear.SetDebugName("Linear");

            // Set the raster state
            D3D11RasterizerDesc RasterizerDesc = new(
                D3D11FillMode.WireFrame,
                D3D11CullMode.None,
                false,
                0,
                0.0f,
                0.0f,
                true,
                false,
                false,
                false
                );

            // Wireframe
            this.g_pRasterizerStateWireframe = device.CreateRasterizerState(RasterizerDesc);
            this.g_pRasterizerStateWireframe.SetDebugName("Wireframe");

            // Solid
            RasterizerDesc.FillMode = D3D11FillMode.Solid;
            this.g_pRasterizerStateSolid = device.CreateRasterizerState(RasterizerDesc);
            this.g_pRasterizerStateSolid.SetDebugName("Solid");
        }

        private void CreateHullShader()
        {
            var device = this.deviceResources.D3DDevice;

            // Release any existing shader
            D3D11Utils.DisposeAndNull(ref this.g_pPNTrianglesHS);

            // Create the shaders
            D3DShaderMacro[] ShaderMacros = new[]
            {
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
                new D3DShaderMacro(null, "1"),
            };

            int i = 0;
            uint switches = 0;

            // Back face culling
            if (this.IsBackFaceCull)
            {
                ShaderMacros[i].Name = "USE_BACK_FACE_CULLING";
                i++;
                switches |= 0x1;
            }

            // View frustum culling
            if (this.IsViewFrustumCull)
            {
                ShaderMacros[i].Name = "USE_VIEW_FRUSTUM_CULLING";
                i++;
                switches |= 0x2;
            }

            // Screen space adaptive tessellation
            if (this.IsScreenSpaceAdaptive)
            {
                ShaderMacros[i].Name = "USE_SCREEN_SPACE_ADAPTIVE_TESSELLATION";
                i++;
                switches |= 0x4;
            }

            // Distance adaptive tessellation (with screen resolution scaling)
            if (this.IsDistanceAdaptive)
            {
                ShaderMacros[i].Name = "USE_DISTANCE_ADAPTIVE_TESSELLATION";
                i++;
                switches |= 0x8;
            }

            // Screen resolution adaptive tessellation
            if (this.IsScreenResolutionAdaptive)
            {
                ShaderMacros[i].Name = "USE_SCREEN_RESOLUTION_ADAPTIVE_TESSELLATION";
                i++;
                switches |= 0x10;
            }

            // Orientation adaptive tessellation
            if (this.IsOrientationAdaptive)
            {
                ShaderMacros[i].Name = "USE_ORIENTATION_ADAPTIVE_TESSELLATION";
                i++;
                switches |= 0x20;
            }

            if (this.g_HS_PNTrianglesContent is null)
            {
                string AdaptiveTessellationContent = File.ReadAllText("AdaptiveTessellation.hlsl");

                this.g_HS_PNTrianglesContent = File.ReadAllText("HS_PNTriangles.hlsl")
                    .Replace("#include \"AdaptiveTessellation.hlsl\"", AdaptiveTessellationContent);
            }

            D3DCompile.Compile(
                this.g_HS_PNTrianglesContent,
                "HS_PNTriangles",
                ShaderMacros,
                "HS_PNTriangles",
                "hs_5_0",
                D3DCompileOptions.OptimizationLevel3 | D3DCompileOptions.WarningsAreErrors,
                out byte[] blob,
                out _);

            this.g_pPNTrianglesHS = device.CreateHullShader(blob, null);
            this.g_pPNTrianglesHS.SetDebugName($"Hull (GUI settings {switches:X8})");
        }

        public void ReleaseDeviceDependentResources()
        {
            for (int iMeshType = 0; iMeshType < this.g_SceneMesh.Length; iMeshType++)
            {
                this.g_SceneMesh[iMeshType]?.Release();
                this.g_SceneMesh[iMeshType] = null;
            }

            D3D11Utils.DisposeAndNull(ref this.g_pSceneVS);
            D3D11Utils.DisposeAndNull(ref this.g_pSceneWithTessellationVS);
            D3D11Utils.DisposeAndNull(ref this.g_pPNTrianglesHS);
            D3D11Utils.DisposeAndNull(ref this.g_pPNTrianglesDS);
            D3D11Utils.DisposeAndNull(ref this.g_pScenePS);
            D3D11Utils.DisposeAndNull(ref this.g_pTexturedScenePS);

            D3D11Utils.DisposeAndNull(ref this.g_pcbPNTriangles);

            D3D11Utils.DisposeAndNull(ref this.g_pSceneVertexLayout);

            D3D11Utils.DisposeAndNull(ref this.g_pSamplePoint);
            D3D11Utils.DisposeAndNull(ref this.g_pSampleLinear);

            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateWireframe);
            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateSolid);
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            XMMatrix projectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 1.0f, 5000.0f);

            for (int i = 0; i < MeshTypeMax; i++)
            {
                this.ProjectionMatrix[i] = projectionMatrix;
            }
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            if (this.RecreateHullShader)
            {
                this.RecreateHullShader = false;
                this.CreateHullShader();
            }

            this.SettingsChanged = false;
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            // Clear the render target & depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.176f, 0.196f, 0.667f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            if (this.SettingsChanged)
            {
                return;
            }

            int meshTypeIndex = (int)this.MeshType;

            // Array of our samplers
            D3D11SamplerState[] ppSamplerStates = new[] { this.g_pSamplePoint, this.g_pSampleLinear };

            // Get the projection & view matrix from the camera class
            XMMatrix mWorld = this.g_m4x4MeshMatrix[meshTypeIndex] * this.WorldMatrix[meshTypeIndex];
            XMMatrix mView = this.ViewMatrix[meshTypeIndex];
            XMMatrix mProj = this.ProjectionMatrix[meshTypeIndex];
            XMMatrix mWorldViewProjection = mWorld * mView * mProj;
            XMMatrix mViewProjection = mView * mProj;

            // Get the direction of the light.
            XMVector v3LightDir = XMVector3.Normalize(this.LightCameraEye.ToVector() - this.LightCameraAt.ToVector());

            // Get the view vector.
            XMVector v3ViewVector = XMVector3.Normalize(this.CameraEye[meshTypeIndex].ToVector() - this.CameraAt[meshTypeIndex].ToVector());

            // Calculate the plane equations of the frustum in world space
            XMVector[] f4ViewFrustumPlanes = new XMVector[6];
            ExtractPlanesFromFrustum(f4ViewFrustumPlanes, mViewProjection);

            // Setup the constant buffer for the scene vertex shader
            PNTrianglesConstantBufferData pPNTrianglesCB = new()
            {
                f4x4World = mWorld.Transpose(),
                f4x4ViewProjection = mViewProjection.Transpose(),
                f4x4WorldViewProjection = mWorldViewProjection.Transpose(),
                fLightDir = new XMVector(v3LightDir.X, v3LightDir.Y, v3LightDir.Z, 0.0f),
                fEye = this.CameraEye[meshTypeIndex],
                fViewVector = v3ViewVector,
                fTessFactors = new XMVector(this.TessFactor, this.TessFactor, this.g_v3AdaptiveTessParams[meshTypeIndex].X, this.g_v3AdaptiveTessParams[meshTypeIndex].Y),
                fScreenParams = new XMVector(this.deviceResources.BackBufferWidth, this.deviceResources.BackBufferHeight, 0.0f, 0.0f),
                fGUIParams1 = new XMVector(this.BackFaceCullEpsilon, this.SilhoutteEpsilon > 0.99f ? 0.99f : this.SilhoutteEpsilon, this.RangeScale, this.EdgeSize),
                fGUIParams2 = new XMVector(this.ResolutionScale, ((this.ViewFrustumCullEpsilon * 2.0f) - 1.0f) * this.g_v3AdaptiveTessParams[meshTypeIndex].Z, 0.0f, 0.0f),
                f4ViewFrustumPlanes = f4ViewFrustumPlanes
            };

            context.UpdateSubresource(this.g_pcbPNTriangles, 0, null, pPNTrianglesCB, 0, 0);
            context.VertexShaderSetConstantBuffers(this.g_iPNTRIANGLESCBBind, new[] { this.g_pcbPNTriangles });
            context.PixelShaderSetConstantBuffers(this.g_iPNTRIANGLESCBBind, new[] { this.g_pcbPNTriangles });

            // Based on app and GUI settings set a bunch of bools that guide the render
            bool bTessellation = this.IsTessellation;
            bool bTextured = this.IsTextured;

            // VS
            if (bTessellation)
            {
                context.VertexShaderSetShader(this.g_pSceneWithTessellationVS, null);
            }
            else
            {
                context.VertexShaderSetShader(this.g_pSceneVS, null);
            }

            context.InputAssemblerSetInputLayout(this.g_pSceneVertexLayout);

            // HS
            if (bTessellation)
            {
                context.HullShaderSetConstantBuffers(this.g_iPNTRIANGLESCBBind, new[] { this.g_pcbPNTriangles });
                context.HullShaderSetShader(this.g_pPNTrianglesHS, null);
            }
            else
            {
                context.HullShaderSetShader(null, null);
            }

            // DS
            if (bTessellation)
            {
                context.DomainShaderSetConstantBuffers(this.g_iPNTRIANGLESCBBind, new[] { this.g_pcbPNTriangles });
                context.DomainShaderSetShader(this.g_pPNTrianglesDS, null);
            }
            else
            {
                context.DomainShaderSetShader(null, null);
            }

            // GS
            context.GeometryShaderSetShader(null, null);

            // PS
            if (bTextured)
            {
                context.PixelShaderSetSamplers(0, ppSamplerStates);
                //context.PixelShaderSetShaderResources(0, new[] { this.g_pDiffuseTextureSRV });
                context.PixelShaderSetShader(this.g_pTexturedScenePS, null);
            }
            else
            {
                context.PixelShaderSetShader(this.g_pScenePS, null);
            }


            // Set the rasterizer state
            if (this.IsWireframe)
            {
                context.RasterizerStageSetState(this.g_pRasterizerStateWireframe);
            }
            else
            {
                context.RasterizerStageSetState(this.g_pRasterizerStateSolid);
            }

            // Render the scene and optionally override the mesh topology and diffuse texture slot
            uint uDiffuseSlot = 0;

            // Decide which prim topology to use
            D3D11PrimitiveTopology PrimitiveTopology = D3D11PrimitiveTopology.Undefined;

            if (bTessellation)
            {
                PrimitiveTopology = D3D11PrimitiveTopology.PatchList3ControlPoint;
            }

            // Render the meshes    
            //this.g_SceneMesh[meshTypeIndex].Render(0, -1, -1);

            for (int iMesh = 0; iMesh < this.g_SceneMesh[meshTypeIndex].Meshes.Count; iMesh++)
            {
                this.RenderMesh(this.g_SceneMesh[meshTypeIndex], iMesh, PrimitiveTopology, uDiffuseSlot);
            }
        }

        /// <summary>
        /// Helper function that allows the app to render individual meshes of an sdkmesh
        /// and override the primitive topology
        /// </summary>
        private void RenderMesh(SdkMeshFile pMeshFile, int uMesh, D3D11PrimitiveTopology PrimType, uint uDiffuseSlot = uint.MaxValue, uint uNormalSlot = uint.MaxValue, uint uSpecularSlot = uint.MaxValue)
        {
            var context = this.deviceResources.D3DContext;

            SdkMeshMesh pMesh = pMeshFile.Meshes[uMesh];

            if (pMesh.VertexBuffers.Length > D3D11Constants.InputAssemblerVertexInputResourceSlotCount)
            {
                return;
            }

            D3D11Buffer[] pVB = new D3D11Buffer[pMesh.VertexBuffers.Length];
            uint[] Strides = new uint[pMesh.VertexBuffers.Length];
            uint[] Offsets = new uint[pMesh.VertexBuffers.Length];

            for (int i = 0; i < pMesh.VertexBuffers.Length; i++)
            {
                pVB[i] = pMesh.VertexBuffers[i].Buffer;
                Strides[i] = pMesh.VertexBuffers[i].StrideBytes;
                Offsets[i] = 0;
            }

            D3D11Buffer pIB = pMesh.IndexBuffer.Buffer;
            DxgiFormat ibFormat = pMesh.IndexBuffer.IndexFormat;

            context.InputAssemblerSetVertexBuffers(0, pVB, Strides, Offsets);
            context.InputAssemblerSetIndexBuffer(pIB, ibFormat, 0);

            for (int uSubset = 0; uSubset < pMesh.Subsets.Count; uSubset++)
            {
                SdkMeshSubset pSubset = pMesh.Subsets[uSubset];

                if (PrimType == D3D11PrimitiveTopology.Undefined)
                {
                    PrimType = pSubset.PrimitiveTopology;
                }

                context.InputAssemblerSetPrimitiveTopology(PrimType);

                SdkMeshMaterial pMat = pMeshFile.Materials[pSubset.MaterialIndex];

                if (uDiffuseSlot != uint.MaxValue && pMat.DiffuseTextureView != null)
                {
                    context.PixelShaderSetShaderResources(uDiffuseSlot, new[] { pMat.DiffuseTextureView });
                }

                if (uNormalSlot != uint.MaxValue && pMat.NormalTextureView != null)
                {
                    context.PixelShaderSetShaderResources(uNormalSlot, new[] { pMat.NormalTextureView });
                }

                if (uSpecularSlot != uint.MaxValue && pMat.SpecularTextureView != null)
                {
                    context.PixelShaderSetShaderResources(uSpecularSlot, new[] { pMat.SpecularTextureView });
                }

                uint IndexCount = (uint)pSubset.IndexCount;
                uint IndexStart = (uint)pSubset.IndexStart;
                int VertexStart = pSubset.VertexStart;

                context.DrawIndexed(IndexCount, IndexStart, VertexStart);
            }
        }

        /// <summary>
        /// Helper function to normalize a plane
        /// </summary>
        /// <param name="pPlaneEquation"></param>
        private static void NormalizePlane(ref XMVector pPlaneEquation)
        {
            float mag = (float)Math.Sqrt(
                pPlaneEquation.X * pPlaneEquation.X
                + pPlaneEquation.Y * pPlaneEquation.Y
                + pPlaneEquation.Z * pPlaneEquation.Z);

            pPlaneEquation.X = pPlaneEquation.X / mag;
            pPlaneEquation.Y = pPlaneEquation.Y / mag;
            pPlaneEquation.Z = pPlaneEquation.Z / mag;
            pPlaneEquation.W = pPlaneEquation.W / mag;
        }

        /// <summary>
        /// Extract all 6 plane equations from frustum denoted by supplied matrix
        /// </summary>
        /// <param name="pPlaneEquation"></param>
        /// <param name=""></param>
        private static void ExtractPlanesFromFrustum(XMVector[] pPlaneEquation, XMMatrix pMatrix)
        {
            // Left clipping plane
            pPlaneEquation[0].X = pMatrix.M14 + pMatrix.M11;
            pPlaneEquation[0].Y = pMatrix.M24 + pMatrix.M21;
            pPlaneEquation[0].Z = pMatrix.M34 + pMatrix.M31;
            pPlaneEquation[0].W = pMatrix.M44 + pMatrix.M41;

            // Right clipping plane
            pPlaneEquation[1].X = pMatrix.M14 - pMatrix.M11;
            pPlaneEquation[1].Y = pMatrix.M24 - pMatrix.M21;
            pPlaneEquation[1].Z = pMatrix.M34 - pMatrix.M31;
            pPlaneEquation[1].W = pMatrix.M44 - pMatrix.M41;

            // Top clipping plane
            pPlaneEquation[2].X = pMatrix.M14 - pMatrix.M12;
            pPlaneEquation[2].Y = pMatrix.M24 - pMatrix.M22;
            pPlaneEquation[2].Z = pMatrix.M34 - pMatrix.M32;
            pPlaneEquation[2].W = pMatrix.M44 - pMatrix.M42;

            // Bottom clipping plane
            pPlaneEquation[3].X = pMatrix.M14 + pMatrix.M12;
            pPlaneEquation[3].Y = pMatrix.M24 + pMatrix.M22;
            pPlaneEquation[3].Z = pMatrix.M34 + pMatrix.M32;
            pPlaneEquation[3].W = pMatrix.M44 + pMatrix.M42;

            // Near clipping plane
            pPlaneEquation[4].X = pMatrix.M13;
            pPlaneEquation[4].Y = pMatrix.M23;
            pPlaneEquation[4].Z = pMatrix.M33;
            pPlaneEquation[4].W = pMatrix.M43;

            // Far clipping plane
            pPlaneEquation[5].X = pMatrix.M14 - pMatrix.M13;
            pPlaneEquation[5].Y = pMatrix.M24 - pMatrix.M23;
            pPlaneEquation[5].Z = pMatrix.M34 - pMatrix.M33;
            pPlaneEquation[5].W = pMatrix.M44 - pMatrix.M43;

            // Normalize the plane equations, if requested
            NormalizePlane(ref pPlaneEquation[0]);
            NormalizePlane(ref pPlaneEquation[1]);
            NormalizePlane(ref pPlaneEquation[2]);
            NormalizePlane(ref pPlaneEquation[3]);
            NormalizePlane(ref pPlaneEquation[4]);
            NormalizePlane(ref pPlaneEquation[5]);
        }
    }
}
