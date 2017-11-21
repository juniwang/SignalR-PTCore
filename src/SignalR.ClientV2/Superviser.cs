using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.ClientV2
{
    class Superviser
    {
        private static HubConnection superviser;
        internal static CrankArguments CrankArguments = null;

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
                var pt = data.Length > 50 ? data.Substring(0, 50) + "..." : data;
                Console.WriteLine($"echo received: {pt}");
            });
            superviser.On<string>(SR.MethodBroadcast, data =>
            {
                var pt = data.Length > 50 ? data.Substring(0, 50) + "..." : data;
                Console.WriteLine($"broadcast received: {pt}");
            });

            await superviser.StartAsync();
            await superviser.SendAsync("JoinGroup", SR.SuperviserGroupName);
        }

        public static void SuperviserEcho(string message)
        {
            superviser.SendAsync(SR.MethodEcho, message);
        }

        public static void SuperviserBroadcast(string message)
        {
            superviser.SendAsync(SR.MethodBroadcast, SR.SuperviserGroupName, message);
        }

        public static void SuperviserSend(string message)
        {
            superviser.SendAsync(SR.MethodSend, SR.SuperviserGroupName, message);
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
        public static void UpdateServer(string[] args)
        {
            if (args.Length < 2)
                return;

            switch (args[1])
            {
                case "behavior":
                    ServerBehavior(args[2]);
                    break;
                case "size":
                    if (int.TryParse(args[2], out int size) && size > 0)
                    {
                        ServerBroadcastSize(size);
                    }
                    break;
                case "rate":
                    if (int.TryParse(args[2], out int rate) && rate > 0)
                    {
                        ServerBroadcastRate(rate);
                    }
                    break;
                case "start":
                    ServerStartBroadcast();
                    break;
                case "x":
                    ServerStopBroadcast();
                    break;
                case "gc":
                    ServerGC();
                    break;
                default:
                    break;
            }
        }
        private static void ServerBehavior(string behavior)
        {
            superviser.SendAsync("SetBehavior", behavior).Wait();
        }

        private static void ServerBroadcastSize(int size)
        {
            superviser.SendAsync("SetBroadcastSize", size).Wait();
        }

        private static void ServerBroadcastRate(int count)
        {
            superviser.SendAsync("SetBroadcastRate", count).Wait();
        }

        private static void ServerGC()
        {
            superviser.SendAsync("ForceGC").Wait();
        }

        private static void ServerStartBroadcast()
        {
            superviser.SendAsync("StartBroadcast").Wait();
        }

        public static void ServerStopBroadcast()
        {
            superviser.SendAsync("StopBroadcast").Wait();
        }

        #endregion
    }
}
