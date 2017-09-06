using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeDToolkit.Interfaces
{
    public interface IConductor : IDisposable
    {
        event Action StreamAdded;
        event Action StreamRemoved;
        event Action PeerConnectionCreated;
        event Action PeerConnectionDestroyed;
        event Action<Exception> Error;
        event Action<int, string> PeerMessage;
        event Action<int, string> PeerData;
        event Action<IConnectionStatistics> ConnectionStatus;
        
        ISignaller Signaller
        {
            get;
            set;
        }

        IList<IIceServer> IceServers
        {
            get;
        }
        
        void CreatePeerConnection(IPeer peer, string sdpOffer);

        void CreateSdpOffer(IPeer peer);

        bool TryGetTexture(uint width, uint height, out IntPtr texturePtr);
    }
}
