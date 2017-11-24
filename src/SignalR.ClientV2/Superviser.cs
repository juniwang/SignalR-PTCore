using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SignalR.ClientV2
{
    class Superviser
    {
        private static HubConnection superviser;
        internal static CrankArguments Arguments = null;
        private static ControllerEvents TestPhase = ControllerEvents.None;
        private static ConcurrentBag<HubConnection> Connections = new ConcurrentBag<HubConnection>();

        private static Stopwatch elapsedTimer; // timer to track the test process as timestamp
        private static Timer statusTimer; // timer for print out statistics periodically

        #region Superviser
        public static async Task CreateSuperviser()
        {
            superviser = ConnectionFactory.Instance.CreateConnection();

            superviser.On<string, string>(SR.MethodGroup, (action, data) =>
            {
                Console.WriteLine(data);
            });
            superviser.On<string, string>(SR.MethodServer, (action, data) =>
            {
                switch (action)
                {
                    default:
                        Console.WriteLine(data);
                        break;
                }
            });
            superviser.On<string>(SR.MethodEcho, data =>
            {
                if (Arguments.Verbose)
                {
                    var pt = data.Length > 50 ? data.Substring(0, 50) + "..." : data;
                    Console.WriteLine($"echo received: {pt}");
                }
            });
            superviser.On<string>(SR.MethodBroadcast, data =>
            {
                if (Arguments.Verbose)
                {
                    var pt = data.Length > 50 ? data.Substring(0, 50) + "..." : data;
                    Console.WriteLine($"broadcast received: {pt}");
                }
            });
            superviser.On<string, string>(SR.SuperviserGroupName, (action, args) =>
            {
                //Console.WriteLine($"instruction `{action}` with args `{args}` received.");
                OnInstructionReceived(action, args);
            });

            await superviser.StartAsync();
            await JoinGroup(superviser, SR.SuperviserGroupName);
            StartStatLoops();

            Run();
        }

        public static void TryEcho(string message)
        {
            superviser.SendAsync(SR.MethodEcho, message);
        }

        public static void TryBroadcast(string message)
        {
            superviser.SendAsync(SR.MethodBroadcast, message, SR.SuperviserGroupName);
        }

        public static void TrySend(string message)
        {
            superviser.SendAsync(SR.MethodSend, message, SR.SuperviserGroupName);
        }

        public static async Task CloseSuperviser()
        {
            if (superviser != null)
            {
                await superviser.DisposeAsync();
            }
        }
        #endregion

        #region Server-side broadcast operations
        public static void ConfigServer(params string[] args)
        {
            if (args.Length < 2)
                return;

            string action = args[1].ToLower();
            string parameter = args.Length > 2 ? string.Join(",", args.Skip(2)) : string.Empty;
            superviser.SendAsync("ServerConfig", action, parameter).Wait();
        }
        #endregion

        #region Superviser communication
        public static void ClientOps(string[] args)
        {
            if (args.Length < 2)
                return;

            string action = args[1].ToLower();
            string parameter = args.Length > 2 ? string.Join(",", args.Skip(2)) : string.Empty;
            SuperviserBroadcast(action, parameter).Wait(); // connect, disconnect, send
        }

        private static void OnInstructionReceived(string action, string args)
        {
            switch (action)
            {
                case "connect":
                    Console.WriteLine("connecting to server...");
                    SignalPhaseChange(ControllerEvents.Connect);
                    break;
                case "disconnect":
                    Console.WriteLine("disconnecting all clients...");
                    SignalPhaseChange(ControllerEvents.Disconnect);
                    break;
                case "start":
                    Console.WriteLine("Begin to send messages...");
                    SignalPhaseChange(ControllerEvents.Send);
                    break;
                case "stop":
                    Console.WriteLine("Stop sending messages...");
                    SignalPhaseChange(ControllerEvents.Idle);
                    break;
                case "batchsize":
                    if (int.TryParse(args, out int batchsize) && batchsize > 0)
                    {
                        Arguments.BatchSize = batchsize;
                        Console.WriteLine($"BatchSize updated to {batchsize}");
                    }
                    break;
                case "connectinterval":
                    if (int.TryParse(args, out int connectinterval) && connectinterval > 0)
                    {
                        Arguments.ConnectInterval = connectinterval;
                        Console.WriteLine($"ConnectInterval updated to {connectinterval}");
                    }
                    break;
                case "connections":
                    if (int.TryParse(args, out int connections) && connections > 0)
                    {
                        Arguments.Connections = connections;
                        Console.WriteLine($"Connections updated to {connections}");
                    }
                    break;
                case "sendbytes":
                    if (int.TryParse(args, out int sendbytes) && sendbytes > 0)
                    {
                        Arguments.SendBytes = sendbytes;
                        Console.WriteLine($"SendBytes updated to {sendbytes}");
                    }
                    break;
                case "sendinterval":
                    if (int.TryParse(args, out int sendinterval) && sendinterval > 0)
                    {
                        Arguments.SendInterval = sendinterval;
                        Console.WriteLine($"SendInterval updated to {sendinterval}");
                    }
                    break;
                case "bc":
                    if (int.TryParse(args, out int bc) && bc > 0)
                    {
                        Arguments.Broadcasters = bc;
                        Console.WriteLine($"Broadcaster number updated to {bc}");
                    }
                    break;
                case "gc":
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Console.WriteLine("Force Client GC done.");
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Connections
        private static void Run()
        {
            ThreadPool.QueueUserWorkItem(_ => RunConnect().Wait());
            ThreadPool.QueueUserWorkItem(_ => RunSend().Wait());
            ThreadPool.QueueUserWorkItem(_ => RunDisconnect());
        }

        private static async Task RunConnect()
        {
            while (true)
            {
                if (TestPhase == ControllerEvents.Connect && Connections.Count < Arguments.Connections)
                {
                    var batched = Arguments.BatchSize > 1;

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
                else
                {
                    await Task.Delay(CrankArguments.ConnectionPollIntervalMS);
                }
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
            var connection = ConnectionFactory.Instance.CreateConnection();
            try
            {
                connection.On<string>(SR.MethodEcho, data =>
                {
                    OnMessageReceived(data);
                });
                connection.On<string>(SR.MethodBroadcast, data =>
                {
                    OnMessageReceived(data);
                });

                await connection.StartAsync();
                Connections.Add(connection);
                await JoinGroup(connection, Environment.MachineName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection.Start Failed: {0}: {1}", e.GetType(), e.Message);
            }
        }

        private static async Task RunSend()
        {
            while (true)
            {
                if (TestPhase == ControllerEvents.Send)
                {
                    if (Arguments.SendBytes > 0 && Arguments.Broadcasters > 0)
                    {
                        await Task.WhenAll(Connections.Take(Arguments.Broadcasters).Select(c =>
                        {
                            // Send beginning timestamp string which starts with C for latency calculation.
                            var timestamp = $"C{DateTime.UtcNow.Ticks.ToString()}|";
                            var payloadWithTimestamp = timestamp.PadRight(Math.Max(Arguments.SendBytes, 20), 'a');
                            try
                            {
                                return c.InvokeAsync(SR.MethodSend, payloadWithTimestamp, Environment.MachineName);
                            }
                            catch (Exception)
                            {
                                return Task.FromResult(string.Empty);
                            }

                        }));
                    }
                }

                await Task.Delay(Arguments.SendInterval);
            }
        }

        private static void RunDisconnect()
        {
            while (true)
            {
                if (Connections.Count > 0)
                {
                    if ((TestPhase == ControllerEvents.Disconnect) || (TestPhase == ControllerEvents.Abort))
                    {
                        Parallel.ForEach(Connections, c =>
                        {
                            c.DisposeAsync().Wait();
                        });
                        Connections.Clear();
                    }
                }
                Task.Delay(CrankArguments.ConnectionPollIntervalMS).Wait();
            }
        }

        private static void OnMessageReceived(string data)
        {
            //Console.WriteLine($"Message received:{data}");
            PerfDataCollector.OnNewMessage(data);
        }
        #endregion

        #region test Phase
        private static void SignalPhaseChange(ControllerEvents phase)
        {
            if (phase != ControllerEvents.Abort)
            {
                TestPhase = phase;
            }
        }
        #endregion

        #region Statistics
        private static void StartStatLoops()
        {
            elapsedTimer = Stopwatch.StartNew();
            statusTimer = new Timer(PrintStatistics, null, 0, 1000);
        }

        private static void PrintStatistics(object state)
        {
            var sample = PerfDataCollector.GetNextSample(TestPhase.ToString(), Connections.Count, elapsedTimer.Elapsed);
            if (Arguments.Verbose)
            {
                sample?.Print();
            }
            superviser?.InvokeAsync("SendStat", sample);
        }
        #endregion

        private static async Task SuperviserBroadcast(string action, string args)
        {
            await superviser.InvokeAsync(SR.SuperviserGroupName, action, args);
        }

        private static async Task JoinGroup(HubConnection connection, string groupName)
        {
            await connection.InvokeAsync("JoinGroup", groupName);
        }
    }
}
