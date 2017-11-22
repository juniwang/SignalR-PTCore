using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class FileLogger
    {
        private static readonly string LogFileFormat = "perf_{0}.csv";
        private static string LogFile;
        private static bool enabled = false;
        private static Timer timer;
        private static ConcurrentBag<PerfSample> samples = new ConcurrentBag<PerfSample>();


        static FileLogger()
        {
            timer = new Timer(WriteToFileInternal, null, 0, 1000);
        }


        public static void LogSample(PerfSample sample)
        {
            if (enabled && sample != null)
            {
                samples.Add(sample);
            }
        }

        private static bool NewLogFile()
        {
            try
            {
                LogFile = string.Format(LogFileFormat, DateTime.Now.ToString("yyyyMMddHHmmss"));
                File.WriteAllText(LogFile, Environment.CommandLine + Environment.NewLine);
                File.WriteAllText(LogFile, PerfSample.FileHeader + Environment.NewLine);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Fail to create log file");
                return false;
            }
        }

        public static void StartNewSession()
        {
            enabled = NewLogFile();
        }

        public static void StopSession()
        {
            enabled = false;
        }

        private static void WriteToFileInternal(object state)
        {
            if (enabled)
            {
                var copy = samples.ToArray();
                samples.Clear();
                if (copy.Length > 0)
                {
                    File.AppendAllLines(LogFile, copy.Select(s => s.ToLine()));
                }
            }
        }
    }
}
