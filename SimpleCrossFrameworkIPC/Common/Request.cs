using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Request class is sent to the server and contains everything it needs to reconstruct info as a object
    /// Changed the classname from https://github.com/darksody/KrakenIPC
    /// </summary>
    internal class Request
    {
        public string MethodName { get; set; }
        public List<Type> ParameterTypes { get; set; }
        public List<object> ParameterValues { get; set; }
        public Type ReturnType { get; set; }
    }
}
