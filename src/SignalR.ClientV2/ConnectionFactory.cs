using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.ClientV2
{
    public class ConnectionFactory
    {
        public static string SignalRServerUri = "http://localhost:8080/perf";

        #region singleton
        private static ConnectionFactory factory = new ConnectionFactory();
        private ConnectionFactory() { }

        public static ConnectionFactory Instance
        {
            get
            {
                return factory;
            }
        }
        #endregion

        public HubConnection CreateConnection()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(SignalRServerUri)
                .WithConsoleLogger()
                .Build();

            return connection;
        }
    }
}
