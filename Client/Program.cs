using ServerClientContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting 2 second before creating and connecting client");
            Thread.Sleep(2000);

            //Create client with a simple event for disconnect
            var client = new SimpleCrossFrameworkIPC.Client<IMySimpleService>();
            client.ClientDisconnected += (sndr, arguments) =>
                Console.WriteLine("Client disconnected (from event)");

            try
            {
                //Connect and give state of connection, return values
                client.Connect(Channel.Name, Channel.TimeoutMS);
                Console.WriteLine($"Connectionstate: {client.IsConnected()}");
                var proxy = client.GetProxy();
                Console.WriteLine($"           Text: {proxy.Text}");
                Console.WriteLine($"         Number: {proxy.Number}");
                Console.WriteLine($"          Count: {proxy.Count}");
                Console.WriteLine($"       Function: {proxy.Function()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }

            //Wait for input before a disconnect is initiated
            Console.WriteLine("Press any key to disconnect");
            Console.ReadLine();

            //Disconnect the client
            client.Disconnect();
            Console.WriteLine($"Connectionstate: {client.IsConnected()}");

            //Testing new client with a different timeout
            //Create client with a simple event for disconnect
            Console.WriteLine("Test timeout for clients, max 3 waittime for 4 second function");
            var client2 = new SimpleCrossFrameworkIPC.Client<IMySimpleService>(3000);
            client2.ClientDataReceiveTimeout += (sndr, arguments) =>
                Console.WriteLine("Client did not receive data in the given time set");
            try
            {
                //Connect and give state of connection, return values
                client2.Connect(Channel.Name, Channel.TimeoutMS);
                var proxy = client2.GetProxy();
                Console.WriteLine($"DelayedFunction: {proxy.DelayedFunction()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }

            client2.Disconnect();

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }
    }
}
