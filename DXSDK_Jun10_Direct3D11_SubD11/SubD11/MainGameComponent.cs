using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static System.Reflection.Metadata.BlobBuilder;

namespace SubD11
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D3D11InputLayout g_pPatchLayout;
        private D3D11InputLayout g_pMeshLayout;

        private D3D11VertexShader g_pPatchSkinningVS;
        private D3D11VertexShader g_pMeshSkinningVS;
        private D3D11HullShader g_pSubDToBezierHS;
        private D3D11HullShader g_pSubDToBezierHS4444;
        private D3D11DomainShader g_pBezierEvalDS;
        private D3D11PixelShader g_pSmoothPS;
        private D3D11PixelShader g_pSolidColorPS;

        private D3D11RasterizerState g_pRasterizerStateSolid;
        private D3D11RasterizerState g_pRasterizerStateWireframe;
        private D3D11SamplerState g_pSamplerStateHeightMap;
        private D3D11SamplerState g_pSamplerStateNormalMap;

        private D3D11Buffer g_pcbTangentStencilConstants;
        private D3D11Buffer g_pcbPerMesh;
        private D3D11Buffer g_pcbPerFrame;

        private uint g_iBindTangentStencilConstants = 0;
        private uint g_iBindPerMesh = 1;
        private uint g_iBindPerFrame = 2;
        //private uint g_iBindValencePrefixBuffer = 0;

        private float g_fFieldOfView = 65.0f;

        private const string g_strDefaultMeshFileName = "SubD\\sebastian.sdkmesh";
        private const string g_strCameraName = "Char_animCameras_combo_camera1";

        public SubDMesh g_SubDMesh;

        public XMMatrix g_viewMatrix;
        public XMMatrix g_projectionMatrix;
        public XMFloat3 g_centerPoint;
        public XMFloat3 g_eyePoint;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel110;

        /// <summary>
        /// Startup subdivisions per side
        /// </summary>
        public int Subdivs { get; set; } = 2;

        /// <summary>
        /// The height amount for displacement mapping
        /// </summary>
        public float DisplacementHeight { get; set; } = 0.0f;

        /// <summary>
        /// Draw the mesh with wireframe overlay
        /// </summary>
        public bool DrawWires { get; set; } = true;

        /// <summary>
        /// Render the object with surface materials
        /// </summary>
        public bool UseMaterials { get; set; } = true;

        public bool CloseupCamera { get; set; } = false;

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            // Create shaders
            this.g_pPatchSkinningVS = device.CreateVertexShader(File.ReadAllBytes("PatchSkinningVS.cso"), null);
            this.g_pPatchSkinningVS.SetDebugName("PatchSkinningVS");

            this.g_pMeshSkinningVS = device.CreateVertexShader(File.ReadAllBytes("MeshSkinningVS.cso"), null);
            this.g_pMeshSkinningVS.SetDebugName("MeshSkinningVS");

            this.g_pSubDToBezierHS = device.CreateHullShader(File.ReadAllBytes("SubDToBezierHS.cso"), null);
            this.g_pSubDToBezierHS.SetDebugName("SubDToBezierHS");

            this.g_pSubDToBezierHS4444 = device.CreateHullShader(File.ReadAllBytes("SubDToBezierHS4444.cso"), null);
            this.g_pSubDToBezierHS4444.SetDebugName("SubDToBezierHS4444");

            this.g_pBezierEvalDS = device.CreateDomainShader(File.ReadAllBytes("BezierEvalDS.cso"), null);
            this.g_pBezierEvalDS.SetDebugName("BezierEvalDS");

            this.g_pSmoothPS = device.CreatePixelShader(File.ReadAllBytes("SmoothPS.cso"), null);
            this.g_pSmoothPS.SetDebugName("SmoothPS");

            this.g_pSolidColorPS = device.CreatePixelShader(File.ReadAllBytes("SolidColorPS.cso"), null);
            this.g_pSolidColorPS.SetDebugName("SolidColorPS");

            // Create our vertex input layout - this matches the SUBD_CONTROL_POINT structure
            D3D11InputElementDesc[] patchlayout = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("WEIGHTS", 0, DxgiFormat.R8G8B8A8UNorm, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("BONES", 0, DxgiFormat.R8G8B8A8UInt, 0, 16, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 20, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 32, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TANGENT", 0, DxgiFormat.R32G32B32Float, 0, 40, D3D11InputClassification.PerVertexData, 0),
            };

            this.g_pPatchLayout = device.CreateInputLayout(patchlayout, File.ReadAllBytes("PatchSkinningVS.cso"));
            this.g_pPatchLayout.SetDebugName("Patch");

            this.g_pMeshLayout = device.CreateInputLayout(patchlayout, File.ReadAllBytes("MeshSkinningVS.cso"));
            this.g_pMeshLayout.SetDebugName("Mesh");

            // Create constant buffers
            this.CreateConstantBuffers();

            // Fill our helper/temporary tables
            this.FillTables();

            // Load mesh
            this.g_SubDMesh = new SubDMesh(device, context);
            this.g_SubDMesh.LoadSubDFromSDKMesh(g_strDefaultMeshFileName, g_strCameraName);

            // Setup the camera's view parameters
            this.g_SubDMesh.GetBounds(out XMFloat3 vCenter, out XMFloat3 vExtents);

            XMFloat3 vEye;

            if (this.CloseupCamera)
            {
                float fRadius = XMVector3.Length(vExtents).X;
                vCenter.Y += fRadius * 0.63f;
                vEye = vCenter;
                vEye.Z -= fRadius * 0.3f;
                vEye.X += fRadius * 0.3f;
            }
            else
            {
                vEye = vCenter;
                float fRadius = XMVector3.Length(vExtents).X;
                float fTheta = XMMath.PI * 0.125f;
                float fDistance = fRadius / (float)Math.Tan(fTheta);
                vEye.Z -= fDistance;
            }

            this.g_eyePoint = vEye;
            this.g_centerPoint = vCenter;
            this.g_viewMatrix = XMMatrix.LookAtLH(this.g_eyePoint, this.g_centerPoint, XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f));

            // Create solid and wireframe rasterizer state objects
            D3D11RasterizerDesc RasterDescSolid = new(D3D11FillMode.Solid, D3D11CullMode.None, false, 0, 0.0f, 0.0f, true, false, false, false);
            this.g_pRasterizerStateSolid = device.CreateRasterizerState(RasterDescSolid);
            this.g_pRasterizerStateSolid.SetDebugName("Solid");

            D3D11RasterizerDesc RasterDescWireframe = new(D3D11FillMode.WireFrame, D3D11CullMode.None, false, 0, 0.0f, 0.0f, true, false, false, false);
            this.g_pRasterizerStateWireframe = device.CreateRasterizerState(RasterDescWireframe);
            this.g_pRasterizerStateWireframe.SetDebugName("Wireframe");

            // Create sampler state for heightmap and normal map
            D3D11SamplerDesc SSDesc = new(
                D3D11Filter.Anisotropic,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                D3D11Constants.MaxAnisotropy,
                D3D11ComparisonFunction.Never,
                null,
                0.0f,
                float.MaxValue
                );

            this.g_pSamplerStateNormalMap = device.CreateSamplerState(SSDesc);
            this.g_pSamplerStateNormalMap.SetDebugName("NormalMap");

            SSDesc.Filter = D3D11Filter.MinMagMipLinear;
            this.g_pSamplerStateHeightMap = device.CreateSamplerState(SSDesc);
            this.g_pSamplerStateHeightMap.SetDebugName("HeightMap");
        }

        private void CreateConstantBuffers()
        {
            var device = this.deviceResources.D3DDevice;

            this.g_pcbTangentStencilConstants = device.CreateBuffer(new(TangentStencilConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbTangentStencilConstants.SetDebugName("CB_TANGENT_STENCIL_CONSTANTS");

            this.g_pcbPerMesh = device.CreateBuffer(new(PerMeshConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbPerMesh.SetDebugName("CB_PER_MESH_CONSTANTS");

            this.g_pcbPerFrame = device.CreateBuffer(new(PerFrameConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));
            this.g_pcbPerFrame.SetDebugName("CB_PER_FRAME_CONSTANTS");
        }

        /// <summary>
        /// Fill the TanM and Ci precalculated tables.  This function precalculates part of the
        /// stencils (weights) used when calculating UV patches.  We precalculate a lot of the
        /// values here and just pass them in as shader constants.
        /// </summary>
        private void FillTables()
        {
            var context = this.deviceResources.D3DContext;

            var data = new TangentStencilConstantBufferData();
            data.TanM = new float[Constants.MaxValence * 64 * 4];
            data.fCi = new float[Constants.MaxValence * 4];

            for (int v = 0; v < Constants.MaxValence; v++)
            {
                float CosfPIV = XMScalar.Cos(XMMath.PI / v);
                float VSqrtTerm = v * (float)Math.Sqrt(4.0f + CosfPIV * CosfPIV);

                for (int i = 0; i < 32; i++)
                {
                    data.TanM[v * 64 * 4 + (i * 2 + 0) * 4 + 0] = ((1.0f / v) + CosfPIV / VSqrtTerm) * XMScalar.Cos((XMMath.TwoPI * i) / v);
                }

                for (int i = 0; i < 32; i++)
                {
                    data.TanM[v * 64 * 4 + (i * 2 + 1) * 4 + 0] = (1.0f / VSqrtTerm) * XMScalar.Cos((XMMath.TwoPI * i + XMMath.PI) / v);
                }
            }

            for (int v = 0; v < Constants.MaxValence; v++)
            {
                data.fCi[v * 4 + 0] = XMScalar.Cos(XMMath.TwoPI / (v + 3.0f));
            }

            context.UpdateSubresource(this.g_pcbTangentStencilConstants, 0, null, data, 0, 0);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.g_pPatchLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pMeshLayout);
            D3D11Utils.DisposeAndNull(ref this.g_pcbTangentStencilConstants);
            D3D11Utils.DisposeAndNull(ref this.g_pcbPerMesh);
            D3D11Utils.DisposeAndNull(ref this.g_pcbPerFrame);

            D3D11Utils.DisposeAndNull(ref this.g_pPatchSkinningVS);
            D3D11Utils.DisposeAndNull(ref this.g_pMeshSkinningVS);
            D3D11Utils.DisposeAndNull(ref this.g_pSubDToBezierHS);
            D3D11Utils.DisposeAndNull(ref this.g_pSubDToBezierHS4444);
            D3D11Utils.DisposeAndNull(ref this.g_pBezierEvalDS);
            D3D11Utils.DisposeAndNull(ref this.g_pSmoothPS);
            D3D11Utils.DisposeAndNull(ref this.g_pSolidColorPS);

            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateSolid);
            D3D11Utils.DisposeAndNull(ref this.g_pRasterizerStateWireframe);
            D3D11Utils.DisposeAndNull(ref this.g_pSamplerStateHeightMap);
            D3D11Utils.DisposeAndNull(ref this.g_pSamplerStateNormalMap);

            this.g_SubDMesh?.Destroy();
        }

        public void CreateWindowSizeDependentResources()
        {
            // Setup the camera's projection parameters
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            float fFOV = XMMath.ConvertToRadians(this.g_fFieldOfView);
            this.g_projectionMatrix = XMMatrix.PerspectiveFovLH(fFOV * 0.5f, fAspectRatio, 0.1f, 4000.0f);
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            this.g_SubDMesh?.Update(XMMatrix.Identity, timer.TotalSeconds);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            // WVP
            XMMatrix mProj = this.g_projectionMatrix;
            XMMatrix mView = this.g_viewMatrix;
            XMFloat3 vCameraPosWorld = this.g_eyePoint;

            this.g_SubDMesh.GetCameraViewMatrix(ref mView, ref vCameraPosWorld);

            XMMatrix mViewProjection = mView * mProj;

            // Update per-frame variables
            PerFrameConstantBufferData data = new()
            {
                mViewProjection = mViewProjection.Transpose(),
                vCameraPosWorld = vCameraPosWorld,
                fTessellationFactor = this.Subdivs,
                fDisplacementHeight = this.DisplacementHeight,
                vSolidColor = new XMVector(0.3f, 0.3f, 0.3f, 0.0f)
            };

            context.UpdateSubresource(this.g_pcbPerFrame, 0, null, data, 0, 0);

            // Clear the render target and depth stencil
            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.01f, 0.01f, 0.02f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // Set state for solid rendering
            context.RasterizerStageSetState(this.g_pRasterizerStateSolid);

            // Render the meshes
            if (this.UseMaterials)
            {
                // Render with materials
                this.RenderSubDMesh(this.g_SubDMesh, this.g_pSmoothPS);
            }
            else
            {
                // Render without materials
                this.RenderSubDMesh(this.g_SubDMesh, this.g_pSolidColorPS);
            }

            // Optionally draw overlay wireframe
            if (this.DrawWires)
            {
                data = new()
                {
                    mViewProjection = mViewProjection.Transpose(),
                    vCameraPosWorld = vCameraPosWorld,
                    fTessellationFactor = this.Subdivs,
                    fDisplacementHeight = this.DisplacementHeight,
                    vSolidColor = new XMVector(0.0f, 1.0f, 0.0f, 0.0f)
                };

                context.UpdateSubresource(this.g_pcbPerFrame, 0, null, data, 0, 0);

                context.RasterizerStageSetState(this.g_pRasterizerStateWireframe);
                // Render the meshes
                this.RenderSubDMesh(this.g_SubDMesh, this.g_pSolidColorPS);
                context.RasterizerStageSetState(this.g_pRasterizerStateSolid);
            }
        }

        /// <summary>
        /// Use the gpu to convert from subds to cubic bezier patches using stream out
        /// </summary>
        private void RenderSubDMesh(SubDMesh pMesh, D3D11PixelShader pPixelShader)
        {
            var context = this.deviceResources.D3DContext;

            // Bind all of the CBs
            context.HullShaderSetConstantBuffers(this.g_iBindTangentStencilConstants, new[] { this.g_pcbTangentStencilConstants });
            context.HullShaderSetConstantBuffers(this.g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.VertexShaderSetConstantBuffers(this.g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.DomainShaderSetConstantBuffers(this.g_iBindPerFrame, new[] { this.g_pcbPerFrame });
            context.PixelShaderSetConstantBuffers(this.g_iBindPerFrame, new[] { this.g_pcbPerFrame });

            // Set the shaders
            context.VertexShaderSetShader(this.g_pPatchSkinningVS, null);
            context.HullShaderSetShader(this.g_pSubDToBezierHS, null);
            context.DomainShaderSetShader(this.g_pBezierEvalDS, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(pPixelShader, null);

            // Set the heightmap sampler state
            context.DomainShaderSetSamplers(0, new[] { this.g_pSamplerStateHeightMap });
            context.PixelShaderSetSamplers(0, new[] { this.g_pSamplerStateNormalMap });

            // Set the input layout
            context.InputAssemblerSetInputLayout(this.g_pPatchLayout);

            bool s_bEnableAnimation = true;

            int PieceCount = pMesh.GetNumPatchPieces();

            // For better performance, the rendering of subd patches has been split into two passes

            // The first pass only renders regular patches (aka valence 4444), with a specialized hull shader which
            // only deals with regular patches
            context.HullShaderSetShader(this.g_pSubDToBezierHS4444, null);

            for (int i = 0; i < PieceCount; i++)
            {
                // Per frame cb update
                PerMeshConstantBufferData data = new()
                {
                    mConstBoneWorld = new XMMatrix[Constants.MaxBoneMatrices]
                };

                int MeshIndex = pMesh.GetPatchMeshIndex(i);
                int NumTransforms = pMesh.GetNumInfluences(MeshIndex);

                Trace.Assert(NumTransforms <= Constants.MaxBoneMatrices);

                for (int j = 0; j < NumTransforms; j++)
                {
                    if (!s_bEnableAnimation)
                    {
                        data.mConstBoneWorld[j] = XMMatrix.Identity;
                    }
                    else
                    {
                        data.mConstBoneWorld[j] = pMesh.GetInfluenceMatrix(MeshIndex, j).Transpose();
                    }
                }

                if (NumTransforms == 0)
                {
                    pMesh.GetPatchPieceTransform(i, out XMMatrix matTransform);
                    data.mConstBoneWorld[0] = matTransform.Transpose();
                }

                context.UpdateSubresource(this.g_pcbPerMesh, 0, null, data, 0, 0);
                context.VertexShaderSetConstantBuffers(this.g_iBindPerMesh, new[] { this.g_pcbPerMesh });

                pMesh.RenderPatchPiece_OnlyRegular(i);
            }

            // The second pass renders the rest of the patches, with the general hull shader
            context.HullShaderSetShader(this.g_pSubDToBezierHS, null);

            for (int i = 0; i < PieceCount; i++)
            {
                // Per frame cb update
                PerMeshConstantBufferData data = new()
                {
                    mConstBoneWorld = new XMMatrix[Constants.MaxBoneMatrices]
                };

                int MeshIndex = pMesh.GetPatchMeshIndex(i);
                int NumTransforms = pMesh.GetNumInfluences(MeshIndex);

                Trace.Assert(NumTransforms <= Constants.MaxBoneMatrices);

                for (int j = 0; j < NumTransforms; j++)
                {
                    if (!s_bEnableAnimation)
                    {
                        data.mConstBoneWorld[j] = XMMatrix.Identity;
                    }
                    else
                    {
                        data.mConstBoneWorld[j] = pMesh.GetInfluenceMatrix(MeshIndex, j).Transpose();
                    }
                }

                if (NumTransforms == 0)
                {
                    pMesh.GetPatchPieceTransform(i, out XMMatrix matTransform);
                    data.mConstBoneWorld[0] = matTransform.Transpose();
                }

                context.UpdateSubresource(this.g_pcbPerMesh, 0, null, data, 0, 0);
                context.VertexShaderSetConstantBuffers(this.g_iBindPerMesh, new[] { g_pcbPerMesh });

                pMesh.RenderPatchPiece_OnlyExtraordinary(i);
            }

            context.VertexShaderSetShader(this.g_pMeshSkinningVS, null);
            context.HullShaderSetShader(null, null);
            context.DomainShaderSetShader(null, null);
            context.GeometryShaderSetShader(null, null);
            context.PixelShaderSetShader(pPixelShader, null);
            context.InputAssemblerSetInputLayout(this.g_pMeshLayout);

            // Then finally renders the poly portion of the mesh
            PieceCount = pMesh.GetNumPolyMeshPieces();

            for (int i = 0; i < PieceCount; i++)
            {
                // Per frame cb update
                PerMeshConstantBufferData data = new()
                {
                    mConstBoneWorld = new XMMatrix[Constants.MaxBoneMatrices]
                };

                int MeshIndex = pMesh.GetPolyMeshIndex(i);
                int NumTransforms = pMesh.GetNumInfluences(MeshIndex);

                Trace.Assert(NumTransforms <= Constants.MaxBoneMatrices);

                for (int j = 0; j < NumTransforms; j++)
                {
                    if (!s_bEnableAnimation)
                    {
                        data.mConstBoneWorld[j] = XMMatrix.Identity;
                    }
                    else
                    {
                        data.mConstBoneWorld[j] = pMesh.GetInfluenceMatrix(MeshIndex, j).Transpose();
                    }
                }

                if (NumTransforms == 0)
                {
                    pMesh.GetPolyMeshPieceTransform(i, out XMMatrix matTransform);
                    data.mConstBoneWorld[0] = matTransform.Transpose();
                }

                context.UpdateSubresource(this.g_pcbPerMesh, 0, null, data, 0, 0);
                context.VertexShaderSetConstantBuffers(this.g_iBindPerMesh, new[] { this.g_pcbPerMesh });

                pMesh.RenderPolyMeshPiece(i);
            }
        }
    }
}
