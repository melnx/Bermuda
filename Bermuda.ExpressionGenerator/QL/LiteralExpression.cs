using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.ExpressionGeneration.Converters;

namespace Bermuda.ExpressionGeneration
{
    public partial class IdentifierExpression : ExpressionTreeBase
    {
        public bool IsQuoted;
        public List<string> Parts { get; private set; }

        public IdentifierExpression()
        {
            Parts = new List<string>();
        }

        public override string ToString()
        {
            return string.Join(".", Parts);
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            yield break;
        }

        public override Expression CreateExpression(object context)
        {
            return null;
        }

        public string LastPart { get { return Parts.LastOrDefault(); } }
    }

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

        public string Value { get; private set; }

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

        public Expression ConvertExpression<ConvertedType>()
        {
            return Expression.Constant(Convert<ConvertedType>());
        }

        public Expression ConvertExpression<ConvertedType>(string specialName)
        {
            return Expression.Constant(Convert<ConvertedType>(specialName));
        }

        public override Expression CreateExpression(object context)
        {
            return Expression.Constant(Value);
        }
    }
}
