using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.MixedReality.WebRTC;
using Mops.Client.Core;
using Newtonsoft.Json;

namespace Mops.Client
{
    /// <summary>
    /// Interaction logic for ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {
        private bool _shouldHideCursor = false;
        private bool _cursorIsHidden = false;
        private PeerConnection _peerConnection;
        private HubConnection _hubConnection;
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
            _peerConnection.CreateOffer();
            //   _shouldHideCursor = true;
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

                var msg = JsonConvert.SerializeObject(mouseEvent);
                _dc.SendMessage(Encoding.UTF8.GetBytes(msg));
            }
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            _peerConnection = new PeerConnection();
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
            new IceServer{ Urls = { "stun:stun.l.google.com:19302", "stun:stun.l.google.com:19302" } }
            };

            _peerConnection.Connected += () =>
            {
                Debugger.Log(0, "", "Peerconnection: DONE");
            };
            _peerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Debugger.Log(0, "", $"ICE state: {newState}\n");
            };

            await _peerConnection.InitializeAsync(config);
            _dc = await _peerConnection.AddDataChannelAsync(14, "vzgo", true, true);

            Debugger.Log(0, "", "Peer connection initialized successfully.\n");

            _peerConnection.LocalSdpReadytoSend += Peer_LocalSdpReadytoSendAsync;
            _peerConnection.IceCandidateReadytoSend += Peer_IceCandidateReadytoSend;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(SignallerConstants.SignallerUrl))
                .AddJsonProtocol()
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10) })
                .Build();


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
            await _hubConnection.InvokeAsync("JoinRoom", SignallerConstants.RoomName);
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _peerConnection.Close();
            _peerConnection.Dispose();
            await _hubConnection.InvokeAsync("LeaveRoom", SignallerConstants.RoomName);
        }

        private async void Peer_LocalSdpReadytoSendAsync(string type, string sdp)
        {
            var msg = new SignallingMessage
            {
                MessageType = SignallingMessage.WireMessageTypeFromString(type),
                Data = sdp,
                IceDataSeparator = "|"
            };
            await SendMessageAsync(msg);
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
            await SendMessageAsync(msg);
        }

        private async Task SendMessageAsync(SignallingMessage message)
        {
            await _hubConnection.InvokeAsync("SendMessage", SignallerConstants.RoomName, JsonConvert.SerializeObject(message));
        }
    }
}
