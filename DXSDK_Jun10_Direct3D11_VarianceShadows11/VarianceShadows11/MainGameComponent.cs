using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkMesh;

namespace VarianceShadows11
{
    class MainGameComponent : IGameComponent
    {
        public readonly MainGameSettings Settings = new MainGameSettings();

        private DeviceResources deviceResources;

        private readonly CascadeConfig cascadeConfig;
        private readonly VarianceShadowsManager varianceShadow;

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
                ShadowBufferFormat = DepthBufferFormat.R32G32
            };

            this.varianceShadow = new VarianceShadowsManager(this.Settings);

            this.varianceShadow.CascadePartitionsZeroToOne[0] = 5;
            this.varianceShadow.CascadePartitionsZeroToOne[1] = 15;
            this.varianceShadow.CascadePartitionsZeroToOne[2] = 60;
            this.varianceShadow.CascadePartitionsZeroToOne[3] = 100;
            this.varianceShadow.CascadePartitionsZeroToOne[4] = 100;
            this.varianceShadow.CascadePartitionsZeroToOne[5] = 100;
            this.varianceShadow.CascadePartitionsZeroToOne[6] = 100;
            this.varianceShadow.CascadePartitionsZeroToOne[7] = 100;

            // Pick some arbitrary intervals for the Cascade Maps
            //this.varianceShadow.CascadePartitionsZeroToOne[0] = 2;
            //this.varianceShadow.CascadePartitionsZeroToOne[1] = 4;
            //this.varianceShadow.CascadePartitionsZeroToOne[2] = 6;
            //this.varianceShadow.CascadePartitionsZeroToOne[3] = 9;
            //this.varianceShadow.CascadePartitionsZeroToOne[4] = 13;
            //this.varianceShadow.CascadePartitionsZeroToOne[5] = 26;
            //this.varianceShadow.CascadePartitionsZeroToOne[6] = 36;
            //this.varianceShadow.CascadePartitionsZeroToOne[7] = 70;

            this.varianceShadow.CascadePartitionsMax = 100;

            this.varianceShadow.MoveLightTexelSize = true;
            this.varianceShadow.SelectedProjectionFit = FitProjection.ToScene;
            this.varianceShadow.SelectedNearFarFit = FitNearFar.SceneAABB;
            this.varianceShadow.SelectedCascadeSelection = CascadeSelection.Map;
            this.varianceShadow.SelectedCamera = CameraSelection.EyeCamera;
            this.varianceShadow.ShadowFilter = ShadowFilter.Anisotropic16x;
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public bool VisualizeCascades { get; set; } = false;

        public DepthBufferFormat ShadowBufferFormat
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

        public int ShadowBlurSize
        {
            get
            {
                return this.varianceShadow.ShadowBlurSize;
            }

            set
            {
                this.varianceShadow.ShadowBlurSize = value;
            }
        }

        public bool BlurBetweenCascades
        {
            get
            {
                return this.varianceShadow.BlurBetweenCascades;
            }

            set
            {
                this.varianceShadow.BlurBetweenCascades = value;
            }
        }

        public float BlurBetweenCascadesAmount
        {
            get
            {
                return this.varianceShadow.BlurBetweenCascadesAmount;
            }

            set
            {
                this.varianceShadow.BlurBetweenCascadesAmount = value;
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

        public ShadowFilter ShadowFilter
        {
            get
            {
                return this.varianceShadow.ShadowFilter;
            }

            set
            {
                this.varianceShadow.ShadowFilter = value;
            }
        }

        public CameraSelection SelectedCamera
        {
            get
            {
                return this.varianceShadow.SelectedCamera;
            }

            set
            {
                this.varianceShadow.SelectedCamera = value;
            }
        }

        public bool MoveLightTexelSize
        {
            get
            {
                return this.varianceShadow.MoveLightTexelSize;
            }

            set
            {
                this.varianceShadow.MoveLightTexelSize = value;
            }
        }

        public FitProjection SelectedProjectionFit
        {
            get
            {
                return this.varianceShadow.SelectedProjectionFit;
            }

            set
            {
                this.varianceShadow.SelectedProjectionFit = value;
            }
        }

        public FitNearFar SelectedNearFarFit
        {
            get
            {
                return this.varianceShadow.SelectedNearFarFit;
            }

            set
            {
                this.varianceShadow.SelectedNearFarFit = value;
            }
        }

        public CascadeSelection SelectedCascadeSelection
        {
            get
            {
                return this.varianceShadow.SelectedCascadeSelection;
            }

            set
            {
                this.varianceShadow.SelectedCascadeSelection = value;
            }
        }

        public int[] CascadePartitionsZeroToOne
        {
            get
            {
                return this.varianceShadow.CascadePartitionsZeroToOne;
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

            this.varianceShadow.Init(
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

            this.varianceShadow.DestroyAndDeallocateShadowResources();
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
            this.varianceShadow.InitScene(this.selectedMesh, this.cascadeConfig);

            XMVector vMeshExtents = this.varianceShadow.SceneAABBMax - this.varianceShadow.SceneAABBMin;
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

            this.varianceShadow.InitFrame(device, this.selectedMesh);
            this.varianceShadow.RenderShadowsForAllCascades(device, context, this.selectedMesh);

            D3D11Viewport vp = new D3D11Viewport(
                0.0f,
                0.0f,
                this.deviceResources.BackBufferWidth,
                this.deviceResources.BackBufferHeight,
                0.0f,
                1.0f);

            this.varianceShadow.RenderScene(context, pRTV, pDSV, this.selectedMesh, vp, this.VisualizeCascades);

            context.RasterizerStageSetViewports(new D3D11Viewport[] { vp });
            context.OutputMergerSetRenderTargets(new[] { pRTV }, pDSV);
        }
    }
}
