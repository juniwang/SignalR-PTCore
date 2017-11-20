using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.CmdLine
{
    public class CommandEnvironment : ICommandEnvironment
    {
        public string CommandLine
        {
            get
            {
                return Environment.CommandLine;
            }
        }

        private string[] args;

        public string[] GetCommandLineArgs()
        {

            return args ?? (args = Environment.GetCommandLineArgs());
        }

        public string Program
        {
            get
            {
                return this.GetCommandLineArgs()[0];
            }
        }
    }
}
