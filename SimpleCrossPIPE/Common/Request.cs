using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossPIPE
{
    internal class Request
    {
        public string MethodName { get; set; }
        public List<Type> ParameterTypes { get; set; }
        public List<object> ParameterValues { get; set; }
        public Type ReturnType { get; set; }
    }
}
