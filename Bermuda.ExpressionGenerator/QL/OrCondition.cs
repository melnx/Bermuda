using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class OrCondition : ConditionalExpression
    {
        override protected Expression LinkExpression(Expression left, Expression right)
        {
            if (left == null && right == null) return null;
            else if (left == null && right != null) return right;
            else if (right == null && left != null) return left;
            else return Expression.OrElse(left, right);
        }

        protected override string ChildToString(bool includeUnimportant)
        {
            return "OR";
        }
    }
}
