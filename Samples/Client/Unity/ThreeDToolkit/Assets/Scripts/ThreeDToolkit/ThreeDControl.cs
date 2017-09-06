using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ThreeDToolkit.Interfaces;
using ThreeDToolkit.Models;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDToolkit
{
    public class ThreeDControl : MonoBehaviour
    {
        public RawImage LeftEye;

        public RawImage RightEye;

        public Vector2 StreamDimensions = new Vector2(2560, 720);

        public TextureFormat StreamTextureFormat = TextureFormat.BGRA32;

        public string ClientName;

        public string ServerUri;

        public string IceUri;

        public string IceUsername;

        public string IcePassword;

        public int HeartbeatInterval;

        public bool ConnectOnStart = true;

        public IConductor Conductor
        {
            get;
            private set;
        }

        public ISignaller Signaller
        {
            get;
            private set;
        }

        public void Connect()
        {
            this.Signaller.Connect(new Uri(this.ServerUri), this.ClientName);
        }

        public void Disconnect()
        {
            this.Signaller.Disconnect();
        }

        private void Awake()
        {
            this.Signaller = new Signaller();
            this.Conductor = new Conductor();

            // wire the signaller into the conductor
            this.Conductor.Signaller = this.Signaller;

            // add our ice server
            this.Conductor.IceServers.Add(new IceServer
            {
                Uri = this.IceUri,
                Username = this.IceUsername,
                Password = this.IcePassword
            });

            // configure our hearbeat
            this.Signaller.HeartbeatInterval = this.HeartbeatInterval;

            // subscribe to the events we need
            this.Conductor.StreamAdded += Conductor_StreamAdded;
        }

        private void Start()
        {
            if (this.ConnectOnStart)
            {
                this.Connect();
            }
        }

        private void OnDestroy()
        {
            this.Conductor.Dispose();
            this.Signaller.Dispose();
        }

        private void Conductor_StreamAdded()
        {
            IntPtr nativeTex;

            if (this.Conductor.TryGetTexture((uint)StreamDimensions.x, (uint)StreamDimensions.y, out nativeTex))
            {
                var managedTex = Texture2D.CreateExternalTexture(
                    (int)StreamDimensions.x,
                    (int)StreamDimensions.y,
                    StreamTextureFormat,
                    false,
                    false,
                    nativeTex);

                this.LeftEye.texture = managedTex;
                this.RightEye.texture = managedTex;
            }
        }
    }
}
