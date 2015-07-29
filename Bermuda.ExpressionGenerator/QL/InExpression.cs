using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    class InExpression : MultiNodeTree
    {

        public override Expression CreateExpression(object context)
        {
            return null;
        }

        public void AddItem(ExpressionTreeBase lit)
        {
            AddChild(lit);
        }
    }
}
