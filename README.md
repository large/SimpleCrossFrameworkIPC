# SimpleCrossPIPE
Simple IPC between .net and .core

This is a lightversion of a IPC to be used on either .net or .core and even mixed if you like.
The "fix" for the conversion is a hack and might not work for later releases.

As for 06.04.2020 it works as expected between netstandard 2.0 and .net 4.7.2.

#Usage
Server and Client need to share a common interface.
```
    public interface ISimple
    {
        int Number { get; }
        string Text { get; }
    }
```

Server contains the data in the interface, so it needs to be filled
```
    public class Simple : ISimple
    {
        public int Number { get => 111; }
        public string Text { get => "Some string"; }
    }

    //Then create server
    var handler = new Server<Simple, ISimple>();
    handler.Start("Channel");

    //Pause for clients
    Console.ReadLine();

    //Stop server
    handler.Stop();
```

