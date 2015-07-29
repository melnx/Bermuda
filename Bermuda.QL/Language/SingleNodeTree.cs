using System;
using System.Collections.Generic;
using System.Linq;

namespace Bermuda.QL
{
    public abstract partial class SingleNodeTree : ExpressionTreeBase
    {
        public ExpressionTreeBase Child { get; private set; }

        public void SetChild(ExpressionTreeBase child)
        {
            Child = child;
            Child.SetParent(this);
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            if (Child != null)
            {
                yield return Child;
                foreach (ExpressionTreeBase current in Child.GetChildren())
                {
                    yield return current;
                }
            }
        }
    }
}
