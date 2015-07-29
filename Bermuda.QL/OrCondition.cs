using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class OrCondition
    {
        override protected Expression LinkExpression(Expression left, Expression right)
        {
            return Expression.OrElse(left, right);
        }
    }
}
