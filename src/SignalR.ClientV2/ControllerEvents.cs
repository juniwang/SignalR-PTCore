using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.ClientV2
{
    public enum ControllerEvents
    {
        None,
        Connect,
        Send,
        Disconnect,
        Complete,
        Abort,
        Idle
    }
}
