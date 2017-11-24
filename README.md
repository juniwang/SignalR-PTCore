# SignalR-PTCore
Performance test projects against SignalR .Net Core. It's built on .NET Core so make sure .Net Core is correctly installed.

## SignalR server
Soucrce codes included in `src\SignalR.CoreHost`. Run `dotnet run` in windows commandLine to start a server. It will then listen on port `8080` and you can connect to `http://localhost:8080/perf` to access SignalR. 

The server actually can serve following requests:
- allow connection to join a group. Every client VM will create a special connection called superviser when initializing and it will join a special group also called superviser. Superviers are used to configure server or client VMs.
- Echo or Broadcast message with an extra leading timestamp string. Echo will send message back to the client connection while broadcast will send message to all connected clients or all connections in a group if a group name is specified.
- Trigger/stop a server-side periodical broadcasting. Server-side means the message dosn't come from any client but the server itself.
- Allow client to configure the server without any config file or downtime. For example, to change the server-side broadcasting rate and message size. The configurations are send via superviser connection.
- Can forward all client configuration too. It's useful while you want to change the client configuration but you get multiple client VMs. Simply configure any of client VM, the change will be sent to server and then be broadcasted to all client VMs through superviser connections.

SignalR server also writes the performance sample data to files. It will create a new data file every time a session begins and stop writing on a session end. A session can a server-side broadcast or client echo/broadcast round trip.

## SignalR client
The client, which is a command tool to configure SignalR server, is located at `src\SignalR.ClientV2`. Similarly you run `dotnet run` to start a client. Run in multiple commandLine will start mulliple clients. It accepts input parameters as the default value. But most of these parameters can be updated on the fly through the commandline. As above said, perform commands on any client VM is all you need to do. Following are the default startup arguments and supported commands. **Note that, this document might be out of sync since the source codes have been evolving**.

### startup arguments
you can start `dotnet run` with additional arguments:
- **/?**: Shows the help screen. The available options are also displayed if the **Url** parameter is omitted.
- **/Url**: The URL for SignalR connections. For a SignalR application using the default mapping, the path will end in "/perf". The default value is http://localhost:8080/perf. 
- **/BatchSize**: The number of clients connections added in each batch. The default is 1.
- **/ConnectInterval**: The interval in milliseconds between adding connections. The default is 500.
- **/Connections**: The number of connections used to load-test the application. The default is 1.
- **SendBytes**: The size of the payload sent to the server in bytes. The default is 0.
- **SendInterval**: The delay in milliseconds between messages to the server. The default is 500.
- **Broadcasters**: The number of connections who sends messages. The default is 1.
- **Verbose**: Show logs on commandline if it's true, apply for current client VM only. The default value is false.


### supported commands
  - **echo XXX**: send message `XXX` to server and the server will echo back the message with a leading timestamp. `XXX` cannot be null or empty.
  - **broadcast XXX**: send message `XXX` to server and the server will broadcast the message to all supervisers with a leading timestamp. `XXX` cannot be null or empty.
  - **send XXX**: send message `XXX` to server. Server will echo back, broadcast to supervisers or do nothing in terms of the `ConnectionBehavior`.
  - **server ACTION args**: configure the server. Where ACTION can be(all ACTIONs are case-insensitive):
    - **behavior**: set the `ConnectionBehavior`. Its value has to be one of **ListenOnly**, **Echo**, **Broadcast**. E.g.`server behavior Echo`.
    - **rate SomeInteger**: set the server-side broadcast rate. It means *SomeInteger messages per second*. e.g.`server rate 10`.
    - **size someInteger**: set the server-side message size in bytes. e.g.`server size 1024`
    - **start**: start a server-side broadcasting session. A new performance data file will be created on server VM. e.g.`server start`.
    - **stop**: stop the server-side broadcasting session. In case the broadcast rate is too high and you enabled console logs, you have no time to input a long comman to stop the broadcast, simply press **`x`** and Enter. It's equivalent to `server stop`.
    - **gc**: Force server to perform GC.
  - **x**: equivalent to `server stop`.
  - **client ACTION args**: configure the client VM. the client command will be broadcast to all supervisers so execute command on one of the Client, all client VMs got updated. Client ACTION can be(case-insensitive too):
    - **BatchSize**: Set the number of clients connections added in each batch. e.g.`client batchsize 50`.
    - **ConnectInterval**: Set the interval in milliseconds between adding connections. e.g.`client ConnectInterval 100`.
    - **Connections**: Set the number of connections used to load-test the application in each client VM. e.g.`client connections 1000`.
    - **SendBytes**: Set the size of the payload sent to the server in bytes. e.g. `client SendBytes 1024`.
    - **SendInterval**: Set the delay in milliseconds between messages to the server. e.g.`client SendInterval 100`.
    - **bc**: Set the number of connections who sends messages. e.g.`client bc 5`.
    - **gc**: Perform GC on client VM. e.g. `client gc`.
    - **connect**: Spin up connections to server. Only superviser connection is created by default. Other connections won't create until run `client connect`. *BatchSize* connections are created every *ConnectInterval* miliseconds until *Connections* reached.
    - **disconnect**: Disconnect all connections except the superviser.
    - **start**: Start sending message to server. Will send messages of *SendBytes* bytes and delay *SendInterval* miliseconds between messages. A new performance data file will be created on server VM. e.g. `client start`. **Note that, server might respond differently according to configuration `ConnectionBehavior`.**.
    - **stop**: Stop the session.


## Supported platforms
Only tested on Windows server. It should be working on Linux/MacOS too since it's built on .Net Core.

## Examples

#### start a server
Copy or clone source code, change directory to */src/SignalR.CoreHost* and run `dotnet run`. Say it's public DNS is *SomeDomain*.

#### start a client
Copy or clone source code, change directory to */src/SignalR.ClientV2* and run: 
```
dotnet run Url://http:SomeDomain:8080/perf
```

#### make sure it's ready for performance test
Try following commands, you can open multiple clients to validate the broadcast:
```
v
Echo stringa
Broadcast stringb
v
```

#### Server-side broadcast performance test
Try performing following commands in client VM(**Note: you don't have to run all of them in every test. For example, you can connect the connections withou disconnecting them. So they are always here for use.**):
```
client batchsize 100
client connections 1000
client connect
server rate 10
server size 128
server start
server stop
client disconnect
```

#### Client round trip test via Echo
Try performing following commands in client VM:
```
server behavior Echo
client batchsize 100
client connections 1000
client connect
client sendbytes 128
server gc
client gc
client start
client stop
client disconnect
```

#### Client round trip test via Broadcast
Try performing following commands in client VM:
```
server behavior Broadcast
client bc 5
client start
client stop
```
