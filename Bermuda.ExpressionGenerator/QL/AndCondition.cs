using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.IO;

namespace Bermuda.ExpressionGeneration
{
    public partial class AndCondition : ConditionalExpression
    {
        protected override string ChildToString(bool includeUnimportant)
        {
            return includeUnimportant ? "AND" : null;
        }

        override protected Expression LinkExpression(Expression left, Expression right)
        {
            if (left == null && right == null) return null;
            else if (left == null && right != null) return right;
            else if (right == null && left != null) return left;
            else return Expression.AndAlso(left, right);
        }
    }
}
