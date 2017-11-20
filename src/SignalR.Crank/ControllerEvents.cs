using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.Crank
{
    public enum ControllerEvents
    {
        None,
        Connect,
        Send,
        Disconnect,
        Complete,
        Abort,
        Sample
    }
}
