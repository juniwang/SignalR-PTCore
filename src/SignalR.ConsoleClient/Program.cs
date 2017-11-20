using Microsoft.AspNetCore.SignalR.Client;
using System;

namespace SignalR.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8080/perf")
                .WithConsoleLogger()
                .Build();

            connection.StartAsync().Wait();
            connection.On<string>("echo", data =>
            {
                Console.WriteLine($"echo received: {data}");
            });
            connection.On<string>("broadcast", data =>
            {
                Console.WriteLine($"broadcast received: {data}");
            });
            connection.On<int>("broadcastRateChanged", count =>
            {
                Console.WriteLine($"new broadcast rate: {count}");
            });
            connection.On<int>("broadcastSizeChanged", size =>
            {
                Console.WriteLine($"new broadcast size: {size}");
            });
            connection.On<string>("connectionBehaviorChanged", behavior =>
            {
                Console.WriteLine($"connectionBehavior changed: {behavior}");
            });


            if (args.Length > 1 && args[0].ToLower() == "stop")
            {
                connection.SendAsync("StopBroadcast");
            }

            string input = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(input))
            {
                var ss = input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (ss[0].ToLower())
                {
                    case "echo": // server will echo back regardless of the ConnectionBehavior
                        connection.SendAsync("Echo", ss[1]);
                        break;
                    case "broadcast": // server will broadcast regardless of the ConnectionBehavior
                        connection.SendAsync("Broadcast", ss[1]);
                        break;
                    case "send": // server will Echo, Broadcast or do nothing in according to ConnectionBehavior
                        connection.SendAsync("Send", ss[1]);
                        break;
                    case "behavior":
                        connection.SendAsync("SetBehavior", ss[1]);
                        break;
                    case "rate":
                        if (int.TryParse(ss[1], out int rate))
                        {
                            connection.SendAsync("SetBroadcastRate", rate);
                        }
                        break;
                    case "size":
                        if (int.TryParse(ss[1], out int size))
                        {
                            connection.SendAsync("SetBroadcastSize", size);
                        }
                        break;
                    case "start":
                        connection.SendAsync("StartBroadcast");
                        break;
                    case "x":
                        connection.SendAsync("StopBroadcast");
                        break;
                    default:
                        break;
                }

                input = Console.ReadLine();
            }

            connection.DisposeAsync().Wait();
        }
    }
}
