using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
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
        private bool _clientJoined = false;
        private readonly ConcurrentBag<SignallingMessage> _defferedMessages = new ConcurrentBag<SignallingMessage>();
        private PeerConnection _peerConnection;
        private HubConnection _hubConnection;
        private DataChannel _dc;
        public ShareWindow()
        {
            InitializeComponent();
            this.SizeChanged += MainWindow_SizeChanged;

            MouseController.RegisterCallback((x, y) =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Render,
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
            new IceServer{ Urls = { "stun:numb.viagenie.ca", "stun:stun.l.google.com:19302"  }, TurnPassword = "babsest4U%%", TurnUserName = "babgev@gmail.com"}
        }
            };
            //_peerConnection.DataChannelAdded += PeerConnection_DataChannelAdded;

            _peerConnection.Connected += () =>
            {
                Debugger.Log(0, "", "PeerConnection: connected.\n");
            };
            _peerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Debugger.Log(0, "", $"ICE state: {newState}\n");
            };

            await _peerConnection.InitializeAsync(config);
            _peerConnection.DataChannelAdded += PeerConnection_DataChannelAdded;
            _dc = await _peerConnection.AddDataChannelAsync(14, "vzgo", true, true);
            _dc.StateChanged += Dc_StateChanged;
            _dc.MessageReceived += Channel_MessageReceived;

            Debugger.Log(0, "", "Peer connection initialized successfully.\n");

            _peerConnection.LocalSdpReadytoSend += Peer_LocalSdpReadytoSend;
            _peerConnection.IceCandidateReadytoSend += Peer_IceCandidateReadytoSend;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(SignallerConstants.SignallerUrl))
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10) })
                .Build();

            _hubConnection.On("PeerJoined", async () =>
            {
                _clientJoined = true;
                foreach (var msg in _defferedMessages)
                {
                    await SendMessageAsync(msg);
                }
            });

            _hubConnection.On<string>("Message", (message) =>
            {
                var msg = JsonConvert.DeserializeObject<SignallingMessage>(message);
                switch (msg.MessageType)
                {
                    case SignallingMessage.WireMessageType.Offer:
                        _peerConnection.SetRemoteDescription("offer", msg.Data);
                        _peerConnection.CreateAnswer();
                        break;

                    case SignallingMessage.WireMessageType.Answer:
                        _peerConnection.SetRemoteDescription("answer", msg.Data);
                        break;

                    case SignallingMessage.WireMessageType.Ice:
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
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("CreateRoom", SignallerConstants.RoomName);

        }

        private void Dc_StateChanged()
        {
            var z = _dc.State;
            Debugger.Log(0, "", z.ToString());
        }

        private void PeerConnection_DataChannelAdded(DataChannel channel)
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

        private async void Peer_LocalSdpReadytoSend(string type, string sdp)
        {
            var msg = new SignallingMessage
            {
                MessageType = SignallingMessage.WireMessageTypeFromString(type),
                Data = sdp,
                IceDataSeparator = "|"
            };
            await SendDefferedMessage(msg);
        }

        private async void Peer_IceCandidateReadytoSend(
            string candidate, int sdpMlineindex, string sdpMid)
        {
            var msg = new SignallingMessage
            {
                MessageType = SignallingMessage.WireMessageType.Ice,
                Data = $"{candidate}|{sdpMlineindex}|{sdpMid}",
                IceDataSeparator = "|"
            };
            await SendDefferedMessage(msg);
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _peerConnection.Close();
            _peerConnection.Dispose();
            await _hubConnection.InvokeAsync("LeaveRoom", SignallerConstants.RoomName);
        }

        private async Task SendDefferedMessage(SignallingMessage message)
        {
            if (_clientJoined)
            {
                await SendMessageAsync(message);
            }
            else
            {
                _defferedMessages.Add(message);
            }
        }

        private async Task SendMessageAsync(SignallingMessage message)
        {
            await _hubConnection.InvokeAsync("SendMessage", SignallerConstants.RoomName, JsonConvert.SerializeObject(message));
        }
    }
}
