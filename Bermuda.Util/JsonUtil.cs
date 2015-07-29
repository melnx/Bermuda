using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace EvoApp.Util
{
    /// <summary>
    /// A class to aid in the serialization and deserialization of objects to and from JSON or XML
    /// </summary>
    public static class JsonUtil
    {
        /// <summary>
        /// Serializes the given object to a Json string
        /// </summary>
        /// <typeparam name="T">The type of the object to be serialized</typeparam>
        /// <param name="toSerialize">The object to serialize</param>
        /// <returns></returns>
        public static string SerializeToJson<T>(T toSerialize)
        {          
            string serialized;
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(toSerialize.GetType());
                serializer.WriteObject(stream, toSerialize);
                serialized = Encoding.Default.GetString(stream.ToArray());
            }

            return serialized;
        }

        /// <summary>
        /// Deserializes a JSON string to the give type of object
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to</typeparam>
        /// <param name="toDeserialize">The JSON string to parse</param>
        /// <returns></returns>
        public static T DeserializeFromJson<T>(string toDeserialize)
        {
            T deserialized;
            //using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(toDeserialize)))
            //{
            //    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            //    deserialized = (T)serializer.ReadObject(ms);
            //    ms.Close();
            //}
            byte[] bytes = Encoding.UTF8.GetBytes(toDeserialize);
            using (XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                deserialized = (T)serializer.ReadObject(jsonReader);
                jsonReader.Close();
            }
            return deserialized;
        }

    }
}
