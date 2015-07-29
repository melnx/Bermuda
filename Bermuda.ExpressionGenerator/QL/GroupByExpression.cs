using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class GroupByExpression : GetExpression
    {
        public List<DimensionExpression> _dimensions = new List<DimensionExpression>();

        public void AddDimension(DimensionExpression dim)
        {
            _dimensions.Add(dim);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("GROUP BY ");
            sb.Append(string.Join(",", _dimensions));

            return sb.ToString();
        }
    }

    
}
