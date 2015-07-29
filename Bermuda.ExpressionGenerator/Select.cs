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
        private static Expression MakeSelectExpression(Expression filter, DimensionExpression[] selects, Type itemType, Type resultType, GetExpression get, Expression source, string paramname)
        {
            var collectionParameter = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(itemType), paramname);

            var elementEnum = MakeWhereExpression(filter, source ?? collectionParameter, null, itemType);

            
            //.AsParallel()
            var res = AppendAsParallel(typeof(ParallelEnumerable), elementEnum);
            elementEnum = res;
            

            var selectLinqClass = typeof(ParallelEnumerable);
            var genericSelectInfos = selectLinqClass.GetMethods().Where(x => x.Name == "Select" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericSelectInfo = genericSelectInfos.FirstOrDefault();
            var selectInfo0 = genericSelectInfo.MakeGenericMethod(itemType, resultType);
            var selectParam = Expression.Parameter(type: itemType, name: "i");

            var memberBindings = new List<MemberBinding>();

            foreach (var select in selects)
            {
                if (select.IsStar)
                {
                    foreach (var field in resultType.GetFields())
                    {
                        var sourceMember = itemType.GetField(field.Name);

                        var binding = Expression.Bind
                        (
                            field,
                            Expression.MakeMemberAccess(selectParam, sourceMember)
                        );

                        memberBindings.Add(binding);
                    }
                }
                else if (select.Function == null)
                {
                    var targetField = GetField(resultType, GetTargetPath(select));
                    //var sourceField = GetField(itemType, select.Source);
                    var sourceExpr = MakeDimensionExpression(itemType, selectParam, select);

                    var binding = Expression.Bind
                    (
                        member: targetField,
                        expression: sourceExpr //Expression.MakeMemberAccess(selectParam, sourceField)
                    );

                    memberBindings.Add(binding);
                }
                else
                {
                    var targetField = GetField(resultType, GetTargetPath(select));
                    var functionExpr = MakeFunctionCallExpression(select, itemType, selectParam);

                    var binding = Expression.Bind
                    (
                        member: targetField,
                        expression: functionExpr
                    );

                    memberBindings.Add(binding);
                }
            }

            var newObjectDecl = Expression.MemberInit
            (
                newExpression: Expression.New(resultType),
                bindings: memberBindings
            );

            var lambda = Expression.Lambda(newObjectDecl, selectParam);

            Expression call = Expression.Call(method: selectInfo0, arg0: elementEnum, arg1: lambda);

            if (get != null && get.Take.HasValue)
            {
                call = AppendTake(typeof(Enumerable), resultType, call, get.Take.Value);
            }

            var result = Expression.Lambda(call, collectionParameter);

            return result;
        }


        private static Expression MakeSelectExpressionEx(Expression filter, DimensionExpression[] groupBy, DimensionExpression[] selects, Expression collectionParameter, Type itemType, Type metadataType, Type groupingType, Type resultType)
        {
            //if (groupBy.Any())
            //{
            //groupBy.First().GroupingEnumParameter = Expression.Parameter(type: typeof(GroupMetadata), name: MakeNumericName("gmd", 0));
            //}

            var elementEnum = MakeWhereExpression(filter, collectionParameter, groupBy, itemType);

            if (groupBy != null && selects != null)
            {
                //var groupParams = new ParameterExpressionWrapper[] { new ParameterExpressionWrapper { Expr = param } };
                elementEnum = MakeGroupByExpressionEx(elementEnum, groupBy, selects, 0, itemType, metadataType, groupingType, resultType);
            }

            return elementEnum;
        }
    }
}
