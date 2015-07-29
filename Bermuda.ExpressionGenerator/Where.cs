using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.ExpressionGeneration;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        private static Expression MakeWhereExpression(Expression filter, Expression collectionParameter, DimensionExpression[] groupBy, Type itemType)
        {
            List<Expression> filters = new List<Expression>();
            var elementType = GetTypeOfEnumerable(collectionParameter.Type);
            var genericWhereInfo = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name == "Where" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var whereInfo = genericWhereInfo.MakeGenericMethod(elementType);

            var mentionParameter = Expression.Parameter(type: elementType, name: GenerateParamName(elementType));

            if (groupBy != null)
            {
                var collectionFields = groupBy.Where(x => x.Source != null).Select(x => new { x.Source, Member = GetMember(itemType, x.Source) });

                collectionFields = collectionFields.Where(x => TypeImplements(GetMemberType(x.Member), typeof(IEnumerable<>)));

                var count = collectionFields.Count();
                if (count == 0 && filter == null) return collectionParameter;

                filters.AddRange(collectionFields.Select
                (
                    x => Expression.ReferenceNotEqual
                    (
                        left: Expression.MakeMemberAccess(mentionParameter, x.Member),
                        right: Expression.Constant(null)
                    )
                ).Cast<Expression>());
            }

            if (filter != null) filters.Add(ParameterRebinder.ReplaceParameters(filter, "x", mentionParameter));

            if (!filters.Any()) return collectionParameter;

            Expression sourceCollection = collectionParameter;
            sourceCollection = AppendAsParallel(typeof(ParallelEnumerable), collectionParameter);

            var whereBody = ChainAndExpressionCollection(filters);

            var result = Expression.Call
            (
                method: whereInfo,
                arguments: new Expression[]
                {
                    sourceCollection,
                    Expression.Lambda
                    (
                        delegateType: typeof(Func<,>).MakeGenericType(elementType, typeof(bool)),
                        parameters: mentionParameter, 
                        body: whereBody
                    )
                }
            );

            //collectionParameter = Expression.Parameter(type: result.Type, name: "c");

            return result;
        }

    }
}
