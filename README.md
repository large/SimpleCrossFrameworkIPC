# SimpleCrossFrameworkIPC
Simple IPC between .net and .core

This is a lightversion of a IPC to be used on either .net or .core and even mixed if you like.
The "fix" for the conversion is a hack and might not work for later releases.

As for 20.09.2020 it works as expected between netstandard 2.0 and .net 4.7.2.

Class is based on KrakenIPC: https://github.com/darksody/KrakenIPC and Full Duplex Pipes: https://www.codeproject.com/Articles/1179195/Full-Duplex-Asynchronous-Read-Write-with-Named-Pip

## Pipes are old, why did you mesh this up
I had a asp.core application that needed to get data from a .net472 application and the only IPC that actually worked was gRPC ( https://grpc.io/ ).
gRPC was overkill for my project so I wanted something simpler and tiny.

#### Updated 20.09.2020 ####
Added a custom delay for the time to wait for server data for the client.
Changed return method to throw an exception instead of null, to make it easier to handle timeouts in the future.
Also an event is added, to ensure that you catch everything if you decide to suppress exceptions.

## Usage
Server and Client need to share a common interface.
```c#
    public interface ISimple
    {
        int Number { get; }
        string Text { get; }
    }
```

Server contains the data in the interface, here presented as static values.
You can use most common data types; int, string, char, float, double, long etc...
```c#
    public class Simple : ISimple
    {
        public int Number { get => 111; }
        public string Text { get => "Some string"; }
    }
```

Now create the server, all it needs is the channelname.
Note that this pipe only works on localhost

```c#
    try
    {
        //Then create server
        var handler = new SimpleCrossFrameworkIPC.Server<Simple, ISimple>();
        handler.Start("Channel");

        //Pause for clients
        Console.ReadLine();

        //Stop server
        handler.Stop();
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
```

When a client connects it will refer to the same interface.
After connection are used to receive data from the server



```c#
    int nWaitForServerDataDelay = 2000; //2 sec max waiting for data
    var client = new SimpleCrossFrameworkIPC.Client<IMySimpleService>(nWaitForServerDataDelay);

    try
    {
        //Connect with a 1 second connection timeout
        client.Connect("Channel", 1000);
        var proxy = client.GetProxy();

        //Print proxy-data to the console
        Console.WriteLine("Text: " + proxy.Text);
        Console.WriteLine("Number: " + proxy.Number.ToString());
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
```
Exceptionhandling is needed for the pipeconnection throws a "Connection timout" and other errors.

## Complexity
I have never used this class for complex classes and support for this is unknown.


# Events and public functions

## Server
### Events
```c#
        public event EventHandler<EventArgs> ClientConnected;
        public event EventHandler<EventArgs> ClientDisconnected;
```
### Functions
```c#
        void Start(string Pipename)
        void Stop()
        public T GetProxy()
```

## Client
### Events
```c#
        public event EventHandler<EventArgs> ClientDisconnected;
```
### Functions
```c#
        void Connect(string Pipename)
        void Connect(string Pipename, int Timeout)
        public void Disconnect()
        public bool IsConnected()
        void UseProxy(Action<T> callback)
        public T GetProxy()
```

## Contribution
Any contribution is welcome :)
