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
using System.Windows.Shapes;

namespace Mops.Client
{
    /// <summary>
    /// Interaction logic for ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {
        public ControlWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        private ICommand start;
        public ICommand Start
        {
            get
            {
                return start
                    ?? (start = new ActionCommand(() =>
                    {
                        this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
                        WindowsService.HideCursor();
                    }));
            }
        }

        private ICommand stop;
        public ICommand Stop
        {
            get
            {
                return stop
                    ?? (stop = new ActionCommand(() =>
                    {
                        this.Background = new SolidColorBrush(Colors.White) { Opacity = 0 };
                        WindowsService.ShowCursor();
                    }));
            }
        }
    }
}
