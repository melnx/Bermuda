using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.QL.Converters;

namespace Bermuda.QL
{
    public partial class LiteralExpression : ExpressionTreeBase
    {
        public ConvertedType Convert<ConvertedType>()
        {
            return (ConvertedType)ConverterBase.GetConverter<ConvertedType>().Convert(Value ?? "");
        }

        public ConvertedType Convert<ConvertedType>(string specialName)
        {
            return (ConvertedType)ConverterBase.GetConverter(specialName).Convert(Value ?? "");
        }

        public bool IsQuoted { get; private set; }

        public string Value { get; private set;  }

        private static char[] _specialCharacters = new char[] { '!', '@', '$', '%', '^', '&', '*', '(', ')', '_', '+', '-', '/', '{', '}', '.', '\\', ' ', '\'' };

        public LiteralExpression(string value)
        {
            Value = value;
        }

        public LiteralExpression(string value, bool isQuoted)
        {
            Value = value;
            IsQuoted = true;
        }

        public override string ToString()
        {
            int r;
            if (IsQuoted || (_specialCharacters.Any(x => Value.Contains(x)) && !int.TryParse(Value, out r)))
            {
                if (Value.Contains('"')) return Value;
                return String.Format("\"{0}\"", Value);
            }
            return Value;
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            yield break;
        }
    }
}
