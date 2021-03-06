﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.ClientV2
{
    class PerfSample
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
            Console.WriteLine("{0} ({1}): {2} Connected, {3} Received, {4},  Avg latency {5} ms",
              TestPhase,
              Elapsed.ToString(),
              ClientsConnected.ToString(),
              MessageCount.ToString(),
              BytesAsString(),
              AvgRoundLatencyMs);
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
    }
}
