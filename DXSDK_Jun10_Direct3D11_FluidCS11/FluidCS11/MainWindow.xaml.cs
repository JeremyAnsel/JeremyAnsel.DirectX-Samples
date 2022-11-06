using JeremyAnsel.DirectX.Window.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FluidCS11
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainGameWindow game;

        public MainWindow()
        {
            InitializeComponent();

            this.game = new MainGameWindow();
            this.ControlHostElement.Child = new WindowHost(this.game);
        }

        private void ResetSimButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.game is null)
            {
                return;
            }

            this.game.InvalidateSimulationBuffers();
        }

        private void NumParticlesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.game is null)
            {
                return;
            }

            var list = (ListBox)sender;

            switch (list.SelectedIndex)
            {
                case 0:
                    this.game.NumParticles = Constants.NUM_PARTICLES_8K;
                    break;

                case 1:
                default:
                    this.game.NumParticles = Constants.NUM_PARTICLES_16K;
                    break;

                case 2:
                    this.game.NumParticles = Constants.NUM_PARTICLES_32K;
                    break;

                case 3:
                    this.game.NumParticles = Constants.NUM_PARTICLES_64K;
                    break;
            }
        }

        private void GravityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.game is null)
            {
                return;
            }

            var list = (ListBox)sender;

            switch (list.SelectedIndex)
            {
                case 0:
                default:
                    this.game.Gravity = Constants.GRAVITY_DOWN;
                    break;

                case 1:
                    this.game.Gravity = Constants.GRAVITY_UP;
                    break;

                case 2:
                    this.game.Gravity = Constants.GRAVITY_LEFT;
                    break;

                case 3:
                    this.game.Gravity = Constants.GRAVITY_RIGHT;
                    break;
            }
        }
    }
}
