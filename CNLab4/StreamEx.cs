using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CNLab4.Messages.Server;
using CNLab4.Messages;

namespace CNLab4
{

    public static class StreamEx
    {
        private static Encoding _defaultEncoding = Encoding.UTF8;

        // BaseMessage
        public static async Task<T> ReadMessageAsync<T>(this Stream stream) where T : BaseMessage
        {
            string jsonString = await stream.ReadStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonString, BaseMessage.HierarchicalConverter);
        }

        public static Task<T> ReadMessageTask<T>(this Stream stream) where T : BaseMessage
        {
            return Task.Run(() => stream.ReadMessage<T>());
        }

        public static T ReadMessage<T>(this Stream stream) where T : BaseMessage
        {
            string jsonString = stream.ReadString();
            return JsonConvert.DeserializeObject<T>(jsonString, BaseMessage.HierarchicalConverter);
        }

        public static async Task WriteAsync(this Stream stream, BaseMessage message)
        {
            string jsonString = JsonConvert.SerializeObject(message, Formatting.None,
                BaseMessage.HierarchicalConverter);
            await stream.WriteAsync(jsonString);
        }

        public static Task WriteTask(this Stream stream, BaseMessage message)
        {
            return Task.Run(() => stream.Write(message));
        }

        public static void Write(this Stream stream, BaseMessage message)
        {
            string jsonString = JsonConvert.SerializeObject(message, Formatting.None,
                BaseMessage.HierarchicalConverter);
            stream.Write(jsonString);
        }

        // JObject/JToken
        public static async Task<JObject> ReadJObjectAsync(this Stream stream, Encoding encoding = null)
        {
            string jsonString = await stream.ReadStringAsync(encoding);
            return JObject.Parse(jsonString);
        }

        public static JObject ReadJObject(this Stream stream, Encoding encoding = null)
        {
            string jsonString = stream.ReadString(encoding);
            return JObject.Parse(jsonString);
        }

        public static async Task WriteAsync(this Stream stream, JToken token, Encoding encoding = null)
        {
            string jsonString = token.ToString(Formatting.None);
            await stream.WriteAsync(jsonString, encoding);
        }

        public static void Write(this Stream stream, JToken token, Encoding encoding = null)
        {
            string jsonString = token.ToString(Formatting.None);
            stream.Write(jsonString, encoding);
        }

        // string
        public static async Task<string> ReadStringAsync(this Stream stream, Encoding encoding = null)
        {
            byte[] data = await stream.ReadBytesWithPrefixAsync();
            if (encoding is null)
                encoding = _defaultEncoding;

            return encoding.GetString(data);
        }

        public static string ReadString(this Stream stream, Encoding encoding = null)
        {
            byte[] data = stream.ReadBytesWithPrefix();
            if (encoding is null)
                encoding = _defaultEncoding;

            return encoding.GetString(data);
        }

        public static async Task WriteAsync(this Stream stream, string data, Encoding encoding = null)
        {
            if (encoding is null)
                encoding = _defaultEncoding;
            byte[] binary = encoding.GetBytes(data);
            await stream.WriteWithPrefixAsync(binary);
        }

        public static void Write(this Stream stream, string data, Encoding encoding = null)
        {
            if (encoding is null)
                encoding = _defaultEncoding;
            byte[] binary = encoding.GetBytes(data);
            stream.WriteWithPrefix(binary);
        }

        // byte[] with prefix
        public static async Task<byte[]> ReadBytesWithPrefixAsync(this Stream stream)
        {
            int length = await stream.ReadInt32Async();
            return await stream.ReadBytesAsync(length);
        }

        public static byte[] ReadBytesWithPrefix(this Stream stream)
        {
            int length = stream.ReadInt32();
            return stream.ReadBytes(length);
        }

        public static async Task WriteWithPrefixAsync(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data.Length);
            await stream.WriteAsync(data);
        }

        public static void WriteWithPrefix(this Stream stream, byte[] data)
        {
            stream.Write(data.Length);
            stream.Write(data);
        }

        // int32
        public static async Task<int> ReadInt32Async(this Stream stream)
        {
            byte[] data = await stream.ReadBytesAsync(4);
            return BitConverter.ToInt32(data, 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            byte[] data = stream.ReadBytes(4);
            return BitConverter.ToInt32(data, 0);
        }

        public static async Task WriteAsync(this Stream stream, int value)
        {
            await stream.WriteAsync(BitConverter.GetBytes(value));
        }

        public static void Write(this Stream stream, int value)
        {
            stream.Write(BitConverter.GetBytes(value));
        }

        // bool
        public static async Task<bool> ReadBooleanAsync(this Stream stream)
        {
            byte[] data = await stream.ReadBytesAsync(1);
            return BitConverter.ToBoolean(data, 0);
        }

        public static bool ReadBoolean(this Stream stream)
        {
            byte[] data = stream.ReadBytes(1);
            return BitConverter.ToBoolean(data, 0);
        }

        public static async Task WriteAsync(this Stream stream, bool value)
        {
            byte[] data = BitConverter.GetBytes(value);
            await stream.WriteAsync(data);
        }

        public static void Write(this Stream stream, bool value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data);
        }

        // byte[]
        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            int hasRead = 0;
            do
            {
                int tm = await stream.ReadAsync(data, hasRead, length - hasRead);
                if (tm == 0)
                    throw new EndOfStreamException();
                hasRead += tm;
            } while (hasRead < length);
            return data;
        }

        public static Task<byte[]> ReadBytesTask(this Stream stream, int length)
        {
            return Task.Run(() => stream.ReadBytes(length));
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            int hasRead = 0;
            do
            {
                int tm = stream.Read(data, hasRead, length - hasRead);
                if (tm == 0)
                    throw new EndOfStreamException();
                hasRead += tm;
            } while (hasRead < length);
            return data;
        }

        public static async Task WriteAsync(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        public static Task WriteTask(this Stream stream, byte[] data)
        {
            return Task.Run(() => stream.Write(data));
        }

        public static void Write(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }
    }
}
