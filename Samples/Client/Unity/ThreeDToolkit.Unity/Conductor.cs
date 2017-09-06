using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThreeDToolkit.Interfaces;

namespace ThreeDToolkit
{
    public class Conductor : IConductor
    {
        public ISignaller Signaller
        {
            get
            {
                return this.signaller;
            }

            set
            {
                this.signaller = value;
                this.signaller.Message += Signaller_Message;
                this.signaller.PeerDisconnected += Signaller_PeerDisconnected;
            }
        }

        public IList<IIceServer> IceServers
        {
            get
            {
                return this.iceServers;
            }
        }

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
        private IPeer currentPeer;

        public void CreatePeerConnection(IPeer peer, string sdpOffer)
        {
            if (this.PeerConnectionCreated != null)
            {
                this.PeerConnectionCreated();
            }

            if (this.StreamAdded != null)
            {
                this.StreamAdded();
            }
        }

        public void CreateSdpOffer(IPeer peer)
        {
            // TODO(bengreenier): support unity client session initiation
            throw new NotImplementedException();
        }

        public bool TryGetTexture(uint width, uint height, out IntPtr texturePtr)
        {
            texturePtr = IntPtr.Zero;
            return false;
        }

        private void StopStream()
        {
            if (this.StreamRemoved != null)
            {
                this.StreamRemoved();
            }

            if (this.PeerConnectionDestroyed != null)
            {
                this.PeerConnectionDestroyed();
            }
        }

        private void Signaller_Message(IPeer arg1, string arg2)
        {
            // check if the message is an sdp descriptor
            if (new Regex("{\\s+\"sdp\"\\s+:\\s+").IsMatch(arg2))
            {
                this.currentPeer = arg1;
                this.CreatePeerConnection(arg1, arg2);
            }
        }

        private void Signaller_PeerDisconnected(IPeer obj)
        {
            if (this.currentPeer != null && this.currentPeer.Equals(obj))
            {
                this.StopStream();
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.signaller != null)
                    {
                        this.signaller.Message -= Signaller_Message;
                        this.signaller.PeerDisconnected -= Signaller_PeerDisconnected;
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
