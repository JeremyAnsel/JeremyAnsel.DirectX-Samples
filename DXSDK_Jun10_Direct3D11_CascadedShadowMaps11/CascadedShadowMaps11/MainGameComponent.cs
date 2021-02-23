using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;

namespace CascadedShadowMaps11
{
    class MainGameComponent : IGameComponent
    {
        public readonly MainGameSettings Settings = new MainGameSettings();

        private DeviceResources deviceResources;

        private readonly CascadeConfig cascadeConfig;
        private readonly CascadedShadowsManager cascadedShadow;

        private SdkMeshFile meshPowerPlant;
        private SdkMeshFile meshTestScene;
        private SdkMeshFile selectedMesh;

        private bool switchMesh;

        public MainGameComponent()
        {
            this.cascadeConfig = new CascadeConfig
            {
                CascadeLevels = 3,
                BufferSize = 1024,
                ShadowBufferFormat = ShadowTextureFormat.R32
            };

            this.cascadedShadow = new CascadedShadowsManager(this.Settings);

            this.cascadedShadow.CascadePartitionsZeroToOne[0] = 5;
            this.cascadedShadow.CascadePartitionsZeroToOne[1] = 15;
            this.cascadedShadow.CascadePartitionsZeroToOne[2] = 60;
            this.cascadedShadow.CascadePartitionsZeroToOne[3] = 100;
            this.cascadedShadow.CascadePartitionsZeroToOne[4] = 100;
            this.cascadedShadow.CascadePartitionsZeroToOne[5] = 100;
            this.cascadedShadow.CascadePartitionsZeroToOne[6] = 100;
            this.cascadedShadow.CascadePartitionsZeroToOne[7] = 100;

            // Pick some arbitrary intervals for the Cascade Maps
            //this.cascadedShadow.CascadePartitionsZeroToOne[0] = 2;
            //this.cascadedShadow.CascadePartitionsZeroToOne[1] = 4;
            //this.cascadedShadow.CascadePartitionsZeroToOne[2] = 6;
            //this.cascadedShadow.CascadePartitionsZeroToOne[3] = 9;
            //this.cascadedShadow.CascadePartitionsZeroToOne[4] = 13;
            //this.cascadedShadow.CascadePartitionsZeroToOne[5] = 26;
            //this.cascadedShadow.CascadePartitionsZeroToOne[6] = 36;
            //this.cascadedShadow.CascadePartitionsZeroToOne[7] = 70;

            this.cascadedShadow.CascadePartitionsMax = 100;

            this.cascadedShadow.MoveLightTexelSize = true;
            this.cascadedShadow.SelectedProjectionFit = FitProjection.ToScene;
            this.cascadedShadow.SelectedNearFarFit = FitNearFar.SceneAABB;
            this.cascadedShadow.SelectedCascadeSelection = CascadeSelection.Map;
            this.cascadedShadow.SelectedCamera = CameraSelection.EyeCamera;
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public bool VisualizeCascades { get; set; } = false;

        public ShadowTextureFormat ShadowBufferFormat
        {
            get
            {
                return this.cascadeConfig.ShadowBufferFormat;
            }

            set
            {
                this.cascadeConfig.ShadowBufferFormat = value;
            }
        }

        public int BufferSize
        {
            get
            {
                return this.cascadeConfig.BufferSize;
            }

            set
            {
                this.cascadeConfig.BufferSize = value;
            }
        }

        public int CascadeLevels
        {
            get
            {
                return this.cascadeConfig.CascadeLevels;
            }

            set
            {
                this.cascadeConfig.CascadeLevels = value;
            }
        }

        public int PCFBlurSize
        {
            get
            {
                return this.cascadedShadow.PCFBlurSize;
            }

            set
            {
                this.cascadedShadow.PCFBlurSize = value;
            }
        }

        public float PCFOffset
        {
            get
            {
                return this.cascadedShadow.PCFOffset;
            }

            set
            {
                this.cascadedShadow.PCFOffset = value;
            }
        }

        public bool BlurBetweenCascades
        {
            get
            {
                return this.cascadedShadow.BlurBetweenCascades;
            }

            set
            {
                this.cascadedShadow.BlurBetweenCascades = value;
            }
        }

        public float BlurBetweenCascadesAmount
        {
            get
            {
                return this.cascadedShadow.BlurBetweenCascadesAmount;
            }

            set
            {
                this.cascadedShadow.BlurBetweenCascadesAmount = value;
            }
        }

        public bool DerivativeBasedOffset
        {
            get
            {
                return this.cascadedShadow.DerivativeBasedOffset;
            }

            set
            {
                this.cascadedShadow.DerivativeBasedOffset = value;
            }
        }

        public SceneSelection SceneSelection
        {
            get
            {
                if (this.selectedMesh == this.meshPowerPlant)
                {
                    return SceneSelection.PowerPlantScene;
                }

                if (this.selectedMesh == this.meshTestScene)
                {
                    return SceneSelection.TestScene;
                }

                return SceneSelection.PowerPlantScene;
            }

            set
            {
                SdkMeshFile mesh = this.selectedMesh;

                switch (value)
                {
                    case SceneSelection.PowerPlantScene:
                    default:
                        this.selectedMesh = this.meshPowerPlant;
                        break;

                    case SceneSelection.TestScene:
                        this.selectedMesh = this.meshTestScene;
                        break;
                }

                if (mesh != this.selectedMesh)
                {
                    this.switchMesh = true;
                }
            }
        }

        public CameraSelection SelectedCamera
        {
            get
            {
                return this.cascadedShadow.SelectedCamera;
            }

            set
            {
                this.cascadedShadow.SelectedCamera = value;
            }
        }

        public bool MoveLightTexelSize
        {
            get
            {
                return this.cascadedShadow.MoveLightTexelSize;
            }

            set
            {
                this.cascadedShadow.MoveLightTexelSize = value;
            }
        }

        public FitProjection SelectedProjectionFit
        {
            get
            {
                return this.cascadedShadow.SelectedProjectionFit;
            }

            set
            {
                this.cascadedShadow.SelectedProjectionFit = value;
            }
        }

        public FitNearFar SelectedNearFarFit
        {
            get
            {
                return this.cascadedShadow.SelectedNearFarFit;
            }

            set
            {
                this.cascadedShadow.SelectedNearFarFit = value;
            }
        }

        public CascadeSelection SelectedCascadeSelection
        {
            get
            {
                return this.cascadedShadow.SelectedCascadeSelection;
            }

            set
            {
                this.cascadedShadow.SelectedCascadeSelection = value;
            }
        }

        public int[] CascadePartitionsZeroToOne
        {
            get
            {
                return this.cascadedShadow.CascadePartitionsZeroToOne;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var d3dDevice = this.deviceResources.D3DDevice;
            var d3dContext = this.deviceResources.D3DContext;

            this.meshPowerPlant = SdkMeshFile.FromFile(d3dDevice, d3dContext, "powerplant\\powerplant.sdkmesh");
            this.meshTestScene = SdkMeshFile.FromFile(d3dDevice, d3dContext, "ShadowColumns\\testscene.sdkmesh");
            this.SceneSelection = SceneSelection.PowerPlantScene;

            this.cascadedShadow.Init(
                d3dDevice,
                d3dContext);

            this.InitScene();
        }

        public void ReleaseDeviceDependentResources()
        {
            this.meshPowerPlant?.Release();
            this.meshPowerPlant = null;

            this.meshTestScene?.Release();
            this.meshTestScene = null;

            this.cascadedShadow.DestroyAndDeallocateShadowResources();
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / this.deviceResources.BackBufferHeight;

            this.Settings.ViewerCameraProjection = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, 0.05f, this.Settings.MeshLength);
            this.Settings.ViewerCameraNearClip = 0.05f;
            this.Settings.ViewerCameraFarClip = this.Settings.MeshLength;

            this.Settings.ActiveCameraProjection = this.Settings.ViewerCameraProjection;
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        private void InitScene()
        {
            this.cascadedShadow.InitScene(this.selectedMesh, this.cascadeConfig);

            XMVector vMeshExtents = this.cascadedShadow.SceneAABBMax - this.cascadedShadow.SceneAABBMin;
            XMVector vMeshLength = XMVector3.Length(vMeshExtents);
            float fMeshLength = vMeshLength.GetByIndex(0);
            this.Settings.MeshLength = fMeshLength;
        }

        public void Update(ITimer timer)
        {
            if (this.switchMesh)
            {
                this.InitScene();
                this.switchMesh = false;
            }
        }

        public void Render()
        {
            var device = this.deviceResources.D3DDevice;
            var context = this.deviceResources.D3DContext;

            float[] ClearColor = new float[] { 0.0f, 0.55f, 0.55f, 1.0f };
            D3D11RenderTargetView pRTV = this.deviceResources.D3DRenderTargetView;
            D3D11DepthStencilView pDSV = this.deviceResources.D3DDepthStencilView;
            context.ClearRenderTargetView(pRTV, ClearColor);
            context.ClearDepthStencilView(pDSV, D3D11ClearOptions.Depth, 1.0f, 0);

            this.cascadedShadow.InitFrame(device, this.selectedMesh);
            this.cascadedShadow.RenderShadowsForAllCascades(device, context, this.selectedMesh);

            D3D11Viewport vp = new D3D11Viewport(
                0.0f,
                0.0f,
                this.deviceResources.BackBufferWidth,
                this.deviceResources.BackBufferHeight,
                0.0f,
                1.0f);

            this.cascadedShadow.RenderScene(context, pRTV, pDSV, this.selectedMesh, vp, this.VisualizeCascades);

            context.RasterizerStageSetViewports(new D3D11Viewport[] { vp });
            context.OutputMergerSetRenderTargets(new[] { pRTV }, pDSV);
        }
    }
}
