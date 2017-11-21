using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SignalR.Crank
{
    class Client
    {
        private static CrankArguments Arguments;
        private static ControllerEvents TestPhase = ControllerEvents.None;
        private static ConcurrentBag<HubConnection> Connections = new ConcurrentBag<HubConnection>();
        private static Stopwatch TestTimer; // timer to control phase

        private static Timer statusTimer; // timer for print out statistics periodically
        private static Stopwatch elapsedTimer; // timer to track the test process as timestamp
        private static int ClientsConnected = 0;
        private static int MessageReceived = 0;
        private static long TotalMessageBytes = 0;

        static void Main(string[] args)
        {
            Arguments = CrankArguments.Parse();
            ThreadPool.SetMinThreads(Arguments.Connections, 2);
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            StartStatLoops();
            StartPhaseController();
            Run().Wait();

            Console.ReadLine();
        }

        #region output and logs
        private static void StartStatLoops()
        {
            elapsedTimer = Stopwatch.StartNew();
            statusTimer = new Timer(PrintStatistics, null, 0, 1000);
        }

        private static void PrintStatistics(object state)
        {
            Console.WriteLine("{0} ({1}): {2} Connected, {3} Received, {4}, {5}",
                TestPhase.ToString(),
                elapsedTimer.Elapsed.ToString(),
                ClientsConnected.ToString(),
                MessageReceived.ToString(),
                BytesAsString(),
                LatencyRecorder.StatusAsString());
        }

        private static string BytesAsString()
        {
            if (TotalMessageBytes < 1024)
                return $"{TotalMessageBytes} Bytes";
            else if (TotalMessageBytes < 10485760)
            {
                return $"{ TotalMessageBytes / 1024} KB";
            }
            else
            {
                return $"{ TotalMessageBytes / 1048576} MB";
            }
        }
        #endregion

        #region Phase controll
        private static void StartPhaseController()
        {
            ThreadPool.QueueUserWorkItem(_ => RunController());
        }

        private static void RunController()
        {
            RunConnectPhase();
            RunSendPhase();
            RunDisconnectPhase();
        }

        private static void RunConnectPhase()
        {
            SignalPhaseChange(ControllerEvents.Connect);
            TestTimer = Stopwatch.StartNew();
            BlockWhilePhase(ControllerEvents.Connect, breakCondition: () =>
            {
                if (TestTimer.Elapsed >= TimeSpan.FromSeconds(Arguments.ConnectTimeout))
                {
                    return true;
                }

                return ClientsConnected >= Arguments.Connections;
            });

            SignalPhaseChange(ControllerEvents.Send);
        }

        private static void RunSendPhase()
        {
            var timeout = TestTimer.Elapsed.Add(TimeSpan.FromSeconds(Arguments.SendTimeout));

            BlockWhilePhase(ControllerEvents.Send, breakCondition: () =>
            {
                return TestTimer.Elapsed >= timeout;
            });

            SignalPhaseChange(ControllerEvents.Disconnect);
        }

        private static void RunDisconnectPhase()
        {
            var timeout = TestTimer.Elapsed.Add(TimeSpan.FromSeconds(Arguments.ConnectTimeout));
            BlockWhilePhase(ControllerEvents.Disconnect, breakCondition: () =>
            {
                if (TestTimer.Elapsed >= timeout)
                {
                    return true;
                }

                return ClientsConnected == 0;
            });

            SignalPhaseChange(ControllerEvents.Complete);
        }

        private static void BlockWhilePhase(ControllerEvents phase, Func<bool> breakCondition = null)
        {
            while (TestPhase == phase)
            {
                if ((breakCondition != null) && breakCondition())
                {
                    break;
                }

                Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
            }
        }

        private static void SignalPhaseChange(ControllerEvents phase)
        {
            if (phase != ControllerEvents.Abort)
            {
                TestPhase = phase;
            }

            OnPhaseChanged(phase);
        }


        internal static void OnPhaseChanged(ControllerEvents phase)
        {
            Debug.Assert(phase != ControllerEvents.None);
            Debug.Assert(phase != ControllerEvents.Sample);

            TestPhase = phase;
        }
        #endregion

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }

        #region Connect, Send and Disconnect

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

        private static async Task RunConnect()
        {
            var batched = Arguments.BatchSize > 1;

            while (TestPhase == ControllerEvents.Connect)
            {
                if (batched)
                {
                    await ConnectBatch();
                }
                else
                {
                    await ConnectSingle();
                }

                await Task.Delay(Arguments.ConnectInterval);
            }
        }

        private static async Task ConnectBatch()
        {
            var tasks = new Task[Arguments.BatchSize];

            for (int i = 0; i < Arguments.BatchSize; i++)
            {
                tasks[i] = ConnectSingle();
            }

            await Task.WhenAll(tasks);
        }

        private static async Task ConnectSingle()
        {
            var connection = CreateConnection();
            try
            {
                await connection.StartAsync();
                Connections.Add(connection);
                //Console.WriteLine("Connection started.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection.Start Failed: {0}: {1}", e.GetType(), e.Message);
            }
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
                        var payloadWithTimestamp = $"C{DateTime.UtcNow.Ticks.ToString()}|{payload}";
                        payloadWithTimestamp = payloadWithTimestamp.Substring(0, Math.Max(Arguments.SendBytes, 20));
                        Console.WriteLine(payloadWithTimestamp);
                        try
                        {
                            return c.InvokeAsync("Send", payloadWithTimestamp);
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

        private static void RunDisconnect()
        {
            if (Connections.Count > 0)
            {
                if ((TestPhase == ControllerEvents.Disconnect) ||
                    (TestPhase == ControllerEvents.Abort))
                {
                    Parallel.ForEach(Connections, c =>
                    {
                        c.DisposeAsync().Wait();
                    });
                }
            }
        }
        #endregion

        #region Create connection

        private static HubConnection CreateConnection()
        {
            var connection = new HubConnectionBuilder()
               .WithUrl(Arguments.Url)
               .WithConsoleLogger()
               .Build();

            connection.On<string>("Echo", data =>
            {
                OnMessageReceived(data);
            });
            connection.On<string>("Broadcast", data =>
            {
                OnMessageReceived(data);
            });

            connection.Connected += Connection_Connected;
            connection.Closed += Connection_Closed;

            return connection;
        }

        private static Task Connection_Closed(Exception arg)
        {
            Interlocked.Decrement(ref ClientsConnected);
            return Task.FromResult(string.Empty);
        }

        private static Task Connection_Connected()
        {
            Interlocked.Increment(ref ClientsConnected);
            return Task.FromResult(string.Empty);
        }

        private static void OnMessageReceived(string data)
        {
            //Console.WriteLine($"Message received:{data}");
            UpdateStatistics(data);
            string sub = data.Substring(0, Math.Min(data.Length, 40));
            LatencyRecorder.UpdateLatency(sub);
        }

        private static void UpdateStatistics(string data)
        {
            Interlocked.Increment(ref MessageReceived);
            Interlocked.Add(ref TotalMessageBytes, data.Length);
        }
        #endregion

    }
}
