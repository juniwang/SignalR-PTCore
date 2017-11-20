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
