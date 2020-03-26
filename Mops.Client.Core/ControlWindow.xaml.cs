using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MessagePack;
using Microsoft.MixedReality.WebRTC;
using Mops.Client.Core;

namespace Mops.Client
{
    /// <summary>
    /// Interaction logic for ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {
        private bool _shouldHideCursor = false;
        private bool _cursorIsHidden = false;
        private DataChannel _dc;

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

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dc != null && _dc.State == DataChannel.ChannelState.Open)
            {
                var mousePos = Mouse.GetPosition(this);
                var mouseEvent = new MouseMoveEvent()
                {
                    WindowHeight = this.Height,
                    WindowWidth = this.Width,
                    MouseX = mousePos.X,
                    MouseY = mousePos.Y,
                };

                var msg = MessagePackSerializer.Serialize(mouseEvent);
                _dc.SendMessage(msg);
            }
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            // Create a new peer connection automatically disposed at the end of the program
            using var pc = new PeerConnection();

            // Initialize the connection with a STUN server to allow remote access
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                        }
            };
            await pc.InitializeAsync(config);

            var signaler = new NamedPipeSignaler(pc, "testpipe");
            signaler.SdpMessageReceived += (string type, string sdp) =>
            {
                pc.SetRemoteDescription(type, sdp);
                if (type == "offer")
                {
                    pc.CreateAnswer();
                }
            };
            signaler.IceCandidateReceived += (string sdpMid, int sdpMlineindex, string candidate) =>
            {
                pc.AddIceCandidate(sdpMid, sdpMlineindex, candidate);
            };
            await signaler.StartAsync();

            pc.DataChannelAdded += (DataChannel dataChannel) =>
           {
               _dc = dataChannel;
           };

        }
    }
}
