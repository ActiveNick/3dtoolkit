using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeDToolkit.Interfaces;

namespace ThreeDToolkit.Models
{
    public class IceServer : IIceServer
    {
        public string Uri
        {
            get;
            set;
        }

        public string Username
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public bool Equals(IIceServer peer)
        {
            return this.Uri.Equals(peer.Uri) &&
                this.Username.Equals(peer.Username) &&
                this.Password.Equals(peer.Password);
        }

        public override bool Equals(object obj)
        {
            if (obj is IIceServer)
            {
                return Equals(obj as IIceServer);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return string.Format("[IceServer({0}) {1}:{2}]", this.Uri, this.Username, this.Password);
        }

        public override int GetHashCode()
        {
            return this.Uri.GetHashCode() ^
                this.Username.GetHashCode() ^
                this.Password.GetHashCode();
        }
    }
}
