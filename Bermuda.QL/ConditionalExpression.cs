using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class ConditionalExpression
    {
        public override Expression CreateExpression(object context)
        {
            Expression childExpression = Child.CreateExpression(null);

            Expression lastExpression = null;
            ConditionalExpression lastConditional = null;
            foreach (ConditionalExpression current in AdditionalConditions)
            {
                Expression expression = current.CreateExpression(lastExpression);
                lastExpression = expression;
                lastConditional = current;
            }
            if (lastConditional == null)
            {
                return childExpression;
            }
            else
            {
                return lastConditional.LinkExpression(childExpression, lastExpression);
            }
        }

        protected abstract Expression LinkExpression(Expression left, Expression right);
    }
}
