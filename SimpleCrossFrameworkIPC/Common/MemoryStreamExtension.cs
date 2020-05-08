﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrossFrameworkIPC
{
    /// <summary>
    /// Byte array as "StringBuilder" works, ref https://stackoverflow.com/questions/4015602/equivalent-of-stringbuilder-for-byte-arrays
    /// I love stackoverflow, the best solutions are usually not your own ;)
    /// </summary>
    public static class MemoryStreamExtensions
    {
        public static void Append(this MemoryStream stream, byte value)
        {
            stream.Append(new[] { value });
        }

        public static void Append(this MemoryStream stream, byte[] values)
        {
            stream.Write(values, 0, values.Length);
        }

        public static void Append(this MemoryStream stream, byte[] values, int length)
        {
            stream.Write(values, 0, length);
        }
    }
}
