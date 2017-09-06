using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeDToolkit.Interfaces;
using Org.WebRtc;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Data.Json;

namespace ThreeDToolkit
{
    public class Conductor : IConductor
    {
        private static readonly string SendDataChannelName = "SendDataChannel";
        private static readonly string SdpTypeName = "type";
        private static readonly string SdpTypeOfferValue = "offer";
        private static readonly string SdpAnswerSdpName = "sdp";
        private static readonly string SdpMidName = "sdpMid";
        private static readonly string SdpMidIndexName = "sdpMLineIndex";
        private static readonly string SdpCandidateName = "candidate";

        public ISignaller Signaller
        {
            get
            {
                return this.signaller;
            }

            set
            {
                if (this.signaller != null)
                {
                    // detach from the old
                    this.signaller.Message -= Signaller_Message;
                    this.signaller.PeerDisconnected -= Signaller_PeerDisconnected;
                }

                this.signaller = value;
                this.signaller.Message += Signaller_Message;
                this.signaller.PeerDisconnected += Signaller_PeerDisconnected;
            }
        }
        
        public IList<IIceServer> IceServers => iceServers;

        public event Action StreamAdded;
        public event Action StreamRemoved;
        public event Action PeerConnectionCreated;
        public event Action PeerConnectionDestroyed;
        public event Action<Exception> Error;
        public event Action<int, string> PeerMessage;
        public event Action<int, string> PeerData;
        public event Action<IConnectionStatistics> ConnectionStatus;

        private ISignaller signaller = null;
        private List<IIceServer> iceServers = new List<IIceServer>();
        private bool disposedValue = false;
        private Lazy<UnityConductorAdapter> adapter = new Lazy<UnityConductorAdapter>(() =>
        {
            return new UnityConductorAdapter();
        });
        private Media media;
        private RTCPeerConnection connection;
        private IPeer currentPeer;

        public Conductor()
        {
            WebRTC.Initialize(CoreApplication.MainView.CoreWindow.Dispatcher);

            this.media = Media.CreateMedia();
        }

        public void CreatePeerConnection(IPeer peer, string sdpOffer)
        {
            var conn = this.BuildConnection();

            var messageType = RTCSdpType.Offer;

            conn.SetRemoteDescription(new RTCSessionDescription(messageType, sdpOffer)).AsTask().Wait();

            var answer = conn.CreateAnswer().GetResults();

            conn.SetLocalDescription(answer).AsTask().Wait();

            var answerJson = new JsonObject
            {
                { Conductor.SdpTypeName, JsonValue.CreateStringValue(answer.Type.GetValueOrDefault().ToString().ToLower()) },
                { Conductor.SdpAnswerSdpName, JsonValue.CreateStringValue(answer.Sdp) }
            };

            Signaller.Send(peer, answerJson.ToString());

            this.currentPeer = peer;
        }

        public void CreateSdpOffer(IPeer peer)
        {
            var conn = this.BuildConnection();

            var offer = conn.CreateOffer().GetResults();

            conn.SetLocalDescription(offer).AsTask().Wait();

            var offerJson = new JsonObject
            {
                { Conductor.SdpTypeName, JsonValue.CreateStringValue(offer.Type.GetValueOrDefault().ToString().ToLower()) },
                { Conductor.SdpAnswerSdpName, JsonValue.CreateStringValue(offer.Sdp) }
            };

            Signaller.Send(peer, offerJson.ToString());

            this.currentPeer = peer;
        }
        
        public bool TryGetTexture(uint width, uint height, out IntPtr texturePtr)
        {
            texturePtr = IntPtr.Zero;

            try
            {
                this.adapter.Value.GetTexture(width, height, out texturePtr);
            }
            catch (Exception ex)
            {
                if (this.Error != null)
                {
                    this.Error(ex);
                }

                return false;
            }

            return true;
        }

