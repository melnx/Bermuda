using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public class HavingExpression : EnumerableBaseExpression
    {
        public override Expression CreateExpression(object context)
        {
            var res = Child.CreateExpression(context);

            return res;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" HAVING ");

            sb.Append(Child.ToString());

            return sb.ToString();
        } 
    }
}
