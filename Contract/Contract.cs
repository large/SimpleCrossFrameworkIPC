﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerClientContract
{
    public interface IMySimpleService
    {
        int Number { get;  }
        string Text { get; }

        int Function();
    }

    public class Channel
    {
        public static string Name = "ChannelName";
        public static int TimeoutMS = 1000;
    }
}
