using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class LiteralExpression
    {
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
