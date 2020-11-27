using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CNLab4_Server
{
    public static class StreamEx
    {
        //private static async Task<IPEndPoint> ReadIPPointAsync(Stream stream)
        //{
        //    byte[] data = await ReadBytesAsync(stream, 4);
        //    int port = await ReadInt32Async(stream);
        //    return new IPEndPoint(new IPAddress(data), port);
        //}

        //private static IPEndPoint ReadIPPoint(Stream stream)
        //{
        //    byte[] data = ReadBytes(stream, 4);
        //    int port = ReadInt32(stream);
        //    return new IPEndPoint(new IPAddress(data), port);
        //}

        //private static async Task WriteAsync(Stream stream, IPEndPoint point)
        //{
        //    await WriteAsync(stream, point.Address.GetAddressBytes());
        //    await WriteAsync(stream, point.Port);
        //}

        //private static void Write(Stream stream, IPEndPoint point)
        //{
        //    Write(stream, point.Address.GetAddressBytes());
        //    Write(stream, point.Port);
        //}

        private static Encoding _defaultEncoding = Encoding.UTF8;

        public static async Task<JObject> ReadJObjectAsync(this Stream stream, Encoding encoding = null)
        {
            if (encoding is null)
                encoding = _defaultEncoding;

            byte[] jsonBinary = await stream.ReadBytesWithPrefixAsync();
            string jsonString = encoding.GetString(jsonBinary);
            return JObject.Parse(jsonString);
        }

        //private static JObject ReadJObject(Stream stream)
        //{
        //    byte[] jsonBinary = ReadBytesWithPrefix(stream);
        //    string jsonString = _defaultEncoding.GetString(jsonBinary);
        //    return JObject.Parse(jsonString);
        //}

        public static async Task WriteAsync(this Stream stream, JToken token, Encoding encoding = null)
        {
            if (encoding is null)
                encoding = _defaultEncoding;

            string jsonString = token.ToString(Newtonsoft.Json.Formatting.None);
            byte[] jsonBinary = encoding.GetBytes(jsonString);
            await stream.WriteWithPrefixAsync(jsonBinary);
        }

        //private static void Write(Stream stream, JToken token)
        //{
        //    string jsonString = token.ToString(Newtonsoft.Json.Formatting.None);
        //    byte[] jsonBinary = _defaultEncoding.GetBytes(jsonString);
        //    WriteWithPrefix(stream, jsonBinary);
        //}

        public static async Task<byte[]> ReadBytesWithPrefixAsync(this Stream stream)
        {
            int length = await stream.ReadInt32Async();
            return await stream.ReadBytesAsync(length);
        }

        //public static byte[] ReadBytesWithPrefix(this Stream stream)
        //{
        //    int length = ReadInt32(stream);
        //    return ReadBytes(stream, length);
        //}

        public static async Task WriteWithPrefixAsync(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data.Length);
            await stream.WriteAsync(data);
        }

        //private static void WriteWithPrefix(Stream stream, byte[] data)
        //{
        //    Write(stream, data.Length);
        //    Write(stream, data);
        //}

        public static async Task<int> ReadInt32Async(this Stream stream)
        {
            byte[] data = await stream.ReadBytesAsync(4);
            return BitConverter.ToInt32(data, 0);
        }

        //private static int ReadInt32(Stream stream)
        //{
        //    byte[] data = ReadBytes(stream, 4);
        //    return BitConverter.ToInt32(data, 0);
        //}

        public static async Task WriteAsync(this Stream stream, int value)
        {
            await stream.WriteAsync(BitConverter.GetBytes(value));
        }

        //private static void Write(Stream stream, int value)
        //{
        //    Write(stream, BitConverter.GetBytes(value));
        //}

        public static async Task<bool> ReadBooleanAsync(this Stream stream)
        {
            byte[] data = await stream.ReadBytesAsync(1);
            return BitConverter.ToBoolean(data, 0);
        }

        //private static bool ReadBoolean(Stream stream)
        //{
        //    byte[] data = ReadBytes(stream, 1);
        //    return BitConverter.ToBoolean(data, 0);
        //}

        public static async Task WriteAsync(this Stream stream, bool value)
        {
            byte[] data = BitConverter.GetBytes(value);
            await stream.WriteAsync(data);
        }

        //private static void Write(Stream stream, bool value)
        //{
        //    byte[] data = BitConverter.GetBytes(value);
        //    Write(stream, data);
        //}

        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            int hasRead = 0;
            do
            {
                hasRead += await stream.ReadAsync(data, hasRead, length - hasRead);
            } while (hasRead < length);
            return data;
        }

        //private static byte[] ReadBytes(Stream stream, int length)
        //{
        //    byte[] data = new byte[length];
        //    int hasRead = 0;
        //    do
        //    {
        //        hasRead += stream.Read(data, hasRead, length - hasRead);
        //    } while (hasRead < length);
        //    return data;
        //}

        public static async Task WriteAsync(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        //public static void Write(this Stream stream, byte[] data)
        //{
        //    stream.Write(data, 0, data.Length);
        //}
    }
}
