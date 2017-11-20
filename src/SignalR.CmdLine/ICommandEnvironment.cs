using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.CmdLine
{
    public interface ICommandEnvironment
    {
        string CommandLine { get; }

        string[] GetCommandLineArgs();

        string Program { get; }
    }
}
