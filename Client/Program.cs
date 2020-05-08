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
            var client = new SimpleCrossFrameworkIPC.Client<IMySimpleService>();
            client.ClientDisconnected += (sndr, arguments) =>
                Console.WriteLine("Client disconnected");

            try
            {
                client.Connect(Channel.Name, Channel.TimeoutMS);
                var proxy = client.GetProxy();
                Console.WriteLine("Text: " + proxy.Text, proxy.Number.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
            Console.ReadLine();
        }
    }
}
