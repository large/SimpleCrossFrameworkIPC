using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossPIPE
{
    public partial class Server<T, U> where T : class, new()
    {
        private List<ServerPipe> serverPipes;
        private T proxysource;

        static readonly bool isNetCore = Type.GetType("System.String, System.Private.CoreLib") != null;

        public event EventHandler<EventArgs> ClientConnected;
        public event EventHandler<EventArgs> ClientDisconnected;

        private string _Pipename { get; set; }

        public Server()
        {
            serverPipes = new List<ServerPipe>();
            proxysource = new T();
        }

        public void Start(string Pipename)
        {
            _Pipename = Pipename;
            CreateServer(Pipename);
        }

        /// <summary>
        /// From PipeClient https://www.codeproject.com/Articles/1179195/Full-Duplex-Asynchronous-Read-Write-with-Named-Pip
        /// </summary>
        /// <returns></returns>
        private ServerPipe CreateServer(string Pipename)
        {
            int serverIdx = serverPipes.Count;
            ServerPipe serverPipe = new ServerPipe(Pipename, p => p.StartMessageReaderAsync());
            serverPipes.Add(serverPipe);

            serverPipe.DataReceived += (sndr, args) =>
            {
                //Console.WriteLine($"{args.String}");
                ServerPipe sender = sndr as ServerPipe;
                OnMessageReceived(sender, args.Data);
            };

            serverPipe.Connected += (sndr, args) =>
            {
                ClientConnected?.Invoke(this, new EventArgs());
                CreateServer(_Pipename);
            };

            serverPipe.Disconnect += (sndr, args) =>
            {
                ClientDisconnected?.Invoke(this, new EventArgs());
                ServerPipe sender = sndr as ServerPipe;
                serverPipes.Remove(sender);
            };

            return serverPipe;
        }

        public void Stop()
        {
            //First server is "async start mode" and shall not be closed as all the other clients
            for(int i=serverPipes.Count-1; i > 1; i--)
            {
                serverPipes[i].Flush();
                serverPipes[i].Close();
            }
        }

        private void OnMessageReceived(ServerPipe serverPipe, byte[] bytes)
        {
            var json = Encoding.ASCII.GetString(bytes);

            //If server is not .Core, convert to mscorlib
            if (!isNetCore)
                json = json.Replace("System.Private.CoreLib", "mscorlib");

            Message message = JsonConvert.DeserializeObject<Message>(json);

            if (message == null)
                return;

            var request = message.GetPayload<Request>();

            object result;
            try
            {
                MethodInfo invokeMethod = typeof(T).GetMethod(request.MethodName);
                for (int i = 0; i < request.ParameterValues.Count; i++)
                {
                    var jObject = request.ParameterValues[i] as JObject;
                    if (jObject != null)
                    {
                        request.ParameterValues[i] = jObject.ToObject(request.ParameterTypes[i]);
                    }
                    else if (request.ParameterTypes[i].BaseType == typeof(Enum))
                    {
                        request.ParameterValues[i] = Convert.ChangeType(request.ParameterValues[i], typeof(int));
                    }
                    else
                    {
                        request.ParameterValues[i] = Convert.ChangeType(request.ParameterValues[i], request.ParameterTypes[i]);
                    }
                }
                result = invokeMethod.Invoke(proxysource, request.ParameterValues.ToArray());
            }
            catch (Exception ex)
            {
                request.ReturnType = typeof(Exception);
                result = ex.InnerException?.Message;
            }

            var response = new Response()
            {
                MethodName = request.MethodName,
                ReturnType = request.ReturnType,
                ReturnValue = result
            };
            var responseMessage = new Message<Response>(response);
            var responseJson = JsonConvert.SerializeObject(responseMessage);
            byte[] responseBytes = Encoding.ASCII.GetBytes(responseJson);
            serverPipe.WriteBytes(responseBytes, false);
        }
    }
}
