using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public class TorrentInfoConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TorrentInfo);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = (JObject)JToken.ReadFrom(reader);
            string torrentName = obj.Value<string>("torrent_name");
            JArray jFilesInfo = obj.Value<JArray>("files_info");
            string accessCode = obj.Value<string>("access_code");
            TorrentFileInfo[] filesInfo = jFilesInfo.ToObject<TorrentFileInfo[]>(serializer);
            return new TorrentInfo(torrentName, filesInfo, accessCode);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is TorrentInfo torrentInfo)
            {
                JObject obj = new JObject(new object[]
                {
                    new JProperty("torrent_name", torrentInfo.Name),
                    new JProperty("files_info", JArray.FromObject(torrentInfo.FilesInfo, serializer)),
                    new JProperty("access_code", torrentInfo.AccessCode)
                });
                obj.WriteTo(writer);
            }
            else
                throw new NotSupportedException();
        }
    }

}
