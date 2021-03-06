﻿using System;
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
        private bool _shouldHideCursor = false;
        private bool _cursorIsHidden = false;
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
                        HideCursor();
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
                        ShowCursor();
                    }));
            }
        }

        private void Start_Clicked(object sender, RoutedEventArgs e)
        {
            _shouldHideCursor = true;
        }

        private void Stop_Clicked(object sender, RoutedEventArgs e)
        {
            _shouldHideCursor = false;
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_cursorIsHidden)
            {
                ShowCursor();
            }
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_shouldHideCursor)
            {
                HideCursor();
            }
        }

        private void ShowCursor()
        {
            this.Background = new SolidColorBrush(Colors.White) { Opacity = 0 };
            WindowsService.ShowCursor();
            _cursorIsHidden = false;
        }

        private void HideCursor()
        {
            this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
            WindowsService.HideCursor();
            _cursorIsHidden = true;
        }
    }
}
