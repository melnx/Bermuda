using System.Linq.Expressions;
using System.Collections.Generic;

namespace Bermuda.ExpressionGeneration
{
    public partial class ValueExpression : ExpressionTreeBase
    {
        public long Value { get; private set; }

        public ValueExpression(long value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("@{0}", Value.ToString());
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            yield break;
        }

        public override Expression CreateExpression(object context)
        {
            return Expression.Constant(Value);
        }
    }
}
