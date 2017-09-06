using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeDToolkit.Interfaces
{
    public interface IIceServer
    {
        string Uri
        {
            get;
        }

        string Username
        {
            get;
        }

        string Password
        {
            get;
        }
    }
}
