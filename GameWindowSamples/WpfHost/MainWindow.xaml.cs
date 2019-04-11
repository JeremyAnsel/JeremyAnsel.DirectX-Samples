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

namespace WpfHost
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game game;

        public MainWindow()
        {
            InitializeComponent();

            this.game = new Game();
            this.ControlHostElement.Child = new WindowHost(this.game);
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            this.game.clearColor = new float[] { 1.0f, 0.0f, 0.0f, 1.0f };
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            this.game.clearColor = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        }
    }
}
