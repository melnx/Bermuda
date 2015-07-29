using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        private static void LinkDimensions(DimensionExpression[] groupBy, DimensionExpression[] selects)
        {
            if (groupBy == null || selects == null) return;

            foreach (var select in selects.Where(x => !x.IsStar && !string.Equals(x.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase)))
            {
                string targetPath = GetTargetPath(select);

                var matchingGroupByFieldReverse = groupBy.FirstOrDefault(x => string.Equals(targetPath, GetSourcePath(x), StringComparison.InvariantCultureIgnoreCase));

                if (matchingGroupByFieldReverse != null)
                {
                    matchingGroupByFieldReverse.Target = select.Target;
                    matchingGroupByFieldReverse.Source = select.Source;
                    select.IsBasedOnGrouping = true;
                    matchingGroupByFieldReverse.IsAutoSelect = true;
                    matchingGroupByFieldReverse.LinkedSelect = select;
                    select.LinkedGroupBy = matchingGroupByFieldReverse;
                    select.IsLinkReversed = true;
                    matchingGroupByFieldReverse.IsLinkReversed = true;
                    continue;
                }

                if (select.Source != null)
                {
                    //find a matching field in the grouping parameters
                    var matchingGroupByField = groupBy.FirstOrDefault(x => string.Equals(GetTargetPath(x), select.Source, StringComparison.InvariantCultureIgnoreCase));

                    if (matchingGroupByField != null)
                    {
                        matchingGroupByField.IsAutoSelect = true;
                        matchingGroupByField.LinkedSelect = select;
                        select.LinkedGroupBy = matchingGroupByField;
                        select.IsBasedOnGrouping = true;
                        continue;
                    }
                }
            }

            foreach (var select in selects)
            {
                if (select.IsStar)
                {
                    foreach (var g in groupBy)
                    {
                        g.IsAutoSelect = true;
                    }
                }
            }
        }

        private static string GetSourcePath(DimensionExpression x)
        {
            var dimensionChild = x.Child as SelectorExpression;
            if(dimensionChild != null)
            {
                var dimChild = dimensionChild.Child as DimensionExpression;
                if (dimChild != null)
                {
                    var result = GetSourcePath(dimChild);
                    return result;
                }
            }

            return x.Source;
        }

    }
}
