using System;
using System.Collections.Generic;
using System.Linq;

namespace Bermuda.ExpressionGeneration
{
    public abstract partial class SingleNodeTree : ExpressionTreeBase
    {
        public ExpressionTreeBase Child { get; private set; }

        public void SetChild(ExpressionTreeBase child)
        {
            Child = child;
            if(Child != null) Child.SetParent(this);
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

    public abstract partial class DoubleNodeTree : SingleNodeTree
    {
        public ExpressionTreeBase Left { get; private set; }
        public ExpressionTreeBase Right { get { return Child; } private set { base.SetChild(value); } }

        public void SetLeft(ExpressionTreeBase left) 
        {
            if(left != null) left.SetParent(this); 
            Left = left; 
        }
        public void SetRight(ExpressionTreeBase right) { Right = right; }

        public void SetChildren(ExpressionTreeBase left, ExpressionTreeBase right)
        {
            if (left != null) Left.SetParent(this);
            Left = left;

            if (right != null) right.SetParent(this);
            Right = right;
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            if (Left != null)
            {
                yield return Left;
                foreach (ExpressionTreeBase current in Left.GetChildren())
                {
                    yield return current;
                }
            }

            foreach (ExpressionTreeBase current in base.GetChildren())
            {
                yield return current;
            }
        }
    }
}
