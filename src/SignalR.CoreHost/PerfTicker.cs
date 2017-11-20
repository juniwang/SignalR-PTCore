using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class PerfTicker
    {
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static int _broadcastCount = 1;

        private Lazy<HighFrequencyTimer> _timerInstance;
        private IHubContext<PerfHub> context;

        public PerfTicker(IHubContext<PerfHub> context)
        {
            this.context = context;
            _timerInstance = new Lazy<HighFrequencyTimer>(() =>
            {
                return new HighFrequencyTimer(1,
                    _ =>
                    {

                        var payloadWithTimestamp = $"C{DateTime.UtcNow.Ticks.ToString()}|{_broadcastPayload}";
                        context.Clients.All.InvokeAsync("broadcast", payloadWithTimestamp);
                    }
                );
            });
        }

        public HighFrequencyTimer Timer
        {
            get { return _timerInstance.Value; }
        }

        public void SetBroadcastRate(int count)
        {
            _broadcastCount = count;
            Timer.FPS = count;
            Console.WriteLine($"broadcastRate changed to: {count} messages per second");
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            SetBroadcastPayload();
            Console.WriteLine($"broadcastSize changed to: {size}");
        }

        internal static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}
