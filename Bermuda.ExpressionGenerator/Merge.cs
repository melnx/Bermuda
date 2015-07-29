
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
        public static Expression GetMergeInvocationExpression(DimensionExpression[] selects, DimensionExpression[] groupBy, Type elementType)
        {

            if ((groupBy == null || !groupBy.Any()) && !selects.All(x => x.IsAggregate)) return null;

            //var elementType = ReduceExpressionGeneration.GetTypeOfEnumerable( collectionType );
            LinkDimensions(groupBy, selects);

            var collectionType = typeof(IEnumerable<>).MakeGenericType(elementType);

            var collectionParameter = Expression.Parameter(collectionType, "col");

            var genericGroupByInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "GroupBy" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericGroupByInfo = genericGroupByInfos.FirstOrDefault();
            var groupByInfo = genericGroupByInfo.MakeGenericMethod(elementType, elementType);

            var lambdaParam = Expression.Parameter(elementType, "x");

            var groupingDimensionBindings = new List<MemberAssignment>();

            
            var fields = elementType.GetFields();
            foreach (var g in groupBy)
            {
                var tar = GetTargetPath(g);
                var tar2 = g.LinkedSelect == null ? null : GetTargetPath(g.LinkedSelect);
                //fields.Any(x => string.Equals(x.Name, tar, StringComparison.InvariantCultureIgnoreCase))
                //if( !sel.IsBasedOnGrouping )   continue;
                //if (string.Equals(GetTargetPath(sel), CountTargetPath)) continue;

                var targetField = fields.FirstOrDefault
                (
                    x =>  g.LinkedSelect == null 
                    ? string.Equals(x.Name, tar, StringComparison.InvariantCultureIgnoreCase) 
                    : string.Equals(x.Name, tar2, StringComparison.InvariantCultureIgnoreCase 
                ));

                var binding = Expression.Bind(targetField, Expression.MakeMemberAccess(lambdaParam, targetField));
                groupingDimensionBindings.Add(binding);
            }
            

            /*
            foreach (var x in elementType.GetFields())
            {
                if (selects.Any(s => !s.IsBasedOnGrouping && string.Equals(x.Name, GetTargetPath(s), StringComparison.InvariantCultureIgnoreCase)))
                    continue;



                var binding = Expression.Bind(x, Expression.MakeMemberAccess(lambdaParam, GetField(elementType, x.Name)));
                groupingDimensionBindings.Add(binding);
            }*/

            var groupByLambda = Expression.Lambda
            (
                parameters: lambdaParam,
                body: Expression.MemberInit
                (
                    Expression.New(elementType),
                    groupingDimensionBindings
                )
            );

            var pointGroups = Expression.Call(method: groupByInfo, arg0: collectionParameter, arg1: groupByLambda);

            var enumType = pointGroups.Type;
            var groupingType = GetTypeOfEnumerable(enumType);
            var genericSelectInfo = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name == "Select" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var selectInfo0 = genericSelectInfo.MakeGenericMethod(groupingType, elementType);

            var selectGroupParam = Expression.Parameter(type: groupingType, name: "g");
            //var selectBody = Expression.Invoke(mergeExpr, selectGroupParam);
            var mergeExpr = GetMergeExpression(elementType, selectGroupParam, selects, groupBy );


            var selectLambda = Expression.Lambda(parameters: selectGroupParam, body: mergeExpr);

            var selectExpr = Expression.Call(method: selectInfo0, arg0: pointGroups, arg1: selectLambda);

            //selectExpr = AppendToArray(elementType, selectExpr);

            var finalLambda = Expression.Lambda(selectExpr, collectionParameter);

            return finalLambda;
        }

        public static MethodCallExpression AppendToArray(Type elementType, MethodCallExpression selectExpr)
        {
            var genericToArrayInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "ToArray" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericToArrayInfo = genericToArrayInfos.FirstOrDefault();
            var toArrayInfo = genericToArrayInfo.MakeGenericMethod(elementType);

            selectExpr = Expression.Call(method: toArrayInfo, arg0: selectExpr);
            return selectExpr;
        }


        private static Expression AttachMethodExpression(Expression source, string methodName, object parameter, Type itemType)
        {
            var methodInfo = typeof(Enumerable).GetMethods().Single(method => method.Name == methodName && method.GetParameters().Length == 2 && method.GetParameters().Skip(1).Single().ParameterType == parameter.GetType()).MakeGenericMethod(itemType);

            return Expression.Call
            (
                method: methodInfo,
                arg0: source,
                arg1: Expression.Constant(parameter)
            );
        }

        private static Expression GetMergeExpression(Type elementType, ParameterExpression selectGroupParam, DimensionExpression[] selects, DimensionExpression[] groupBy)
        {
            //if (select.Aggregate == "First")
            //{
            //    var genericFirstOrDefaultInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "FirstOrDefault" && x.IsGenericMethod && x.GetParameters().Length == 1);
            //    var genericFirstOrDefaultInfo = genericFirstOrDefaultInfos.FirstOrDefault();
            //    var firstOrDefaultInfo = genericFirstOrDefaultInfo.MakeGenericMethod(elementType);
            //    var mergeExpr = Expression.Call(method: firstOrDefaultInfo, arg0: selectGroupParam);
            //    return mergeExpr;
            //}

            //if ((groupBy == null || !groupBy.Any()) && !selects.All(x => ReduceExpressionGeneration.IsAggregateFunction(x.Function) || x.IsStar)) return null;

            var memberBindigs = new List<MemberAssignment>();

            /*
            //group by all the non-select fields
            foreach (var x in elementType.GetFields())
            {
                if (selects.Any(s => string.Equals(x.Name, GetTargetPath(s), StringComparison.InvariantCultureIgnoreCase))) continue;

                var keyAccess = Expression.MakeMemberAccess
                (
                    member: selectGroupParam.Type.GetProperty("Key"),
                    expression: selectGroupParam
                );

                var memberAccess = Expression.Bind
                (
                    member: x,
                    expression: Expression.MakeMemberAccess
                    (
                        member: x,
                        expression: keyAccess
                    )
                );

                memberBindigs.Add(memberAccess);
            }*/

            var fields = elementType.GetFields();
            foreach (var sel in fields)
            {
                //if (sel.IsStar) continue;

                //var tar = GetTargetPath(sel);
                //if (fields.Any(x => string.Equals(x.Name, tar, StringComparison.InvariantCultureIgnoreCase))) continue;

                if (selects.Any(x => string.Equals(GetTargetPath(x), sel.Name, StringComparison.InvariantCultureIgnoreCase))) continue;
                if (string.Equals(sel.Name, CountTargetPath, StringComparison.InvariantCultureIgnoreCase)) continue;

                //var targetMember = fields.FirstOrDefault(x => string.Equals(x.Name, tar));
                var targetMember = sel;

                var keyAccess = Expression.MakeMemberAccess
                (
                    member: selectGroupParam.Type.GetProperty("Key"),
                    expression: selectGroupParam
                );

                var memberAccess = Expression.Bind
                (
                    member: targetMember,
                    expression: Expression.MakeMemberAccess
                    (
                        member: targetMember,
                        expression: keyAccess
                    )
                );

                memberBindigs.Add(memberAccess);
            }

            var actualSelects = selects.ToList();

            if (!actualSelects.Any(x => string.Equals(x.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase)) && actualSelects.Any(x => IsCountRequiredForAggregate(x.Function) ))
            {
                actualSelects.Add(new DimensionExpression { Function = CountAggregateString, Target = CountTargetPath } );
            }

            foreach(var g in groupBy)
            {
                if (g.IsAutoSelect && g.LinkedSelect == null)
                {
                    var groupByTargetPath = GetTargetPath(g);
                    var selectTargetPath = g.LinkedSelect == null ? null : GetTargetPath(g.LinkedSelect);
                    actualSelects.Add(new DimensionExpression
                    {
                        Source = groupByTargetPath,
                        Target = selectTargetPath ?? groupByTargetPath,
                        IsBasedOnGrouping = true
                    });
                }
                //else
                //{
                //    var groupByTargetPath = GetTargetPath(g);
                //    if( g.LinkedSelect. != null ) groupByTargetPath = GetTargetPath(g.LinkedSelect);
                //    var matchingField = fields.FirstOrDefault(x => string.Equals(x.Name, groupByTargetPath, StringComparison.InvariantCultureIgnoreCase));
 
                    
                //}
            }

            foreach (var sel in actualSelects)
            {
                if (sel.IsStar) continue;

                var targetPath = GetTargetPath(sel);

                var targetField = GetField(elementType, targetPath);

                if (sel.IsBasedOnGrouping)
                {
                    var keyPropertyInfo = selectGroupParam.Type.GetProperty("Key");
                    var keyMemberAccess = Expression.MakeMemberAccess(selectGroupParam, keyPropertyInfo);
                    var keyPropertyAccess = Expression.MakeMemberAccess(keyMemberAccess, targetField);

                    memberBindigs.Add(Expression.Bind
                    (
                        member: targetField,
                        expression: keyPropertyAccess
                    ));
                }
                else
                {
                    //if( targetField.FieldType != ty

                    var genericSumInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "Sum" && x.IsGenericMethod && x.GetParameters().Length == 2 && x.ReturnType == targetField.FieldType);
                    var genericSumInfo = genericSumInfos.FirstOrDefault();
                    var sumInfo = genericSumInfo.MakeGenericMethod(elementType);

                    var genericSumInfos2 = typeof(Enumerable).GetMethods().Where(x => x.Name == "Sum" && x.IsGenericMethod && x.GetParameters().Length == 2 && x.ReturnType == typeof(long));
                    var genericSumInfo2 = genericSumInfos2.FirstOrDefault();
                    var sumInfo2 = genericSumInfo2.MakeGenericMethod(elementType);

                    ////TargetField = g.Sum(p => p.TargetField * p._Count) / g.Sum(p => p._Count)
                    if ( string.Equals(sel.Function, "Average", StringComparison.InvariantCultureIgnoreCase) )
                    {
                        var actualCountTargetPath = CountTargetPath;
                        var countSelect = actualSelects.FirstOrDefault(x => string.Equals(x.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase));

                        if (countSelect == null) throw new Exception("The provided type has no required count field");

                        actualCountTargetPath = GetTargetPath(countSelect);

                        var pointParameter = Expression.Parameter(elementType, "p1");
                        var countAccess = Expression.MakeMemberAccess(pointParameter, GetField(elementType, actualCountTargetPath));
                        var targetPathAccess = Expression.MakeMemberAccess(pointParameter, GetField(elementType, targetPath));
                        var sumBody = Expression.Multiply
                        (
                            left: (countAccess.Type == targetPathAccess.Type) ? (Expression)countAccess : Expression.Convert(countAccess, targetPathAccess.Type),
                            right: targetPathAccess
                        );

                        var pointParameter2 = Expression.Parameter(elementType, "p2");
                        var sumBody2 = Expression.MakeMemberAccess(pointParameter2, GetField(elementType, actualCountTargetPath));

                        Expression sumOfProducts = Expression.Call(method: sumInfo, arg0: selectGroupParam, arg1: Expression.Lambda(parameters: pointParameter, body: sumBody));
                        Expression sumOfCounts = Expression.Call(method: sumInfo2, arg0: selectGroupParam, arg1: Expression.Lambda(parameters: pointParameter2, body: sumBody2));

                        var division = Expression.Divide
                        (
                            left: sumOfProducts,
                            right: sumOfProducts.Type != sumOfCounts.Type ? Expression.Convert(sumOfCounts, sumOfProducts.Type) : sumOfCounts
                        );

                        memberBindigs.Add(Expression.Bind
                        (
                            member: targetField,
                            expression: division
                        ));
                    }

                    //TargetField = g.Sum(p => p.TargetField)
                    else// (string.Equals(sel.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase) || string.Equals(sel.Function, "Sum", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var pointParameter = Expression.Parameter(elementType, "p0");
                        var sumBody = Expression.MakeMemberAccess(pointParameter, GetField(elementType, targetPath));

                        memberBindigs.Add(Expression.Bind
                        (
                            member: targetField,
                            expression: Expression.Call(method: sumInfo, arg0: selectGroupParam, arg1: Expression.Lambda(parameters: pointParameter, body: sumBody)))
                        );
                    }

                }
            }

            var result = Expression.MemberInit
            (
                newExpression: Expression.New(elementType),
                bindings: memberBindigs
            );

            return result;
        }

    }
}
