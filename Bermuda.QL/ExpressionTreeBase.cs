using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class ExpressionTreeBase
    {
        public abstract Expression CreateExpression(object context);

        protected Expression GetExpression<ObjectType, PropertyType>(Expression<Func<ObjectType, PropertyType>> expression)
        {
            return expression.Body;
        }

        protected Expression GetExpression<ObjectType>(Expression<Func<ObjectType, bool>> expression)
        {
            return expression.Body;
        }
    }
}
