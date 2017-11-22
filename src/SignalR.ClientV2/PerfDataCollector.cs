using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace SignalR.ClientV2
{
    class PerfDataCollector
    {
        private static readonly int ROTATE = 200;
        private static InnerCollector[] collectors;

        static PerfDataCollector()
        {
            collectors = NewCollectors();
        }

        public static void OnNewMessage(string message)
        {
            collectors[Thread.CurrentThread.ManagedThreadId % ROTATE].OnNewMessage(message);
        }

        public static PerfSample GetNextSample(string phase, int connections, TimeSpan elapsed)
        {
            // assign a new array to collect future data
            InnerCollector[] old = collectors;
            collectors = NewCollectors();
            return GetNextSample(old ?? new InnerCollector[0], phase, connections, elapsed);
        }

        private static PerfSample GetNextSample(InnerCollector[] ic, string phase, int connections, TimeSpan elapsed)
        {
            var sample = new PerfSample
            {
                TestPhase = phase,
                Machine = Environment.MachineName,
                ClientsConnected = connections,
                Elapsed = elapsed,
                MessageCountLessThan100 = ic.Sum(p => p.MessageCountLessThan100),
                MessageCountLessThan250 = ic.Sum(p => p.MessageCountLessThan250),
                MessageCountLessThan500 = ic.Sum(p => p.MessageCountLessThan500),
                MessageCountLessThan1000 = ic.Sum(p => p.MessageCountLessThan1000),
                MessageCountLessThan2000 = ic.Sum(p => p.MessageCountLessThan2000),
                MessageCountGreaterThan2000 = ic.Sum(p => p.MessageCountGreaterThan2000),
                MessageCount = ic.Sum(p => p.MessageCount),
                TotalMessageBytes = ic.Sum(p => p.TotalMessageBytes),
            };
            if (sample.MessageCount > 0)
            {
                sample.AvgSendLatencyMs = (int)(ic.Sum(p => p.TotalSendLatencyTicks) / sample.MessageCount / 10000);
                sample.AvgRoundLatencyMs = (int)(ic.Sum(p => p.TotalRoundLatencyTicks) / sample.MessageCount / 10000);
            }

            return sample;
        }

        private static InnerCollector[] NewCollectors()
        {
            var cs = new InnerCollector[ROTATE];
            for (int i = 0; i < ROTATE; i++)
            {
                cs[i] = new InnerCollector();
            }
            return cs;
        }

        class InnerCollector
        {
            public int MessageCount = 0;
            public long TotalMessageBytes = 0;
            public long TotalSendLatencyTicks = 0;
            public long TotalRoundLatencyTicks = 0;

            public int MessageCountLessThan100 = 0;
            public int MessageCountLessThan250 = 0;
            public int MessageCountLessThan500 = 0;
            public int MessageCountLessThan1000 = 0;
            public int MessageCountLessThan2000 = 0;
            public int MessageCountGreaterThan2000 = 0;

            public void OnNewMessage(string message)
            {
                if (ParseTicks(message, out long sendLatency, out long roundLatency))
                {
                    Interlocked.Increment(ref MessageCount);
                    Interlocked.Add(ref TotalMessageBytes, message.Length);
                    Interlocked.Add(ref TotalSendLatencyTicks, sendLatency);
                    Interlocked.Add(ref TotalRoundLatencyTicks, roundLatency);

                    var rms = roundLatency / 10000;
                    if (rms <= 100)
                        Interlocked.Increment(ref MessageCountLessThan100);
                    else if (rms <= 250)
                        Interlocked.Increment(ref MessageCountLessThan250);
                    else if (rms <= 500)
                        Interlocked.Increment(ref MessageCountLessThan500);
                    else if (rms <= 1000)
                        Interlocked.Increment(ref MessageCountLessThan1000);
                    else if (rms <= 2000)
                        Interlocked.Increment(ref MessageCountLessThan2000);
                    else
                        Interlocked.Increment(ref MessageCountGreaterThan2000);
                }
            }

            private bool ParseTicks(string message, out long sendLatency, out long roundLatency)
            {
                sendLatency = 0;
                roundLatency = 0;

                try
                {
                    long now = DateTime.UtcNow.Ticks;
                    long clientTicks = 0;
                    long serverTicks = 0;
                    var ticks = message.Substring(0, Math.Min(60, message.Length));
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

                    if (clientTicks > 0)
                    {
                        roundLatency = now - clientTicks;
                        if (serverTicks > 0)
                        {
                            sendLatency = serverTicks - clientTicks;
                        }
                    }
                    //Console.WriteLine($"sendLatency:{sendLatency}, roundLatency:{roundLatency}");
                    return sendLatency >= 0 && roundLatency >= 0;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
