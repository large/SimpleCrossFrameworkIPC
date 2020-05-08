﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Message is the class that serialize and deserialize based on json data.
    /// It has a quick hack for the .core and .net simple types
    /// /// </summary>
    public class Message
    {
        public string Payload { get; set; }
        public Type Type { get; protected set; }

        static readonly bool isNetCore = Type.GetType("System.String, System.Private.CoreLib") != null;

        public T GetPayload<T>()
        {
            //A rough hack for handling .core and .net difference baseclasses
            if (!isNetCore)
                Payload = Payload.Replace("System.Private.CoreLib", "mscorlib");

            return JsonConvert.DeserializeObject<T>(Payload);
        }
    }

    public class Message<T> : Message
    {
        public Message(T obj)
        {
            this.Type = typeof(T);
            this.Payload = JsonConvert.SerializeObject(obj);
        }
    }
}
