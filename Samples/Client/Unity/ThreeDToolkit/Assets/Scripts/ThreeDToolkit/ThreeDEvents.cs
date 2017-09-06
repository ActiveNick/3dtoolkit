using System;
using ThreeDToolkit.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDToolkit
{
    [RequireComponent(typeof(ThreeDControl))]
    public class ThreeDEvents : MonoBehaviour
    {
        [Serializable]
        public class GenericWrappedEvent : UnityEvent
        {
        }

        [Serializable]
        public class GenericWrappedEventEX : UnityEvent<Exception>
        {
        }

        [Serializable]
        public class GenericWrappedEventI : UnityEvent<int>
        {
        }

        [Serializable]
        public class GenericWrappedEventIP : UnityEvent<IPeer>
        {
        }

        [Serializable]
        public class GenericWrappedEventICS : UnityEvent<IConnectionStatistics>
        {
        }

        [Serializable]
        public class GenericWrappedEventIS : UnityEvent<int, string>
        {
        }

        [Serializable]
        public class GenericWrappedEventIPS : UnityEvent<IPeer, string>
        {
        }

        // Conductor events
        public GenericWrappedEvent StreamAdded;
        public GenericWrappedEvent StreamRemoved;
        public GenericWrappedEvent PeerConnectionCreated;
        public GenericWrappedEvent PeerConnectionDestroyed;
        public GenericWrappedEventEX ConductorError;
        public GenericWrappedEventIS PeerMessage;
        public GenericWrappedEventIS PeerData;
        public GenericWrappedEventICS ConnectionStatus;

        // Signaller events
        public GenericWrappedEvent Connected;
        public GenericWrappedEventI Disconnected;
        public GenericWrappedEventEX SignallerError;
        public GenericWrappedEventI Heartbeat;
        public GenericWrappedEventIPS Message;
        public GenericWrappedEventIP PeerConnected;
        public GenericWrappedEventIP PeerDisconnected;

        private void Start()
        {
            var control = this.GetComponent<ThreeDControl>();

            RewireEvents(control);
        }

        public void RewireEvents(ThreeDControl control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            // conductor wiring
            control.Conductor.StreamAdded += () => this.StreamAdded.Invoke();
            control.Conductor.StreamRemoved += () => this.StreamRemoved.Invoke();
            control.Conductor.PeerConnectionCreated += () => this.PeerConnectionCreated.Invoke();
            control.Conductor.PeerConnectionDestroyed += () => this.PeerConnectionDestroyed.Invoke();
            control.Conductor.Error += (Exception ex) => this.ConductorError.Invoke(ex);
            control.Conductor.PeerMessage += (int id, string message) => this.PeerMessage.Invoke(id, message);
            control.Conductor.PeerData += (int id, string message) => this.PeerData.Invoke(id, message);
            control.Conductor.ConnectionStatus += (IConnectionStatistics stats) => this.ConnectionStatus.Invoke(stats);

            // signaller wiring
            control.Signaller.Connected += () => this.Connected.Invoke();
            control.Signaller.Disconnected += (int code) => this.Disconnected.Invoke(code);
            control.Signaller.Error += (Exception ex) => this.SignallerError.Invoke(ex);
            control.Signaller.Heartbeat += (int code) => this.Heartbeat.Invoke(code);
            control.Signaller.Message += (IPeer peer, string msg) => this.Message.Invoke(peer, msg);
            control.Signaller.PeerConnected += (IPeer peer) => this.PeerConnected.Invoke(peer);
            control.Signaller.PeerDisconnected += (IPeer peer) => this.PeerDisconnected.Invoke(peer);
        }
    }
}
