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

namespace SimpleBezier11
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
    }
}
