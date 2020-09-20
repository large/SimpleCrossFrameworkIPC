using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Client class is a mix between Full Duplex PipeServer and KrakenIPC
    /// See links:
    /// https://www.codeproject.com/Articles/1179195/Full-Duplex-Asynchronous-Read-Write-with-Named-Pip
    /// https://github.com/darksody/KrakenIPC
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Client<T> where T : class
    {
        //Client used only one pipe
        private ClientPipe clientPipe;
        
        //Proxy is the callback function with the pipe
        private T proxy;

        //Pipedata is async and needs a sync to wait for data in the GetInstance() proxy function
        private ManualResetEventSlim responseEvent = new ManualResetEventSlim(false);
        private string responseJson;

        //Function returns true if runtime is .core, false if .net
        static readonly bool isNetCore = Type.GetType("System.String, System.Private.CoreLib") != null;

        //Event if client was disconnected
        public event EventHandler<EventArgs> ClientDisconnected;

        //Event if client timed out for waiting for data
        public event EventHandler<EventArgs> ClientDataReceiveTimeout;

        //Timeout variable (in ms) for waiting for data from server. Default 2000 ms (2 secs)
        public int ClientReceiveTimeout { get; set; }

        //Variable for connection-station
        private bool bConnected;

        /// <summary>
        /// Proxy uses a callbackfunction that builds with the Response-class received from the server
        /// Default constructor for 2000 ms wait time for data
        /// </summary>
        public Client() : this(2000)
        {
        }

        /// <summary>
        /// Alternative creator for clients
        /// </summary>
        /// <param name="_ClientReceiveTimeout"></param>
        public Client(int _ClientReceiveTimeout)
        {
            clientPipe = null;
            proxy = ProxyHelper.GetInstance<T>(OnMethodCallback);
            bConnected = false;
            ClientReceiveTimeout = _ClientReceiveTimeout;
        }

        /// <summary>
        /// Connects with unlimited timeout value
        /// </summary>
        /// <param name="Pipename"></param>
        public void Connect(string Pipename)
        {
            Connect(Pipename, 0);
        }

        /// <summary>
        /// Connects with a spesified timeout value
        /// 0 = infinity
        /// </summary>
        /// <param name="Pipename"></param>
        /// <param name="Timeout"></param>
        public void Connect(string Pipename, int Timeout)
        {
            clientPipe = new ClientPipe(".", Pipename, p => p.StartMessageReaderAsync());

            clientPipe.DataReceived += (sndr, args) =>
            {
                ClientPipe sender = sndr as ClientPipe;
                OnMessageReceived(sender, args.Data);
            };

            //Disconnect event trigger, sets connected state to false
            clientPipe.Disconnect += (sndr, args) =>
            {
                bConnected = false;
                ClientDisconnected?.Invoke(this, new EventArgs());
            };

            clientPipe.Connect(Timeout);
            bConnected = true;
        }

        /// <summary>
        /// Disconnects from the pipe
        /// </summary>
        public void Disconnect()
        {
            clientPipe.Close();
            bConnected = false;
        }

        /// <summary>
        /// Returns the state of the connection
        /// </summary>
        /// <returns>true = connected, false = disconnected</returns>
        public bool IsConnected()
        {
            return bConnected;
        }

        /// <summary>
        /// Uses the proxy instance to call the server
        /// </summary>
        /// <param name="callback">Delegate to use the proxy class</param>
        public void UseProxy(Action<T> callback)
        {
            callback(proxy);
        }

        /// <summary>
        /// Get the proxy class so you can make calls to the server
        /// </summary>
        /// <returns>Proxy instance that respects the contract</returns>
        public T GetProxy()
        {
            return proxy;
        }

        /// <summary>
        /// A callbackfunction from the ProxyHelper class
        /// Requests in the proxy will ask the server for the info and fill the class with that info
        /// (No complex types through)
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameterValues"></param>
        /// <param name="parameterTypes"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private object OnMethodCallback(string methodName, List<object> parameterValues, List<object> parameterTypes, Type returnType)
        {
            //call server here, get response if needed
            Request request = new Request()
            {
                MethodName = methodName,
                ParameterTypes = parameterTypes.OfType<Type>().ToList(),
                ParameterValues = parameterValues,
                ReturnType = returnType
            };

            //Create a request based on data asked for
            var requestMessage = new Message<Request>(request);
            var json = JsonConvert.SerializeObject(requestMessage);

            //This a dirty "hack" to distinc .net and .core
            if (!isNetCore)
                json = json.Replace("System.Private.CoreLib", "mscorlib");

            //Send the requests to the server
            byte[] requestBytes = Encoding.ASCII.GetBytes(json);
            clientPipe.WriteBytes(requestBytes, false);

            //Wait for data to be received, then build the data (user set timeout)
            responseEvent.Wait(TimeSpan.FromMilliseconds(ClientReceiveTimeout));
            if (responseEvent.IsSet)
            {
                Message message = JsonConvert.DeserializeObject<Message>(responseJson);
                responseEvent.Reset();
                var result = message.GetPayload<Response>();
                if (result.ReturnValue == null)
                {
                    return null;
                }
                else
                {
                    if (result.ReturnType == typeof(Exception))
                    {
                        throw new Exception(result.ReturnValue.ToString());
                    }
                    if (result.ReturnValue.ToString().StartsWith("{") == false && result.ReturnValue.ToString().StartsWith("[") == false)
                    {
                        //not a json, it's a primitive
                        if (result.ReturnType.BaseType == typeof(Enum))
                        {
                            return Convert.ChangeType(result.ReturnValue, typeof(int));
                        }
                        return Convert.ChangeType(result.ReturnValue, result.ReturnType);
                    }
                    return JsonConvert.DeserializeObject(result.ReturnValue.ToString(), returnType);
                }

            }
            else //If event is not set, then return nada
            {
                ClientDataReceiveTimeout?.Invoke(this, new EventArgs());
                throw new Exception("Timeout waiting for data");
            }
        }

        /// <summary>
        /// Since the Pipes used is full duplex, the received messages are through other threads
        /// The OnMethodCallback needs this info, it waits for the responseEvent to be set
        /// </summary>
        /// <param name="clientPipe"></param>
        /// <param name="bytes"></param>
        private void OnMessageReceived(ClientPipe clientPipe, byte[] bytes)
        {
            responseJson = Encoding.ASCII.GetString(bytes);
            responseEvent.Set();
        }

    }
}
