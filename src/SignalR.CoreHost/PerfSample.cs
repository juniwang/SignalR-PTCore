using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.CoreHost
{
    public class PerfSample
    {

        public string Machine { get; set; }
        public string TestPhase { get; set; }
        public TimeSpan Elapsed { get; set; }
        public int ClientsConnected { get; set; }
        public int MessageCount { get; set; }
        public int MessageCountLessThan100 { get; set; }
        public int MessageCountLessThan250 { get; set; }
        public int MessageCountLessThan500 { get; set; }
        public int MessageCountLessThan1000 { get; set; }
        public int MessageCountLessThan2000 { get; set; }
        public int MessageCountGreaterThan2000 { get; set; }
        public long TotalMessageBytes { get; set; }
        public int AvgSendLatencyMs { get; set; }
        public int AvgRoundLatencyMs { get; set; }

        public void Print()
        {
            Console.WriteLine("[{6}]{0} ({1}): {2} Connected, {3} Received, {4},  Avg latency {5} ms",
              TestPhase,
              Elapsed.ToString(),
              ClientsConnected.ToString(),
              MessageCount.ToString(),
              BytesAsString(),
              AvgRoundLatencyMs,
              Machine);
        }

        private string BytesAsString()
        {
            if (TotalMessageBytes < 1024)
                return $"{TotalMessageBytes} Bytes";
            else if (TotalMessageBytes < 10485760)
            {
                return $"{ TotalMessageBytes / 1024} KB";
            }
            else
            {
                return $"{ TotalMessageBytes / 1048576} MB";
            }
        }

        public static string FileHeader
        {
            get
            {
                return "Machine,TestPhase,Elapsed," +
                    "ClientsConnected,MessageCount,MessageCountLessThan100," +
                    "MessageCountLessThan250,MessageCountLessThan500,MessageCountLessThan1000," +
                    "MessageCountLessThan2000,MessageCountGreaterThan2000,TotalMessageBytes," +
                    "AvgSendLatencyMs,AvgRoundLatencyMs";
            }
        }

        public string ToLine()
        {
            return $"{Machine},{TestPhase},{Elapsed}," +
                $"{ClientsConnected},{MessageCount},{MessageCountLessThan100}," +
                $"{MessageCountLessThan250},{MessageCountLessThan500},{MessageCountLessThan1000}," +
                $"{MessageCountLessThan2000},{MessageCountGreaterThan2000},{TotalMessageBytes}," +
                $"{AvgSendLatencyMs},{AvgRoundLatencyMs}";
        }
    }
}
