using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace SimpleCrossPIPE
{
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

        public Client()
        {
            clientPipe = null;
            proxy = ProxyHelper.GetInstance<T>(OnMethodCallback);
        }

        public void Connect(string Pipename)
        {
            Connect(Pipename, 0);
        }

        public void Connect(string Pipename, int Timeout)
        {
            clientPipe = new ClientPipe(".", Pipename, p => p.StartMessageReaderAsync());

            clientPipe.DataReceived += (sndr, args) =>
            {
                ClientPipe sender = sndr as ClientPipe;
                OnMessageReceived(sender, args.Data);
            };

            clientPipe.Disconnect += (sndr, args) =>
                ClientDisconnected?.Invoke(this, new EventArgs());

            clientPipe.Connect(Timeout);
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

            var requestMessage = new Message<Request>(request);
            var json = JsonConvert.SerializeObject(requestMessage);

            if (!isNetCore)
                json = json.Replace("System.Private.CoreLib", "mscorlib");

            byte[] requestBytes = Encoding.ASCII.GetBytes(json);
            clientPipe.WriteBytes(requestBytes, false);

            //Wait for data to be received, then build the rest
            responseEvent.Wait(TimeSpan.FromMilliseconds(2000));
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
            else //If event is not set, then it is a failure...
                return null;
        }

        private void OnMessageReceived(ClientPipe clientPipe, byte[] bytes)
        {
            //Store the message received and trigger event to continue OnMethodCallback()....
            responseJson = Encoding.ASCII.GetString(bytes);
            responseEvent.Set();
        }

    }
}
