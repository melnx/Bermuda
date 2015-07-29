using System;
using System.Collections.Generic;

namespace Bermuda.QL
{
    public partial class ValueExpression : ExpressionTreeBase
    {
        public int Value { get; private set; }

        public ValueExpression(int value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return String.Format("@{0}", Value.ToString());
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            yield break;
        }
    }
}
