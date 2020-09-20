using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerClientContract
{
    //Interface shared between server and client(s)
    public interface IMySimpleService
    {
        int Number { get;  }
        string Text { get; }
        int Count { get; set; }

        int Function();
        bool DelayedFunction();
    }

    //Simple common class to share name and timeouts
    public class Channel
    {
        public static string Name = "ChannelName";
        public static int TimeoutMS = 1000; //Connection timeout
    }
}
