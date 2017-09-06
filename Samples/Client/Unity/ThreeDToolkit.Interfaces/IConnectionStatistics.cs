namespace ThreeDToolkit.Interfaces
{
    public interface IConnectionStatistics
    {
        //
        // Summary:
        //     Stores a description of the ICE candidate connected to a remote peer.
        string RemoteCandidateType
        {
            get;
            set;
        }

        //
        // Summary:
        //     Stores a description of the ICE candidate connected to this peer.
        string LocalCandidateType
        {
            get;
            set;
        }

        //
        // Summary:
        //     Gets or set the round-trip time.
        long RTT
        {
            get;
            set;
        }

        //
        // Summary:
        //     Gets or sets the send bit rate in Kilobits per second.
        long SentKbps
        {
            get;
            set;
        }

        //
        // Summary:
        //     Gets or sets the number of bytes sent during the lifetime of a peer connection.
        long SentBytes
        {
            get;
            set;
        }

        //
        // Summary:
        //     Gets or sets the receive bit rate in Kilobits per second.
        long ReceivedKpbs
        {
            get;
            set;
        }

        //
        // Summary:
        //     Gets or sets the number of bytes received during the lifetime of a peer connection.
        long ReceivedBytes
        {
            get;
            set;
        }
    }
}
