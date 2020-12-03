using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    public class BitArrayConverter : JsonConverter
    {
        private static string _hexPropName = "hex";
        private static string _lengthPropName = "len";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BitArray) || objectType == typeof(IEnumerable<BitArray>);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (typeof(IEnumerable<BitArray>).IsAssignableFrom(objectType))
            {
                JArray jArray = (JArray)JToken.ReadFrom(reader);
                return ReadFrom(jArray);
            }
            else if (objectType == typeof(BitArray))
            {
                JObject jObject = (JObject)JToken.ReadFrom(reader);
                return ReadFrom(jObject);
            }
            else
                throw new NotSupportedException();
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IEnumerable<BitArray> enumerable)
            {
                JArray jArray = new JArray();
                foreach (BitArray bitArray in enumerable)
                    jArray.Add(ToJObject(bitArray));
                jArray.WriteTo(writer);
            }
            else if (value is BitArray bitArray)
            {
                ToJObject(bitArray).WriteTo(writer);
            }
            else
                throw new NotSupportedException();
        }

        private object ReadFrom(JArray jArray)
        {
            List<BitArray> list = new List<BitArray>();
            foreach (JObject jObject in jArray)
                list.Add(ReadFrom(jObject));
            return list;
        }

        private BitArray ReadFrom(JObject jObject)
        {
            byte[] bytes = ConvertEx.ToBytesArray(jObject.Value<string>(_hexPropName));
            BitArray bitArray = new BitArray(bytes);
            bitArray.Length = jObject.Value<int>(_lengthPropName);
            return bitArray;
        }

        private JObject ToJObject(BitArray bitArray)
        {
            int bytesCount = bitArray.Length / 8;
            if (bitArray.Length % 8 != 0)
                bytesCount++;
            byte[] bytes = new byte[bytesCount];
            bitArray.CopyTo(bytes, 0);

            return new JObject(new object[]
            {
                    new JProperty(_hexPropName, ConvertEx.ToString(bytes)),
                    new JProperty(_lengthPropName, bitArray.Length)
            });
        }
    }
}
