using ServerClientContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
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
            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }
    }
}
