using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public class IPEndPointConverter : JsonConverter
    {
        private string _ipPropName = "ip";
        private string _portPropName = "port";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = (JObject)JToken.ReadFrom(reader);

            IPAddress ip = IPAddress.Parse(obj.Value<string>(_ipPropName));
            int port = int.Parse(obj.Value<string>(_portPropName));

            return new IPEndPoint(ip, port);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IPEndPoint address)
            {
                JObject obj = new JObject(new object[]
                {
                    new JProperty(_ipPropName, address.Address.ToString()),
                    new JProperty(_portPropName, address.Port),
                });
                obj.WriteTo(writer);
            }
            else
                throw new NotSupportedException();
        }
    }
}
