using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public enum SignalRBackPlain
    {
        /// <summary>
        /// No backplain
        /// </summary>
        None,
        /// <summary>
        /// Scale out using Redis
        /// </summary>
        Redis,
        /// <summary>
        /// Scale out using ServiceBus, not implementated. SignalR doesn't support it yet.
        /// </summary>
        ServiceBus,
        /// <summary>
        /// Scale out using SQLServer, not implementated. SignalR doesn't support it yet.
        /// </summary>
        SQLServer
    }
}
