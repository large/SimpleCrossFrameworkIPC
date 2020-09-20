using ServerClientContract;
using SimpleCrossFrameworkIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class MySimpleService : IMySimpleService
    {
        public int Number { get => 111; }
        public string Text { get => "Some string"; }
        public int Count { get; set; }

        //Simple function to show how to handle the receiveclientTimeout event
        public bool DelayedFunction()
        {
            Thread.Sleep(4000);
            return true;
        }

        public int Function()
        {
            return Count * 12;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Create server with spesified class and interface
            var handler = new SimpleCrossFrameworkIPC.Server<MySimpleService, IMySimpleService>();
            int nCount = 0;

            Console.WriteLine("Server starting channel: " + Channel.Name);
            Console.WriteLine("Press enter to quit");

            //Create events
            handler.ClientConnected += (sndr, arguments) =>
            {
                //Increase and set count on each connect to give an example on how to set values from server
                nCount++;
                handler.GetProxy().Count = nCount;
                Console.WriteLine("Client connected");
            };

            //Simple event for disconnected clients
            handler.ClientDisconnected += (sndr, arguments) =>
                Console.WriteLine("Client disconnected");

            try
            {
                //Start server
                handler.Start(Channel.Name);

                //Wait for user input before exiting application
                Console.ReadLine();

                //Stopping
                handler.Stop();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                Console.ReadLine();
            }


        }
    }
}
