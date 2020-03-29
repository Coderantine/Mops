using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessagePack;
using Microsoft.MixedReality.WebRTC;
using Mops.Client.Core;
using Newtonsoft.Json;

namespace Mops.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ShareWindow : Window
    {
        private PeerConnection _peerConnection;
        private NodeDssSignaler _signaler;

        public ShareWindow()
        {
            InitializeComponent();
            this.SizeChanged += MainWindow_SizeChanged;

            MouseController.RegisterCallback((x, y) =>
            {
                Debugger.Log(0, "", $"{x}-{y}");
                Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() =>
                  {
                      Canvas.SetLeft(Zima, x);
                      Canvas.SetTop(Zima, y);
                  }));
            });
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WindowSettings.WindowHeight = this.Height;
            WindowSettings.WindowWidth = this.Width;
        }

        private async void Window_Initialized(object sender, System.EventArgs e)
        {
            _peerConnection = new PeerConnection();
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
        }
            };

            _peerConnection.Connected += () =>
            {
                Debugger.Log(0, "", "PeerConnection: connected.\n");
            };
            _peerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Debugger.Log(0, "", $"ICE state: {newState}\n");
            };

            await _peerConnection.InitializeAsync(config);

            Debugger.Log(0, "", "Peer connection initialized successfully.\n");

            _peerConnection.LocalSdpReadytoSend += Peer_LocalSdpReadytoSend;
            _peerConnection.IceCandidateReadytoSend += Peer_IceCandidateReadytoSend;

            _signaler = new NodeDssSignaler()
            {
                HttpServerAddress = "http://127.0.0.1:3000/",
                LocalPeerId = "share",
                RemotePeerId = "control",
            };
            _signaler.OnMessage += (NodeDssSignaler.Message msg) =>
            {
                switch (msg.MessageType)
                {
                    case NodeDssSignaler.Message.WireMessageType.Offer:
                        _peerConnection.SetRemoteDescription("offer", msg.Data);
                        _peerConnection.CreateAnswer();
                        break;

                    case NodeDssSignaler.Message.WireMessageType.Answer:
                        _peerConnection.SetRemoteDescription("answer", msg.Data);
                        break;

                    case NodeDssSignaler.Message.WireMessageType.Ice:
                        var parts = msg.Data.Split(new string[] { msg.IceDataSeparator },
                            StringSplitOptions.RemoveEmptyEntries);
                        // Note the inverted arguments for historical reasons.
                        // 'candidate' is last in AddIceCandidate(), but first in the message.
                        string sdpMid = parts[2];
                        int sdpMlineindex = int.Parse(parts[1]);
                        string candidate = parts[0];
                        _peerConnection.AddIceCandidate(sdpMid, sdpMlineindex, candidate);
                        break;
                }
            };
            _signaler.StartPollingAsync();
            _peerConnection.DataChannelAdded += _peerConnection_DataChannelAdded;


        }

        private void _peerConnection_DataChannelAdded(DataChannel channel)
        {
            Debugger.Log(0, "", "data channel created for SHARE");
            channel.MessageReceived += Channel_MessageReceived;
        }

        private void Channel_MessageReceived(byte[] obj)
        {
            var bytesAsString = Encoding.UTF8.GetString(obj);
            var evnt = JsonConvert.DeserializeObject<MouseMoveEvent>(bytesAsString);
            MouseController.Move(evnt);
        }

        private void Peer_LocalSdpReadytoSend(string type, string sdp)
        {
            var msg = new NodeDssSignaler.Message
            {
                MessageType = NodeDssSignaler.Message.WireMessageTypeFromString(type),
                Data = sdp,
                IceDataSeparator = "|"
            };
            _signaler.SendMessageAsync(msg);
        }

        private void Peer_IceCandidateReadytoSend(
            string candidate, int sdpMlineindex, string sdpMid)
        {
            var msg = new NodeDssSignaler.Message
            {
                MessageType = NodeDssSignaler.Message.WireMessageType.Ice,
                Data = $"{candidate}|{sdpMlineindex}|{sdpMid}",
                IceDataSeparator = "|"
            };
            _signaler.SendMessageAsync(msg);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_peerConnection != null)
            {
                _peerConnection.Close();
                _peerConnection.Dispose();
                _peerConnection = null;
            }

            if (_signaler != null)
            {
                _signaler.StopPollingAsync();
                _signaler = null;
            }
        }
    }
}
