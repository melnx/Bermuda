using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Bermuda.ExpressionGeneration
{
    public static class LinqRuntimeTypeBuilder
    {
        private static AssemblyName assemblyName = new AssemblyName() { Name = "DynamicLinqTypes" };
        private static ModuleBuilder moduleBuilder = null;
        private static ConcurrentDictionary<string, Type> builtTypes = new ConcurrentDictionary<string, Type>();
        private static readonly char delimiter = ';';

        static LinqRuntimeTypeBuilder()
        {
            moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        }

        private static string GetTypeKey(Dictionary<string, Type> fields)
        {
            //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter
            string key = string.Empty;
            foreach (var field in fields.OrderBy(x => x.Key))
                key += field.Key + delimiter + field.Value + delimiter;

            //return key;
            var result = key + ".T" + key.GetHashCode();

            return result;
        }

        public static Dictionary<string, Type> ParseTypeKey(string key)
        {
            var parts = key.Split(delimiter);

            Dictionary<string, Type> result = new Dictionary<string, Type>();

            for (int i = 0; i < parts.Length - 1; i+=2)
            {
                result[parts[i]] = Type.GetType(parts[i + 1]);
            }

            return result;
        }

        public static Type GetDynamicType(Dictionary<string, Type> fields)
        {
            if (null == fields)
                throw new ArgumentNullException("fields");
            if (0 == fields.Count)
                throw new ArgumentOutOfRangeException("fields", "fields must have at least 1 field definition");

            try
            {
                Monitor.Enter(builtTypes);
                string className = GetTypeKey(fields);

                if (builtTypes.ContainsKey(className))
                    return builtTypes[className];

                //TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);
                TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Serializable, typeof(ValueType));

                foreach (var field in fields)
                    typeBuilder.DefineField(field.Key, field.Value, FieldAttributes.Public);

                return builtTypes[className] = typeBuilder.CreateType();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                Monitor.Exit(builtTypes);
            }

            return null;
        }


        private static string GetTypeKey(IEnumerable<PropertyInfo> fields)
        {
            return GetTypeKey(fields.ToDictionary(f => f.Name, f => f.PropertyType));
        }

        public static Type GetDynamicType(IEnumerable<PropertyInfo> fields)
        {
            return GetDynamicType(fields.ToDictionary(f => f.Name, f => f.PropertyType));
        }

        public static Type GetDynamicType(IEnumerable<FieldInfo> fields)
        {
            return GetDynamicType(fields.ToDictionary(f => f.Name, f => f.FieldType));
        }

        public static object DeserializeJson(string json, string typeKey, bool isList)
        {
            Type type = GetTypeFromTypeKey(typeKey);
            if (isList) type = type.MakeArrayType();
            return JsonConvert.DeserializeObject(json, type);
            //return new JavaScriptSerializer().Deserialize(json, type);
        }

        public static Type GetTypeFromTypeKey(string typeKey)
        {
            if (typeKey == null) throw new ArgumentNullException("Type key cannot be null");

            Type type = null;

            if (!typeKey.Contains(delimiter))
            {
                type = Type.GetType(typeKey);
            }
            else
            {
                var fields = ParseTypeKey(typeKey);
                type = GetDynamicType(fields);
            }

            return type;
        }

        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj);
            //return new JavaScriptSerializer().Serialize(obj);
        }

        public static bool IsNotCoreType(Type type)
        {
            return (type != typeof(object) && Type.GetTypeCode(type) == TypeCode.Object);
        }

        public static string GetTypeKey(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (IsNotCoreType(type))
            {
                return GetTypeKey(type.GetFields().ToDictionary(f => f.Name, f => f.FieldType));
            }

            return type.FullName;
        }
    }
}
