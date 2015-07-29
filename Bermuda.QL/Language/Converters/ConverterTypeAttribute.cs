using System;

namespace Bermuda.QL.Converters
{
    public class ConverterTypeAttribute : Attribute
    {
        public Type OutputType { get; set; }

        public string SpecialName { get; set; }

        public ConverterTypeAttribute()
        {

        }
    }
}
