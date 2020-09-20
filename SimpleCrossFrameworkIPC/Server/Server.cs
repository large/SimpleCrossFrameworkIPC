using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Server class is based on the KrakenIPC class https://github.com/darksody/KrakenIPC
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    public partial class Server<T, U> where T : class, new()
    {
        private List<ServerPipe> serverPipes;
        private T proxysource;
        private string _Pipename { get; set; }

        //Function to detect .Core
        static readonly bool isNetCore = Type.GetType("System.String, System.Private.CoreLib") != null;

        //Public events
        public event EventHandler<EventArgs> ClientConnected;
        public event EventHandler<EventArgs> ClientDisconnected;

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
        private void CreateServer(string Pipename)
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
                ServerPipe sender = sndr as ServerPipe;
                bool bPipeRemoved = serverPipes.Remove(sender);
                if (!bPipeRemoved)
                    throw new Exception("Pipe not found, rare case ");
                else
                    ClientDisconnected?.Invoke(this, new EventArgs());
            };
        }

        /// <summary>
        /// Stop exlcude the first element since that is the "base server" and needs to be close otherwise
        /// </summary>
        public void Stop()
        {
            //First server is "async start mode" and shall not be closed as all the other clients
            for(int i=serverPipes.Count-1; i > 1; i--)
            {
                serverPipes[i].Flush();
                serverPipes[i].Close();
            }
        }

        /// <summary>
        /// Get the proxy class so you can make calls to the server
        /// </summary>
        /// <returns>Proxy instance that respects the contract</returns>
        public T GetProxy()
        {
            return proxysource;
        }

        /// <summary>
        /// Messages received by the client is a request for data.
        /// It converts the request and compare it to the Interface and return the result
        /// </summary>
        /// <param name="serverPipe"></param>
        /// <param name="bytes"></param>
        private void OnMessageReceived(ServerPipe serverPipe, byte[] bytes)
        {
            var json = Encoding.ASCII.GetString(bytes);

            //If server is not .Core, convert to mscorlib, a rough hack
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

            try
            {
                serverPipe.WriteBytes(responseBytes, false);
            }
            catch(IOException) //Catch this exception to ensure disconnect
            {
            }
        }
    }
}
