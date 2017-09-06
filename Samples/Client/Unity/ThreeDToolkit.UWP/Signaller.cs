using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ThreeDToolkit.Interfaces;
using ThreeDToolkit.Models;

namespace ThreeDToolkit
{
    public class Signaller : ISignaller
    {
        public IEnumerable<IPeer> Peers
        {
            get
            {
                return peers;
            }
        }

        public int HeartbeatInterval
        {
            get
            {
                return heartbeatInterval;
            }
            set
            {
                this.heartbeatInterval = value;

                if (this.connected && this.heartbeatTimer == null)
                {
                    this.heartbeatTimer = new Timer(ExecuteHeartbeatGet, null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(this.heartbeatInterval));
                }

                if (this.heartbeatTimer != null)
                {
                    this.heartbeatTimer.Change(0, this.heartbeatInterval);
                }
            }
        }

        public IDictionary<string, string> HttpHeaders
        {
            get
            {
                return headers;
            }
        }

        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        public event Action Connected;
        public event Action<int> Disconnected;
        public event Action<Exception> Error;
        public event Action<int> Heartbeat;
        public event Action<IPeer, string> Message;
        public event Action<IPeer> PeerConnected;
        public event Action<IPeer> PeerDisconnected;

        private bool disposedValue = false;
        private List<IPeer> peers = new List<IPeer>();
        private int heartbeatInterval = 0;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private bool connected = false;
        private Timer heartbeatTimer;
        private Task hangingGetThread;
        private CancellationTokenSource hangingGetCancel = new CancellationTokenSource();
        private List<IAsyncResult> pendingRequests = new List<IAsyncResult>();
        private Peer peer;
        private Uri connectUri;

