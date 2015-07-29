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
        public static Expression GetPagingExpression(GetExpression paging, Type itemType)
        {
            var collectionType = typeof(IEnumerable<>).MakeGenericType(itemType);
            var collectionParameter = Expression.Parameter(collectionType, "col");

            Expression result = null;

            if (paging.Ordering != null)
            {
                string[] properties = paging.Ordering.Source.Split('.');

                string methodName = paging.OrderDescending ? "OrderByDescending" : "OrderBy";

                var xParameter = Expression.Parameter(itemType, "x");

                Expression currentExpression = xParameter;
                var type = itemType;

                foreach (string current in properties)
                {
                    var member = GetMember(type, current);
                    currentExpression = Expression.MakeMemberAccess(currentExpression, member);
                    type = GetMemberType(member);
                }

                Type delegateType = typeof(Func<,>).MakeGenericType(itemType, type);
                LambdaExpression lambda = Expression.Lambda(delegateType, currentExpression, xParameter);

                var methodInfo = typeof(Enumerable).GetMethods().Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2).MakeGenericMethod(itemType, type);

                result = Expression.Call
                (
                    method: methodInfo,
                    arg0: collectionParameter,
                    arg1: lambda
                );
            }
            if (paging.Skip.HasValue)
            {
                int skip = paging.Skip.Value;

                if (skip < 0)
                {
                    throw new ArgumentException();
                }

                result = AttachMethodExpression(result, "Skip", skip, itemType);
            }
            if (paging.Take.HasValue)
            {
                int take = paging.Take.Value;

                if (take < 0)
                {
                    throw new ArgumentException();
                }

                result = AttachMethodExpression(result, "Take", take, itemType);
            }

            result = Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(collectionParameter.Type, collectionParameter.Type),
                parameters: collectionParameter,
                body: result
            );

            return result;
        }

    }
}
