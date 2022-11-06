using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif
        }

        public uint NumParticles
        {
            get
            {
                return this.mainGameComponent?.NumParticles ?? Constants.NUM_PARTICLES_16K;
            }

            set
            {
                if (value != this.mainGameComponent.NumParticles)
                {
                    this.mainGameComponent.NumParticles = value;
                    this.mainGameComponent.InvalidateSimulationBuffers();
                    this.NotifyPropertyChanged();
                }
            }
        }

        public XMFloat2 Gravity
        {
            get
            {
                return this.mainGameComponent?.Gravity ?? Constants.GRAVITY_DOWN;
            }

            set
            {
                if (value != this.mainGameComponent.Gravity)
                {
                    this.mainGameComponent.Gravity = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public SimulationMode SimMode
        {
            get
            {
                return this.mainGameComponent?.SimMode ?? SimulationMode.Grid;
            }

            set
            {
                if (value != this.mainGameComponent.SimMode)
                {
                    this.mainGameComponent.SimMode = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public void InvalidateSimulationBuffers()
        {
            this.mainGameComponent.InvalidateSimulationBuffers();
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();

            this.mainGameComponent.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            this.mainGameComponent.CreateWindowSizeDependentResources();
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            this.mainGameComponent.Update(this.Timer);
        }

        protected override void Render()
        {
            this.mainGameComponent.Render();
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);
        }
    }
}
