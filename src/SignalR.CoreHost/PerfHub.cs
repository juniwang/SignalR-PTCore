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

        public PerfHub(PerfTicker perfTicker)
        {
            this.perfTicker = perfTicker;
        }

        public async Task Echo(string message)
        {
            Console.WriteLine("New echo message arrived:" + message);
            await CurrentClient.InvokeAsync("echo", $"E{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        public async Task Broadcast(string message)
        {
            Console.WriteLine("New broadcast message arrived:" + message);
            await Clients.All.InvokeAsync("broadcast", $"B{DateTime.UtcNow.Ticks.ToString()}|{message}");
        }

        internal static void Init()
        {
            PerfTicker.SetBroadcastPayload();
        }

        public void SetBroadcastRate(int count)
        {
            perfTicker.SetBroadcastRate(count);
            Others.InvokeAsync("broadcastRateChanged", count);
        }

        public void SetBroadcastSize(int size)
        {
            perfTicker.SetBroadcastSize(size);
            Others.InvokeAsync("broadcastSizeChanged", size);
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
