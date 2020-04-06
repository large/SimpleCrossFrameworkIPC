# SimpleCrossPIPE
Simple IPC between .net and .core

This is a lightversion of a IPC to be used on either .net or .core and even mixed if you like.
The "fix" for the conversion is a hack and might not work for later releases.

As for 06.04.2020 it works as expected between netstandard 2.0 and .net 4.7.2.

Class is based on KrakenIPC: https://github.com/darksody/KrakenIPC and Full Duplex Pipes: https://www.codeproject.com/Articles/1179195/Full-Duplex-Asynchronous-Read-Write-with-Named-Pip


## Pipes are old, why did you mesh this up
I had a asp.core application that needed to get data from a .net472 application and the only IPC that actually worked was gRPC ( https://grpc.io/ ).
gRPC was overkill for my project so I wanted something simpler and tiny.

## Usage
Server and Client need to share a common interface.
```c#
    public interface ISimple
    {
        int Number { get; }
        string Text { get; }
    }
```

Server contains the data in the interface, so it needs to be filled
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
    //Then create server
    var handler = new Server<Simple, ISimple>();
    handler.Start("Channel");

    //Pause for clients
    Console.ReadLine();

    //Stop server
    handler.Stop();
```

When a client connects it will refer to the same interface.
After connection are used to receive data from the server

```c#
    var client = new SimpleCrossPIPE.Client<IMySimpleService>();

    try
    {
        //Connect with a 1 second timeout
        client.Connect("Channel", 1000);
        var proxy = client.GetProxy();

        //Print proxy-data to the console
        Console.WriteLine("Text: " + proxy.Text);
        Console.WriteLine("Number: " + proxy.Number.ToString());
    }
    catch(Exception ex) //
    {
        Console.WriteLine(ex.ToString());
    }
```
Exceptionhandling is needed for the pipeconnection throws a "Connection timout" and other errors.

## Complexity
I do now belive it will handle complex classes, so i recommend to KISS!
