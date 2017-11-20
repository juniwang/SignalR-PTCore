using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SignalR.Crank
{
    class Program
    {
        private static CrankArguments Arguments;
        private static ConcurrentBag<HubConnection> Connections = new ConcurrentBag<HubConnection>();
        private static ControllerEvents TestPhase = ControllerEvents.None;

        static void Main(string[] args)
        {
            Arguments = CrankArguments.Parse();
            ThreadPool.SetMinThreads(Arguments.Connections, 2);
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Run().Wait();
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }

        private static async Task Run()
        {
            while (TestPhase != ControllerEvents.Connect)
            {
                if (TestPhase == ControllerEvents.Abort)
                {
                    Console.WriteLine("Test Aborted");
                    return;
                }

                await Task.Delay(CrankArguments.ConnectionPollIntervalMS);
            }

            await RunConnect();
            await RunSend();
            RunDisconnect();
        }

        private static async Task RunSend()
        {
            var payload = (Arguments.SendBytes == 0) ? string.Empty : new string('a', Arguments.SendBytes);
            while (TestPhase == ControllerEvents.Send)
            {
                if (Arguments.SendBytes > 0)
                {
                    await Task.WhenAll(Connections.Select(c =>
                    {
                        // Send beginning timestamp string which starts with C for latency calculation.
                        var payloadWithTimestamp = $"C {DateTime.UtcNow.Ticks.ToString()}|{payload}";
                        payloadWithTimestamp = payloadWithTimestamp.Substring(0, Math.Max(Arguments.SendBytes, 20));
                        try
                        {
                            return c.InvokeAsync()
                            return c.Send(payloadWithTimestamp);
                        }
                        catch (Exception)
                        {
                            return Task.FromResult(string.Empty);
                        }

                    }));
                }

                await Task.Delay(Arguments.SendInterval);
            }
        }

        internal static void OnPhaseChanged(ControllerEvents phase)
        {
            Debug.Assert(phase != ControllerEvents.None);
            Debug.Assert(phase != ControllerEvents.Sample);

            TestPhase = phase;
        }

        private static HubConnection CreateConnection()
        {
            var connection = new HubConnectionBuilder()
               .WithUrl(Arguments.Url)
               .WithConsoleLogger()
               .Build();

            return connection;
        }
    }
}
