using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class PerfHub : Hub
    {
        private static readonly string SuperviserGroupName = "Superviser";
        private static readonly string MethodServer = "Server";
        private static readonly string MethodClient = "Client";
        private static readonly string MethodStat = "Stat";
        private static readonly string MethodEcho = "Echo";
        private static readonly string MethodBroadcast = "Broadcast";
        private static readonly string MethodSend = "Send";
        private static readonly string MethodGroup = "Group";

        private readonly PerfTicker perfTicker;
        private static ConnectionBehavior connectionBehavior = ConnectionBehavior.ListenOnly;

        public PerfHub(PerfTicker perfTicker)
        {
            this.perfTicker = perfTicker;
        }

        #region Group
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddAsync(Context.ConnectionId, groupName);
            if (groupName == SuperviserGroupName)
            {
                await Clients.Group(groupName).InvokeAsync("Group", "Join", $"{Context.ConnectionId} joined in group `{groupName}`.");
            }
        }
        #endregion

        #region Server Config
        public void SetBehavior(string behavior)
        {
            if (Enum.TryParse(behavior, out ConnectionBehavior b))
            {
                connectionBehavior = b;
                Clients.Group(SuperviserGroupName).InvokeAsync(MethodServer, "Behavior",
                    $"SignalR server behavior changed to: { b.ToString()}");
            }
        }

        public void SetBroadcastSize(int size)
        {
            if (size > 0)
            {
                perfTicker.SetBroadcastSize(size);
                Clients.Group(SuperviserGroupName).InvokeAsync(MethodServer, "MessageSize",
                    $"SignalR server-side broadcast message size updated to: { size.ToString()}.");
            }
        }

        public void SetBroadcastRate(int count)
        {
            if (count > 0)
            {
                perfTicker.SetBroadcastRate(count);
                Clients.Group(SuperviserGroupName).InvokeAsync(MethodServer, "MessageRate",
                    $"SignalR server-side broadcast message rate sed to: { count.ToString()} message per second.");
            }
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Force Server GC done.");
        }

        public void StartBroadcast()
        {
            Console.WriteLine("Starting Broadcast...");
            perfTicker.Timer.Start();
        }

        public void StopBroadcast()
        {
            perfTicker.Timer.Stop();
            Console.WriteLine("Stop Broadcast...");
        }

        #endregion

        #region Message
        public async Task Echo(string message)
        {
            //Console.WriteLine("New echo message arrived:" + message);
            await CurrentClient.InvokeAsync(MethodEcho, $"E{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        public async Task Broadcast(string group, string message)
        {
            //Console.WriteLine("New broadcast message arrived:" + message);
            if (string.IsNullOrWhiteSpace(group))
                await Clients.All.InvokeAsync(MethodBroadcast, $"B{DateTime.UtcNow.Ticks.ToString()}|{message}");
            else
                await Clients.Group(group).InvokeAsync(MethodBroadcast, $"B{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        public async Task Send(string group, string message)
        {
            //Console.WriteLine("New client message arrived:" + message);
            switch (connectionBehavior)
            {
                case ConnectionBehavior.Echo:
                    await Echo(message);
                    break;
                case ConnectionBehavior.Broadcast:
                    await Broadcast(group, message);
                    break;
                default:
                    break;
            }
        }
        #endregion

        internal static void Init()
        {
            PerfTicker.SetBroadcastPayload();
        }

        #region Clients
        private IClientProxy Others
        {
            get
            {
                return Clients.AllExcept(new List<string> { Context.ConnectionId });
            }
        }

        private IClientProxy CurrentClient
        {
            get
            {
                return Clients.Client(Context.ConnectionId);
            }
        }
        #endregion
    }
}
