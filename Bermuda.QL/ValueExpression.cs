using System.Linq.Expressions;

namespace Bermuda.QL
{
    public partial class ValueExpression
    {
        public override Expression CreateExpression(object context)
        {
            return Expression.Constant(Value);
        }
    }
}
