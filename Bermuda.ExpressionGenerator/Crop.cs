using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        public static Expression AppendCropping(Expression source, DimensionExpression[] selects, DimensionExpression[] groupBy, Type elementType)
        {
            //listlol.GroupBy(x => x.OccurredOn).Take(5).SelectMany(x => x).GroupBy(x => x.Name).Take(5).SelectMany(x => x);
            
            var genericGroupByInfos = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "GroupBy" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericGroupByInfo = genericGroupByInfos.FirstOrDefault();
            var groupByInfo = genericGroupByInfo.MakeGenericMethod(elementType, elementType);

            var genericTakeInfos = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "Take" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericTakeInfo = genericTakeInfos.FirstOrDefault();
            var takeInfo = genericTakeInfo.MakeGenericMethod(elementType, elementType);

            List<MemberBinding> memberBindings = new List<MemberBinding>();

            var fields = elementType.GetFields();

            //var valueField = fields.FirstOrDefault(x => !groupBy.Any(g => string.Equals(GetTargetPath(g), x.Name, StringComparison.InvariantCultureIgnoreCase)));

            foreach (var g in groupBy)
            {
                if (g.Take.HasValue)
                {
                    var targetPath = GetTargetPath(g);

                    var targetMember = GetMember(elementType, targetPath);

                    var iParam = Expression.Parameter( elementType, "i" );

                    var memberAccess = Expression.MakeMemberAccess(iParam, targetMember);

                    var lambda = Expression.Lambda(memberAccess, iParam);

                    var groupByCall = Expression.Call
                    (
                        method: groupByInfo,
                        arg0: source,
                        arg1: lambda
                    );
                }
            }

            return null;
        }
    }
}
