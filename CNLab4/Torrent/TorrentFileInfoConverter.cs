using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public class TorrentFileInfoConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TorrentFileInfo);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = (JObject)JToken.ReadFrom(reader);

            string path = obj.Value<string>("path");
            long fileLength = obj.Value<long>("file_length");
            int blockSize = obj.Value<int>("block_size");

            return new TorrentFileInfo(path, fileLength, blockSize);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is TorrentFileInfo fInfo)
            {
                JObject obj = new JObject(new object[]
                {
                    new JProperty("path", fInfo.FilePath),
                    new JProperty("file_length", fInfo.FileLength),
                    new JProperty("block_size", fInfo.BlockSize)
                });
                obj.WriteTo(writer);
            }
            else
                throw new NotSupportedException();
        }

    }

}
