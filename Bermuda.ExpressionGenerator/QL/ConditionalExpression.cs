using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Bermuda.ExpressionGeneration
{
    public abstract partial class ConditionalExpression : SingleNodeTree
    {
        List<ConditionalExpression> _additionalConditions;

        public ReadOnlyCollection<ConditionalExpression> AdditionalConditions { get; private set; }

        public ConditionalExpression()
        {
            _additionalConditions = new List<ConditionalExpression>();
            AdditionalConditions = new ReadOnlyCollection<ConditionalExpression>(_additionalConditions);
        }

        public void AddCondition(ConditionalExpression condition)
        {
            _additionalConditions.Add(condition);
            condition.SetParent(this);
        }

        public override string ToString()
        {
            return FullToString(false);
        }

        protected string FullToString(bool includeUnimportant)
        {
            string childString = ChildToString(includeUnimportant);
            return String.Format("{0}{1} {2}", childString == null ? null : childString + " ", Child.ToString(), MultiToString(AdditionalConditions, x => ((ConditionalExpression)x).FullToString(true)));
        }

        protected abstract string ChildToString(bool includeUnimportant);

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            if (Child != null)
            {
                yield return Child;
                foreach (ExpressionTreeBase children in Child.GetChildren())
                {
                    yield return children;
                }
            }
            foreach (ConditionalExpression current in AdditionalConditions)
            {
                yield return current;
                foreach (ExpressionTreeBase children in current.GetChildren())
                {
                    yield return children;
                }
            }
        }

        public override Expression CreateExpression(object context)
        {
            Expression childExpression = Child.CreateExpression(null);

            Expression lastExpression = null;
            ConditionalExpression lastConditional = null;
            foreach (ConditionalExpression current in AdditionalConditions)
            {
                Expression expression = current.CreateExpression(lastExpression);
                lastExpression = expression;
                lastConditional = current;
            }
            if (lastConditional == null)
            {
                return childExpression;
            }
            else
            {
                return lastConditional.LinkExpression(childExpression, lastExpression);
            }
        }

        protected abstract Expression LinkExpression(Expression left, Expression right);
    }
}
