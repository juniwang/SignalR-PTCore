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
                            Superviser.SuperviserEcho($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "broadcast": // server will broadcast to all supervisers regardless of the ConnectionBehavior
                            Superviser.SuperviserBroadcast($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "send": // server will Echo, Broadcast or do nothing in according to ConnectionBehavior, supervisers only
                            Superviser.SuperviserSend($"C{DateTime.UtcNow.Ticks.ToString()}|{ss[1]}");
                            break;
                        case "server":
                            Superviser.UpdateServer(ss);
                            break;
                        case "x":
                            Superviser.ServerStopBroadcast();
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
            Superviser.CrankArguments = Arguments;
            await Superviser.CreateSuperviser();
        }

        static async Task Shutdown()
        {
            await Superviser.CloseSuperviser();
        }
    }
}
