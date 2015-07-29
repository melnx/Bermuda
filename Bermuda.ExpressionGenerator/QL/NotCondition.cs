using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class NotCondition : ConditionalExpression
    {
        public override Expression CreateExpression(object context)
        {
            var expr = Child.CreateExpression(context);
            if (expr == null) return null;
            else return Expression.Not(expr);
        }

        protected override Expression LinkExpression(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        protected override string ChildToString(bool includeUnimportant)
        {
            return "NOT:";
        }
    }

    public partial class NegateCondition : ConditionalExpression
    {
        public override Expression CreateExpression(object context)
        {
            var expr = Child.CreateExpression(context);
            if (expr == null) return null;
            else return Expression.Negate(expr);
        }

        protected override Expression LinkExpression(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        protected override string ChildToString(bool includeUnimportant)
        {
            return "-";
        }
    }
}