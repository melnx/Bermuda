using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq.Expressions;

namespace Bermuda.QL
{
    public abstract partial class ExpressionTreeBase
    {
        public RootExpression Root { get; protected set; }

        public ExpressionTreeBase Parent { get; private set; }

        public void SetParent(ExpressionTreeBase parent)
        {
            Parent = parent;
            Root = parent.Root;
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

        public abstract IEnumerable<ExpressionTreeBase> GetChildren();

        IEnumerable<ExpressionTreeBase> Tree
        {
            get
            {
                if (Parent == null)
                {
                    yield break;
                }
                foreach (ExpressionTreeBase next in Parent.Tree)
                {
                    yield return next;
                }
            }
        }

        protected string MultiToString(IEnumerable items)
        {
            return MultiToString(items, x => x.ToString());
        }

        protected string MultiToString(IEnumerable items, Func<object, string> getter)
        {
            return String.Join(" ", items.OfType<object>().Select(x => getter(x)).ToArray());
        }
    }
}
