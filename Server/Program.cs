using ServerClientContract;
using SimpleCrossPIPE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class MySimpleService : IMySimpleService
    {
        public int Number { get => 111; set => Number = value; }
        public string Text { get => "Some string"; set => Text = value; }

        public int Function()
        {
            return Number * 12;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var handler = new Server<MySimpleService, IMySimpleService>();

            Console.WriteLine("Server starting channel: " + Channel.Name);
            Console.WriteLine("Press enter to quit");
            handler.Start(Channel.Name);

            //Create events
            handler.ClientConnected += (sndr, arguments) =>
                Console.WriteLine("Client connected");
            handler.ClientDisconnected += (sndr, arguments) =>
                Console.WriteLine("Client disconnected");

            Console.ReadLine();

            //Stopping
            handler.Stop();
        }
    }
}
