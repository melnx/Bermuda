using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public class ArgumentListExpression : ExpressionTreeBase
    {
        List<ExpressionTreeBase> Dimensions;

        public ArgumentListExpression()
        {
            Dimensions = new List<ExpressionTreeBase>();
        }

        public void AddArgument(ExpressionTreeBase dim)
        {
            if (dim != null) dim.SetParent(this);
            Dimensions.Add(dim);
        }

        public void RemoveArgument(DimensionExpression dim)
        {
            if(Dimensions.Contains(dim)) Dimensions.Remove(dim);
        }

        public override System.Linq.Expressions.Expression CreateExpression(object context)
        {
            return null;
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            foreach (var child in Dimensions)
            {
                yield return child;
            }
        }

        public ExpressionTreeBase FirstItem { get { return Dimensions.FirstOrDefault(); } }
    }
}
