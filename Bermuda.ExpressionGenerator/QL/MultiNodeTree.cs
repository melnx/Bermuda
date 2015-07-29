using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bermuda.ExpressionGeneration
{
    public abstract partial class MultiNodeTree : ExpressionTreeBase
    {
        List<ExpressionTreeBase> _children = new List<ExpressionTreeBase>();

        public ReadOnlyCollection<ExpressionTreeBase> Children
        {
            get
            {
                return _children.AsReadOnly();
            }
        }

        public void AddChild(ExpressionTreeBase child)
        {
            _children.Add(child);
            child.SetParent(this);
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            foreach (ExpressionTreeBase current in Children)
            {
                yield return current;
                foreach (ExpressionTreeBase subChildren in current.GetChildren())
                {
                    yield return subChildren;
                }
            }
        }
    }
}
