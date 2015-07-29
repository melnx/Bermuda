using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class NotCondition
    {
        public override Expression CreateExpression(object context)
        {
            return Expression.Not(Child.CreateExpression(context));
        }

        protected override Expression LinkExpression(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }
    }
}