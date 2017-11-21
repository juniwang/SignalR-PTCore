using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace SignalR.Crank
{
    class LatencyRecorder
    {
        static readonly int ROTATE = 100;

        static int _state = 0;
        static long[] totalTicks = new long[ROTATE];
        static int[] samples = new int[ROTATE];

        private static readonly HighFrequencyTimer _timerInstance = new HighFrequencyTimer(1,
                _ =>
                {
                    var current = Interlocked.CompareExchange(ref _state, (_state + 1) % ROTATE, _state);
                    if (samples[current] > 0)
                    {
                        long latencyMs = totalTicks[current] / samples[current] / 10000;
                        Console.WriteLine("Current:{0}, Samples: {1}, Avg Latency:{2} ms,  totalTicks: {3}",
                            current.ToString(),
                            samples[current].ToString(),
                            latencyMs.ToString(),
                             totalTicks[current]);
                        PerfCounters.SendRequestLatencyPC(latencyMs);
                        PerfCounters.SendMessageSamples(samples[current]);
                        totalTicks[current] = 0;
                        samples[current] = 0;
                    }
                }
            );

        static LatencyRecorder()
        {
            //_timerInstance.Start();
        }

        public static void UpdateLatency(string ticks, long maxTimeoutMs = 60 * 1000)
        {
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long clientTicks = 0;
                long serverTicks = 0;
                //Console.WriteLine("N" + now.ToString() + "|" + ticks);
                foreach (var tick in ticks.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    char start = tick[0];
                    switch (start)
                    {
                        case 'C': // the timestamp before sending
                            clientTicks = long.Parse(tick.Substring(1));
                            break;
                        case 'E': // the timestamp before SignalR server echo it back
                        case 'B': // the timestamp before SignalR server broadcast it.
                            serverTicks = long.Parse(tick.Substring(1));
                            break;
                        default:
                            break;
                    }
                }

                long requestLatency = now - clientTicks;
                if (requestLatency > 0 && clientTicks > 0)
                {
                    var current = Thread.CurrentThread.ManagedThreadId % ROTATE;
                    Interlocked.Add(ref totalTicks[current], requestLatency);
                    Interlocked.Increment(ref samples[current]);
                }
            }
            catch { }
        }


        public static string StatusAsString()
        {
            long ss = samples.Sum();
            long ts = totalTicks.Sum();
            if (ss > 0)
            {
                return $"Avg latency: {(ts / ss / 10000).ToString()} ms";
            }
            return string.Empty;
        }
    }
}
