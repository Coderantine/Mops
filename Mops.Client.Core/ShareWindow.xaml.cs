using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MessagePack;
using Microsoft.MixedReality.WebRTC;
using Mops.Client.Core;

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

        private async void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
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

            // Setup signaling
            var signaler = new NamedPipeSignaler(pc, "testpipe");
            signaler.SdpMessageReceived += (string type, string sdp) =>
            {
                pc.SetRemoteDescription(type, sdp);
            };
            signaler.IceCandidateReceived += (string sdpMid, int sdpMlineindex, string candidate) =>
            {
                pc.AddIceCandidate(sdpMid, sdpMlineindex, candidate);
            };
            await signaler.StartAsync();

            pc.CreateOffer();
            var dc = await pc.AddDataChannelAsync("test", true, true);

            WindowSettings.WindowHeight = this.Height;
            WindowSettings.WindowWidth = this.Width;

            dc.MessageReceived += (byte[] data) =>
            {
                var k = MessagePackSerializer.Deserialize<MouseMoveEvent>(data);
                MouseController.Move(k);
            };

            MouseController.RegisterCallback((x, y) =>
            {
                Canvas.SetLeft(Zima, x);
                Canvas.SetTop(Zima, y);
            });


        }
    }
}
