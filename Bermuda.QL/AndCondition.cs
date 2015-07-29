using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.IO;

namespace Bermuda.QL
{
    public partial class AndCondition
    {
        override protected Expression LinkExpression(Expression left, Expression right)
        {
            return Expression.AndAlso(left, right);
        }
    }
}
