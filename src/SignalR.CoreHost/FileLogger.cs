using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class FileLogger
    {
        private static readonly string LogFileFormat = "perf_{0}.csv";
        private static string LogFile;
        private static bool enabled = false;

        static FileLogger()
        {
            NewLogFile();
        }


        public static void WriteToFile(PerfSample sample)
        {
            if (enabled && sample != null)
            {
                File.AppendAllText(LogFile, sample.ToLine() + Environment.NewLine);
            }
        }

        public static void NewLogFile()
        {
            try
            {
                LogFile = string.Format(LogFileFormat, DateTime.Now.ToString("yyyyMMddHHmmss"));
                File.WriteAllText(LogFile, Environment.CommandLine + Environment.NewLine);
                File.WriteAllText(LogFile, PerfSample.FileHeader + Environment.NewLine);
                enabled = true;
            }
            catch (Exception)
            {
                enabled = false;
                Console.WriteLine("Fail to create log file");
            }
        }
    }
}
