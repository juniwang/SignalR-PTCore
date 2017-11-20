# SignalR-PTCore
SignalR performance test projects against .Net Core

## SignalR server
Soucrce codes included in `src\SignalR.CoreHost`. run `dotnet run` in windows commandLine to start a server. It will then listen on port `8080` and you can connect to `http://localhost:8080/perf` to access SignalR.

## SignalR simple client
The client, which is a simple tool to configure SignalR server, is located at `src\SignalR.ConsoleClient`. Similarly you run `dotnet run` to start a client. Run in multiple commandLine will start mulliple clients. It accepts input commands to configure the SignalR server. See the source codes for details. As a quick guide, you can use following commands:

  - `echo XXX`: send message `XXX` to server and the server will echo back the message with a leading timestamp. `XXX` cannot be null or empty.
  - `broadcast XXX`: send message `XXX` to server and the server will broadcast the message to all clients with a leading timestamp. `XXX` cannot be null or empty.
  - `rate SomeInteger`: set the server-side broadcast rate. It means *SomeInteger messages per second*. 
  - `szie someInteger`: set the server-side message size in bytes.
  - `start`: start the server-side broadcasting.
  - `x`: stop the server-side broadcasting. In case the broadcast rate is too high and you cannot input anything, run `dotnet run stop` in a new command line to stop it. If you press `Ctrl C`, it will only exit the current client where the server keeps broadcasting to other clients.

Server-side messages means that the server will broadcast a fixed-size message to all clients in fixed inteval.

## Crank
codes at `src\SignalR.Crank` is for load testing which allows tens of thousands concurrent clients. It's an imitation of the original `Microsoft.AspNet.SignalR.Crank` in [SignalR Crank](https://github.com/SignalR/SignalR/tree/dev/src/Microsoft.AspNet.SignalR.Crank). But the offical version is built against .Net Framework and doesn't suppor .Net Core. That's why this project is created.

Available options for the Crank tool include:

- **/?**: Shows the help screen. The available options are also displayed if the **Url** parameter is omitted.
- **/Url**: The URL for SignalR connections. This parameter is required. For a SignalR application using the default mapping, the path will end in "/signalr".
- **/BatchSize**: The number of clients added in each batch. The default is 50.
- **/ConnectInterval**: The interval in milliseconds between adding connections. The default is 500.
- **/Connections**: The number of connections used to load-test the application. The default is 100,000.
- **/ConnectTimeout**: The timeout in seconds before aborting the test. The default is 300.
- **SendBytes**: The size of the payload sent to the server in bytes. The default is 0.
- **SendInterval**: The delay in milliseconds between messages to the server. The default is 500.
- **SendTimeout**: The timeout in milliseconds for messages to the server. The default is 300.
- **Logfile**: The filename for the logfile for the test run. The default is `crank.csv`.

### Example

The following command will test a site called `pfsignalr` on Azure that hosts an application on port 8080 with a hub named "MyHub", using 100 connections.

`dotnet run /Connections:100 /Url:http://localhost:8080/perf`
