﻿using SignalR.CmdLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.ClientV2
{
    [CommandLineArguments(Program = "dotnet run")]
    internal class CrankArguments
    {
        internal const int ConnectionPollIntervalMS = 1000;
        internal const int ConnectionPollAttempts = 25;

        private string server;

        [CommandLineParameter(Command = "?", Name = "Help", Default = false, Description = "Show Help", IsHelp = true)]
        public bool Help { get; set; }

        [CommandLineParameter(Command = "Url", Required = false, Default = "http://localhost:8080/perf", Description = "Server URL for SignalR connections")]
        public string Url { get; set; }

        [CommandLineParameter(Command = "BatchSize", Required = false, Default = 1, Description = "(Connect phase) Batch size for parallel connections. Default: 1 (batch disabled)")]
        public int BatchSize { get; set; }

        [CommandLineParameter(Command = "ConnectInterval", Required = false, Default = 1000, Description = "(Connect phase) Time in milliseconds between connection adds. Default: 1000 ms")]
        public int ConnectInterval { get; set; }

        [CommandLineParameter(Command = "Connections", Required = false, Default = 1, Description = "(Connect phase) Number of connections to open. Default: 1")]
        public int Connections { get; set; }

        [CommandLineParameter(Command = "Broadcasters", Required = false, Default = 1, Description = "(Send phase) Number of connections which send out messages. Default: 1")]
        public int Broadcasters { get; set; }

        [CommandLineParameter(Command = "SendBytes", Required = false, Default = 50, Description = "(Send phase) Payload size in bytes. Default: 50 bytes")]
        public int SendBytes { get; set; }

        [CommandLineParameter(Command = "SendInterval", Required = false, Default = 500, Description = "(Send phase) Time in milliseconds between sends. Default: 500 ms")]
        public int SendInterval { get; set; }

        [CommandLineParameter(Command = "Verbose", Required = false, Default = false, Description = "(all phases)Show or hide the console logs. Default: false")]
        public bool Verbose { get; set; }

        public string Server
        {
            get
            {
                if (server == null)
                {
                    server = GetHostName(Url);
                }
                return server;
            }
        }

        public static CrankArguments Parse()
        {
            CrankArguments args = null;
            try
            {
                args = CommandLine.Parse<CrankArguments>();
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.ArgumentHelp.Message);
                Console.WriteLine(e.ArgumentHelp.GetHelpText(Console.BufferWidth));
                Environment.Exit(1);
            }
            return args;
        }

        private static string GetHostName(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                return new Uri(url).Host;
            }
            return String.Empty;
        }
    }
}
