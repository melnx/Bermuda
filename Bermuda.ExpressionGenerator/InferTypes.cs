using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        public static Type InferClrType(DimensionExpression[] selects, DimensionExpression[] groupBy, Type itemType)
        {
            if (selects == null || !selects.Any()) return itemType;

            var containsAggregates = selects.Any(x => x.IsAggregate);

            Dictionary<string, Type> requiredFields = null;

            if (groupBy != null || containsAggregates)
            {
                requiredFields = InferReduceType(selects, groupBy, itemType);
            }
            else
            {
                requiredFields = InferSelectType(selects, itemType);
            }

            var type = LinqRuntimeTypeBuilder.GetDynamicType(requiredFields);

            return type;

        }

        private static Dictionary<string, Type> InferReduceType(DimensionExpression[] selects, DimensionExpression[] groupBy, Type itemType)
        {
            var requiredFields = new Dictionary<string, Type>();

            int i = 0;

            //            if( groupBy != null && selects != null && selects.Any(x => x.IsStar))
            if (groupBy != null)
                foreach (var g in groupBy)
                {
                    //if( g.GroupBy == null ) continue;
                    if (g.LinkedSelect != null) continue;

                    var nameBase = GetTargetPath(g);

                    //if( g.

                    Type memberType = GetTargetTypeForGroupByDimension(itemType, g);

                    if (IsCollectionType(memberType))
                        memberType = GetTypeOfEnumerable(memberType);

                    if (IsTupleType(memberType))
                        memberType = memberType.GetGenericArguments().Last();
                    

                    requiredFields[nameBase] = memberType;

                    //if (memberType == typeof(string))
                    //{
                    //    requiredFields[nameBase + "_Hash"] = typeof(long);
                    //}

                    i++;
                }

            bool isCountSelected = false;
            bool isCountRequired = false;
            if (selects != null)
                foreach (var select in selects.Where(x => !x.IsStar))
                {

                    var isCountSel = string.Equals(select.Function, "Count", StringComparison.InvariantCultureIgnoreCase);
                    var isCountReq = IsCountRequiredForAggregate(select.Function);
                    isCountSelected = isCountSelected || isCountSel;
                    isCountRequired = isCountRequired || isCountReq;

                    var targetPath = GetTargetPath(select);
                    if (targetPath != null)
                    {
                        if (isCountSel)
                        {
                            requiredFields[targetPath] = typeof(long);
                        }
                        else if (select.Function != null)
                        {
                            var firstArg = select.Arguments.FirstOrDefault();
                            Type argType = null;

                            if (firstArg != null)
                            {
                                var paramLol = Expression.Parameter(itemType, "param");
                                var firstArgExpression = MakeDimensionExpression(itemType, paramLol, firstArg);
                                argType = firstArgExpression.Type;
                            }

                            var aggregateMethodInfo = GetAggregateFunction(select.Function, itemType, argType);
                            if (aggregateMethodInfo != null)
                            {
                                requiredFields[targetPath] = aggregateMethodInfo.ReturnType;
                            }
                            else
                            {
                                var methodInfo = GetFunctionInfo(select.Function);
                                if (methodInfo != null)
                                {
                                    requiredFields[targetPath] = methodInfo.ReturnType;
                                }
                                else
                                {
                                    throw new BermudaExpressionGenerationException("Unknown function:" + select.Function);
                                }
                            }
                        }
                        else
                        {
                            if (select.Child != null)
                            {
                                var expr =  select.CreateExpression(null);
                                requiredFields[targetPath] = expr.Type;
                            }
                            else
                            {
                                //try to retrieve the source MemberInfo from the original type
                                var member = GetMember(itemType, select.Source, false);

                                //set the inferred member to the retrieved type
                                if (member != null)
                                {
                                    if (select.LinkedGroupBy == null) throw new Exception("Invalid Column Name: " + select.Source);
                                    requiredFields[targetPath] = GetMemberType(member);
                                }
                                else
                                {
                                    //try to fall back to a groupby dimension
                                    var matchingGroupBy = groupBy.FirstOrDefault(x => string.Equals(x.Target, select.Source));
                                    if (matchingGroupBy != null)
                                    {
                                        var targetType = GetTargetTypeForGroupByDimension(itemType, matchingGroupBy);
                                        requiredFields[targetPath] = targetType;
                                    }
                                    else
                                    {
                                        //if source path can't be found just use the column name as a literal value
                                        requiredFields[targetPath] = select.SourceType;
                                    }
                                }
                            }
                        }
                    }

                }

            if (!isCountSelected && isCountRequired)
            {
                requiredFields[CountTargetPath] = typeof(long);
            }
            return requiredFields;
        }


        public static Expression GetMapreduceExpression(Expression filter, DimensionExpression[] selects, DimensionExpression[] groupBy, Type itemType, GetExpression get, Expression source, string paramname)
        {
            var hash = (filter == null ? 0 : filter.ToString().GetHashCode()) + "|" + (selects == null ? "" : string.Join(",", selects.Select(x => x.GetChecksum()))) + "|" + (groupBy == null ? "" : string.Join(",", groupBy.Select(x => x.GetChecksum()))) + "|" + (get == null ? null : get.Take);
            Expression expr;

            var containsAggregates = selects != null && selects.Any(x => x.IsAggregate);

            LinkDimensions(groupBy, selects);

            var resultType = InferClrType(selects, groupBy, itemType);

            if (expressionCache.TryGetValue(hash, out expr))
            {
                return expr;
            }

            //a flat select transformation
            if (selects != null && groupBy == null && !containsAggregates)
            {
                expr = MakeSelectExpression(filter, selects, itemType, resultType, get, source, paramname);
            }
            else
            {
                //a non-grouping aggregate like "SELECT COUNT(*) FROM table"
                if (groupBy == null || !groupBy.Any()) groupBy = new DimensionExpression[] { new DimensionExpression { Source = null, Function = null } };

                expr = MakeMapreduceExpression(filter, selects, groupBy, itemType, resultType, get, source, paramname);
            }

            expressionCache[hash] = expr;

            return expr;
        }

        private static Dictionary<string, Type> InferSelectType(DimensionExpression[] selects, Type itemType)
        {
            var requiredFields = new Dictionary<string, Type>();

            var itemParam = Expression.Parameter(itemType, "p");

            foreach (var select in selects)
            {
                if (select.Function == null)
                {

                    //var sourceField = GetField(itemType, select.Source);
                    var sourceExpression = MakeDimensionExpression(itemType, itemParam, select);

                    if (!select.IsStar)
                    {
                        var targetPath = GetTargetPath(select);

                        if (targetPath == null) throw new BermudaExpressionGenerationException("No alias provided for the select field: " + select);

                        requiredFields[targetPath] = sourceExpression.Type;
                    }
                    else
                    {
                        foreach (var field in itemType.GetFields().Where(x => !IsCollectionType(x.FieldType) ))
                        {
                            requiredFields[field.Name] = field.FieldType;
                        }
                    }
                }
                else
                {
                    if (select.Target == null) throw new BermudaExpressionGenerationException("Alias not provided for function call: " + select);

                    //var methodInfo = GetFunctionInfo(select.Function);

                    if (IsAggregateFunction(select.Function)) throw new Exception("Cannot use an aggregate function without grouping or while having a non-aggregate select");

                    var functionCallExpr = MakeFunctionCallExpression(select, itemType, Expression.Parameter(itemType, "x"));


                    var targetPath = GetTargetPath(select);
                    requiredFields[targetPath] = functionCallExpr.Type;

                }
            }

            return requiredFields;
        }


    }
}
