using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class RangeExpression : LiteralExpression
    {
        public string LowerValue { get; set; }

        public string UpperValue { get; set; }

        public RangeExpression(string value)
            : base(value)
        {
            string[] parts = value.Split(new string[] { ".." }, StringSplitOptions.None);
            LowerValue = parts[0];
            UpperValue = parts[1];
        }

        public override string ToString()
        {
            return String.Format("{0}..{1}", LowerValue, UpperValue);
        }

        public override Expression CreateExpression(object context)
        {
            var result = Expression.NewArrayInit
            (
                typeof(string),
                new Expression[]
                {
                    Expression.Constant( LowerValue.Trim('"') ),
                    Expression.Constant( UpperValue.Trim('"') )
                }
            );

            return result;

            //return null;
        }
    }
}
