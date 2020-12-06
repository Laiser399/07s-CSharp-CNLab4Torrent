using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4
{
    class TargetTypeException : Exception { }

    class HierarchicalConverter : JsonConverter
    {
        private string _classPropName = "@ClassName";
        private string _objectPropName = "@Object";
        private Type[] _supportedTypes;

        public HierarchicalConverter(Type baseType)
        {
            Assembly assembly = Assembly.GetAssembly(baseType);
            _supportedTypes = assembly.GetTypes()
                .Where(t => baseType.IsAssignableFrom(t))
                .ToArray();
        }

        public override bool CanConvert(Type objectType)
        {
            return _supportedTypes.Contains(objectType);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = (JObject)JToken.ReadFrom(reader);
            string className = obj.Value<string>(_classPropName);
            JObject classObj = obj.Value<JObject>(_objectPropName);
            foreach (Type type in _supportedTypes)
            {
                if (type.FullName == className)
                {
                    if (objectType.IsAssignableFrom(type))
                        return classObj.ToObject(type);
                    else
                        throw new TargetTypeException();
                }
            }

            throw new TargetTypeException();
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject obj = new JObject(new object[]
            {
                new JProperty(_classPropName, value.GetType().FullName),
                new JProperty(_objectPropName, JObject.FromObject(value))
            });
            obj.WriteTo(writer);
        }

    }

}
