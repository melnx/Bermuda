using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class ConditionGroup
    {
        public override Expression CreateExpression(object context)
        {
            foreach (ExpressionTreeBase current in Children)
            {
                return current.CreateExpression(null);
            }
            return null;
        }
    }
}
