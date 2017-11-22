using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.ClientV2
{
    class Program
    {
        private static CrankArguments Arguments;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello SignalR Superviser!");

            Arguments = CrankArguments.Parse();
            ThreadPool.SetMinThreads(Arguments.Connections, 2);
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            ConnectionFactory.SignalRServerUri = Arguments.Url;

            Initialize().Wait();

            string input = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    var ss = input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    switch (ss[0].ToLower())
                    {
                        case "echo": // server will echo back regardless of the ConnectionBehavior
                            Superviser.TryEcho($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "broadcast": // server will broadcast to all supervisers regardless of the ConnectionBehavior
                            Superviser.TryBroadcast($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "send": // server will Echo, Broadcast or do nothing in according to ConnectionBehavior, supervisers only
                            Superviser.TrySend($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "server":
                            Superviser.ConfigServer(ss);
                            break;
                        case "client":
                            Superviser.ClientOps(ss);
                            break;
                        case "x": // a quick command for `server stop`
                            Superviser.ConfigServer("server", "stop");
                            break;
                        case "v":
                            Arguments.Verbose = !Arguments.Verbose;
                            break;
                        default:
                            break;
                    }
                }
                catch
                {
                    Console.WriteLine("input invalid");
                }

                input = Console.ReadLine();
            }

            Shutdown().Wait();
        }

        static async Task Initialize()
        {
            // start superviser
            Superviser.Arguments = Arguments;
            await Superviser.CreateSuperviser();
        }

        static async Task Shutdown()
        {
            await Superviser.CloseSuperviser();
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }
    }
}
