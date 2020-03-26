using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mops.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ShareWindow : Window
    {
        public ShareWindow()
        {
            InitializeComponent();
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WindowSettings.WindowHeight = this.Height;
            WindowSettings.WindowWidth = this.Width;

            MouseController.RegisterCallback((x, y) =>
            {
                Canvas.SetLeft(Zima, x);
                Canvas.SetTop(Zima, y);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var mousePos = Mouse.GetPosition(this);
            var mouseEvent = new MouseMoveEvent()
            {
                WindowHeight = this.Height,
                WindowWidth = this.Width,
                MouseX = mousePos.X,
                MouseY = mousePos.Y,
            };
            MouseController.Move(mouseEvent);
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = Mouse.GetPosition(SmallScreen);
            var mouseEvent = new MouseMoveEvent()
            {
                WindowHeight = SmallScreen.Height,
                WindowWidth = SmallScreen.Width,
                MouseX = mousePos.X,
                MouseY = mousePos.Y,
            };
            MouseController.Move(mouseEvent);
        }
    }
}