        private RTCPeerConnection BuildConnection()
        {
            var config = new RTCConfiguration()
            {
                BundlePolicy = RTCBundlePolicy.Balanced,
                IceTransportPolicy = RTCIceTransportPolicy.All
            };

            foreach (var server in this.IceServers)
            {
                config.IceServers.Add(new RTCIceServer()
                {
                    Url = server.Uri,
                    Username = server.Username,
                    Credential = server.Password
                });
            }

            if (this.connection != null)
            {
                this.connection.Close();

                if (this.PeerConnectionDestroyed != null)
                {
                    this.PeerConnectionDestroyed();
                }
            }

            var conn = new RTCPeerConnection(config);

            this.connection = conn;

            if (this.PeerConnectionCreated != null)
            {
                this.PeerConnectionCreated();
            }

            var sendDataChannel = conn.CreateDataChannel(Conductor.SendDataChannelName, new RTCDataChannelInit()
            {
                Ordered = true
            });

            var mediaStreamConstraints = new RTCMediaStreamConstraints()
            {
                audioEnabled = true,
                videoEnabled = true
            };

            var mediaStream = media.GetUserMedia(mediaStreamConstraints).GetResults();
            
            conn.OnAddStream += (MediaStreamEvent evt) =>
            {
                // TODO(bengreenier): SoC - this shouldn't occur inside here
                var remoteStream = conn.GetRemoteStreams().FirstOrDefault();
                if (remoteStream != null)
                {
                    // TODO(bengreenier): failure to cleanup
                    var remoteTrack = remoteStream.GetVideoTracks().FirstOrDefault();
                    var remoteSource = this.media.CreateMediaStreamSource(remoteTrack, 60, "media") as MediaStreamSource;
                    
                    this.adapter.Value.Play(remoteSource);
                }

                if (this.StreamAdded != null)
                {
                    this.StreamAdded();
                }
            };
            
            conn.OnRemoveStream += (MediaStreamEvent evt) =>
            {
                if (this.StreamRemoved != null)
                {
                    this.StreamRemoved();
                }
            };

            conn.AddStream(mediaStream);

            return conn;
        }

        private void Signaller_Message(IPeer arg1, string arg2)
        {
            if (JsonObject.TryParse(arg2, out JsonObject message))
            {
                // if we get a peer offer
                if (message.ContainsKey(Conductor.SdpTypeName) &&
                    message[Conductor.SdpTypeName].GetString() == Conductor.SdpTypeOfferValue)
                {
                    // create a peer
                    CreatePeerConnection(arg1, arg2);
                }
                // otherwise it had better be an ice offer
                else if (this.connection != null &&
                    message.ContainsKey(Conductor.SdpMidName) &&
                    message.ContainsKey(Conductor.SdpMidIndexName) &&
                    message.ContainsKey(Conductor.SdpCandidateName))
                {
                    // create the ice candidate representation
                    var iceCandidate = new RTCIceCandidate(message[Conductor.SdpCandidateName].GetString(),
                        message[Conductor.SdpMidName].GetString(),
                        (ushort)message[Conductor.SdpMidIndexName].GetNumber());

                    // add add it
                    this.connection.AddIceCandidate(iceCandidate).AsTask().Wait();
                }
                // otherwise it's an error
                else if (this.Error != null)
                {
                    // and we react as such
                    this.Error(new Exception("Unable to parse message"));
                }
            }
        }

        private void Signaller_PeerDisconnected(IPeer obj)
        {
            if (this.connection != null && this.currentPeer != null && this.currentPeer.Equals(obj))
            {
                this.connection.Close();
                this.connection = null;

                if (this.PeerConnectionDestroyed != null)
                {
                    this.PeerConnectionDestroyed();
                }
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.connection != null)
                    {
                        this.connection.Close();
                    }

                    if (this.media != null)
                    {
                        this.media.Dispose();
                    }

                    if (this.adapter.IsValueCreated)
                    {
                        this.adapter.Value.Dispose();
                    }
                }

                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
