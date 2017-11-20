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
                .WithUrl("http://localhost:61169/perf")
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

            string input = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(input))
            {
                var ss = input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (ss[0].ToLower())
                {
                    case "echo":
                        connection.SendAsync("Echo", ss[1]);
                        break;
                    case "broadcast":
                        connection.SendAsync("Broadcast", ss[1]);
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
                    case "C":
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
