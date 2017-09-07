using System;
using ThreeDToolkit.Interfaces;
using UnityEngine;

namespace ThreeDToolkit
{
    [RequireComponent(typeof(ThreeDControl))]
    public class ThreeDLogger : MonoBehaviour
    {
        private void Awake()
        {
            var control = this.GetComponent<ThreeDControl>();

            // conductor wiring
            control.Conductor.StreamAdded += () => this.Log("StreamAdded");
            control.Conductor.StreamRemoved += () => this.Log("StreamRemoved");
            control.Conductor.PeerConnectionCreated += () => this.Log("PeerConnectionCreated");
            control.Conductor.PeerConnectionDestroyed += () => this.Log("PeerConnectionDestroyed");
            control.Conductor.Error += (Exception ex) => this.Log("Error", ex);
            control.Conductor.PeerMessage += (int id, string message) => this.Log("PeerMessage", id, message);
            control.Conductor.PeerData += (int id, string message) => this.Log("PeerData", id, message);
            control.Conductor.ConnectionStatus += (IConnectionStatistics stats) => this.Log("ConnectionStatus", stats);

            // signaller wiring
            control.Signaller.Connected += () => this.Log("Connected");
            control.Signaller.Disconnected += (int code) => this.Log("Disconnected", code);
            control.Signaller.Error += (Exception ex) => this.Log("Error", ex);
            control.Signaller.Heartbeat += (int code) => this.Log("Heartbeat", code);
            control.Signaller.Message += (IPeer peer, string msg) => this.Log("Message", peer, msg);
            control.Signaller.PeerConnected += (IPeer peer) => this.Log("PeerConnected", peer);
            control.Signaller.PeerDisconnected += (IPeer peer) => this.Log("PeerDisconnected", peer);
        }

        private void Log(params object[] data)
        {
            string fmt = "";

            for (var i = 0; i < data.Length; i++)
            {
                fmt += "{" + i + "} ";
            }

            Debug.LogFormat(fmt, data);
        }
    }
}
