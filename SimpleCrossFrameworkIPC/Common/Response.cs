using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Response is what the server sends to the client.
    /// Client can reconstruct data and result with this info
    /// Class is renamed but kept orginals from https://github.com/darksody/KrakenIPC
    /// </summary>
    class Response
    {
        public string MethodName { get; set; }
        public Type ReturnType { get; set; }
        public object ReturnValue { get; set; }
    }
}
