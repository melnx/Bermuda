using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bermuda.QL.Converters
{
    public abstract class ConverterBase
    {
        public abstract object Convert(string text);

        static Dictionary<Type, ConverterBase> _converters = new Dictionary<Type, ConverterBase>();
        static Dictionary<string, ConverterBase> _specialConverters = new Dictionary<string, ConverterBase>();

        public static ConverterBase GetConverter<ObjectType>()
        {
            return _converters.ContainsKey(typeof(ObjectType)) ? _converters[typeof(ObjectType)] : null;
        }

        public static ConverterBase GetConverter(string specialName)
        {
            return _specialConverters.ContainsKey(specialName) ? _specialConverters[specialName] : null;
        }
        static ConverterBase()
        {
            Type converterType = typeof(ConverterBase);
            Type[] types = Assembly.GetCallingAssembly().GetTypes();
            foreach (Type current in types)
            {
                try
                {
                    ConstructorInfo info;
                    if (converterType.IsAssignableFrom(current) && (info = current.GetConstructor(new Type[] { })) != null)
                    {
                        object[] attributes = current.GetCustomAttributes(true);
                        foreach (object next in attributes)
                        {
                            if (next is ConverterTypeAttribute)
                            {
                                ConverterTypeAttribute converterAttribute = (ConverterTypeAttribute)next;
                                ConverterBase actualConverter = (ConverterBase)info.Invoke(null);
                                if (converterAttribute.SpecialName != null)
                                {
                                    if (!_specialConverters.ContainsKey(converterAttribute.SpecialName))
                                    {
                                        _specialConverters.Add(converterAttribute.SpecialName, actualConverter);
                                    }
                                }
                                else
                                {
                                    if (!_converters.ContainsKey(converterAttribute.OutputType))
                                    {
                                        _converters.Add(converterAttribute.OutputType, actualConverter);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
