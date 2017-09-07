using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeDToolkit.Interfaces;

namespace ThreeDToolkit.Models
{
    public class Peer : IPeer
    {
        public int Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public bool Equals(IPeer peer)
        {
            return this.Id.Equals(peer.Id) &&
                this.Name.Equals(peer.Name);
        }

        public override bool Equals(object obj)
        {
            if (obj is IPeer)
            {
                return Equals(obj as IPeer);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return string.Format("[Peer({0}): {1}]", this.Id, this.Name);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^
                this.Name.GetHashCode();
        }
    }
}