        public void Connect(Uri uri, string peerName)
        {
            if (this.connected)
            {
                throw new InvalidOperationException("You must first Disconnect");
            }

            this.connectUri = uri;

            var req = new HttpRequest(uri)
                .Get("/sign_in")
                .AppendQuery("peer_name", peerName)
                .DoneAsync((HttpWebResponse res) =>
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        connected = true;

                        if (Connected != null)
                        {
                            Connected();
                        }

                        var pragmaId = int.Parse(res.Headers["Pragma"]);

                        peer = new Peer()
                        {
                            Id = pragmaId,
                            Name = peerName
                        };

                        using (var sr = new StreamReader(res.GetResponseStream()))
                        {
                            var data = sr.ReadToEnd();

                            ParseAndUpdatePeers(data);
                        }

                        StartHangingGet();

                        if (this.heartbeatInterval > 0)
                        {
                            this.heartbeatTimer = new Timer(this.ExecuteHeartbeatGet, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(this.heartbeatInterval));
                        }
                    }
                    else
                    {
                        throw new WebException("Unable to connect", WebExceptionStatus.UnknownError);
                    }
                }, (Exception ex) =>
                {
                    if (Error != null)
                    {
                        Error(ex);
                    }
                });
            this.pendingRequests.Add(req);
        }

        private void ExecuteHeartbeatGet(object state)
        {
            if (!this.connected)
            {
                return;
            }

            var req = new HttpRequest(this.connectUri)
                .Get("/heartbeat")
                .AppendQuery("peer_id", peer.Id)
                .DoneAsync((HttpWebResponse res) =>
                {
                    if (this.Heartbeat != null)
                    {
                        this.Heartbeat((int)res.StatusCode);
                    }
                }, (Exception ex) =>
                {
                    if (Error != null)
                    {
                        Error(ex);
                    }
                });
            this.pendingRequests.Add(req);
        }

        private void StartHangingGet()
        {
            if (!this.connected)
            {
                return;
            }

            // kill any existing hanging get thread
            if (this.hangingGetThread != null && !this.hangingGetThread.IsCompleted)
            {
                this.hangingGetCancel.Cancel();
                this.hangingGetCancel = new CancellationTokenSource();
                this.hangingGetThread.Wait();
            }

            // create it
            this.hangingGetThread = new Task(() =>
            {
                while (this.connected)
                {
                    var res = new HttpRequest(this.connectUri)
                        .Get("/wait")
                        .AppendQuery("peer_id", peer.Id)
                        .Sync();

                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        var pragmaId = int.Parse(res.Headers["Pragma"]);

                        using (var sr = new StreamReader(res.GetResponseStream()))
                        {
                            var data = sr.ReadToEnd();

                            if (pragmaId == this.peer.Id)
                            {
                                ParseAndUpdatePeers(data);
                            }
                            else
                            {
                                if (data == "BYE")
                                {
                                    if (this.PeerDisconnected != null)
                                    {
                                        this.PeerDisconnected(this.peers.First(p => p.Id == pragmaId));
                                    }
                                }
                                else
                                {
                                    if (this.Message != null)
                                    {
                                        this.Message(this.peers.First(p => p.Id == pragmaId), data);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Error != null)
                        {
                            Error(new WebException("Unable to wait", WebExceptionStatus.UnknownError));
                        }
                    }
                }
            }, this.hangingGetCancel.Token);

            // start it
            this.hangingGetThread.Start();
        }

        private void ParseAndUpdatePeers(string body)
        {
            // oh boy, new peers
            var oldPeers = new List<IPeer>(this.peers);

            // peer updates aren't incremental
            this.peers.Clear();

            foreach (var line in body.Split('\n'))
            {
                var parts = line.Split(',');

                if (parts.Length != 3)
                {
                    continue;
                }

                var entry = new Peer()
                {
                    Name = parts[0],
                    Id = int.Parse(parts[1])
                };

                // add the new ones (ignore ourself)
                if (!entry.Equals(this.peer))
                {
                    this.peers.Add(entry);
                }
            }

            foreach (var peer in this.peers)
            {
                if (!oldPeers.Contains(peer) && this.PeerConnected != null)
                {
                    this.PeerConnected(peer);
                }
                oldPeers.Remove(peer);
            }

            // handles disconnects that aren't respectful (and don't send /sign_out)
            foreach (var oldPeer in oldPeers)
            {
                if (this.PeerDisconnected != null)
                {
                    this.PeerDisconnected(oldPeer);
                }
            }
        }

        public void Disconnect()
        {
            if (!this.connected)
            {
                return;
            }

            var req = new HttpRequest(this.connectUri)
                .Get("/sign_out")
                .AppendQuery("peer_id", this.peer.Id)
                .DoneAsync((HttpWebResponse res) =>
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        connected = false;

                        if (Disconnected != null)
                        {
                            Disconnected((int)res.StatusCode);
                        }
                    }
                    else
                    {
                        throw new WebException("Unable to disconnect", WebExceptionStatus.UnknownError);
                    }
                }, (Exception ex) =>
                {
                    if (Error != null)
                    {
                        Error(ex);
                    }
                });
            this.pendingRequests.Add(req);
        }

        public void Send(IPeer peer, string message)
        {
            var req = new HttpRequest(this.connectUri)
                .Post("/message")
                .Data(message)
                .AppendQuery("peer_id", this.peer.Id)
                .AppendQuery("to", peer.Id)
                .DoneAsync((HttpWebResponse res) =>
                {
                    if (res.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebException("Unable to message", WebExceptionStatus.UnknownError);
                    }
                }, (Exception ex) =>
                {
                    if (Error != null)
                    {
                        Error(ex);
                    }
                });
            this.pendingRequests.Add(req);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.heartbeatTimer != null)
                    {
                        this.heartbeatTimer.Dispose();
                    }

                    if (this.hangingGetThread != null && !this.hangingGetThread.IsCompleted)
                    {
                        this.hangingGetCancel.Cancel();
                        this.hangingGetThread.Wait();
                    }

                    this.Disconnect();
                    var disconnectReq = this.pendingRequests.Last();
                    if (!disconnectReq.IsCompleted)
                    {
                        disconnectReq.AsyncWaitHandle.WaitOne();
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
