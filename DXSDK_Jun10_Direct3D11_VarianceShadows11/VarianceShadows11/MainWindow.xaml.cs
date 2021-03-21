using JeremyAnsel.DirectX.Window.Wpf;
using System.Windows;

namespace VarianceShadows11
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
