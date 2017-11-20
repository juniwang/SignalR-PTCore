using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class PerfHub : Hub
    {
        private readonly PerfTicker perfTicker;
        private static ConnectionBehavior connectionBehavior = ConnectionBehavior.ListenOnly;

        public PerfHub(PerfTicker perfTicker)
        {
            this.perfTicker = perfTicker;
        }

        public async Task Echo(string message)
        {
            //Console.WriteLine("New echo message arrived:" + message);
            await CurrentClient.InvokeAsync("echo", $"E{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        public async Task Broadcast(string message)
        {
            //Console.WriteLine("New broadcast message arrived:" + message);
            await Clients.All.InvokeAsync("broadcast", $"B{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        public async Task Send(string message)
        {
            //Console.WriteLine("New client message arrived:" + message);
            switch (connectionBehavior)
            {
                case ConnectionBehavior.Echo:
                    await Echo(message);
                    break;
                case ConnectionBehavior.Broadcast:
                    await Broadcast(message);
                    break;
                default:
                    break;
            }
        }

        internal static void Init()
        {
            PerfTicker.SetBroadcastPayload();
        }

        public void SetBehavior(string behavior)
        {
            if (Enum.TryParse(behavior, out ConnectionBehavior b))
            {
                connectionBehavior = b;
                Others.InvokeAsync("connectionBehaviorChanged", b.ToString());
            }
        }

        public void SetBroadcastSize(int size)
        {
            perfTicker.SetBroadcastSize(size);
            Others.InvokeAsync("broadcastSizeChanged", size);
        }

        public void SetBroadcastRate(int count)
        {
            perfTicker.SetBroadcastRate(count);
            Others.InvokeAsync("broadcastRateChanged", count);
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Force GC done.");
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
    }
}
