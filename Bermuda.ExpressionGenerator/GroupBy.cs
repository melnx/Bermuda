using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.Interface;
using System.Reflection;
using Bermuda.ExpressionGeneration;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        private static Expression MakeGroupByExpressionEx(Expression collectionParameter, DimensionExpression[] groupBy, DimensionExpression[] selects, int depth, Type itemType, Type metadataType, Type groupingType, Type resultType)
        {
            var linqExtensionClassType = !groupBy.Any(x => x.ParallelizedLinq) ? PLinqExtensionClassType : LinqExtensionClassType;
            var linqEnumType = linqExtensionClassType == PLinqExtensionClassType ? PLinqEnumType : LinqEnumType;

            var remDepth = groupBy.Skip(depth).Count();
            var currentDimension = groupBy.ElementAt(depth);
            var currentGroupBy = currentDimension.Source;
            var currentFunction = currentDimension.Function;
            var currentChild = currentDimension.Child;

            if (currentDimension.LinkedSelect != null)
            {
                currentFunction = currentDimension.LinkedSelect.Function;
            }

            if (currentGroupBy == null && currentFunction == null && currentChild == null)
            {
                if (remDepth > 1)
                {
                    var newCollectionParameter = Expression.Parameter(type: typeof(EnumMetadata<>).MakeGenericType(linqEnumType.MakeGenericType(itemType)), name: MakeNumericName("gmd", depth + 1));
                    groupBy.ElementAt(depth + 1).GroupingEnumParameter = newCollectionParameter;
                    return MakeGroupByExpressionEx(collectionParameter, groupBy, selects, depth + 1, itemType, metadataType, groupingType, resultType);
                }
            }

            var sourceCollection = collectionParameter.Type.GetGenericTypeDefinition() == typeof(EnumMetadata<>)
                ? Expression.MakeMemberAccess(collectionParameter, collectionParameter.Type.GetField("Enum"))
                : collectionParameter;

            if (linqEnumType == PLinqEnumType && !TypeImplements(GetTypeOfEnumerable(sourceCollection.Type), PLinqEnumType))
            {
                //.AsParallel()
                var res = AppendAsParallel(linqExtensionClassType, sourceCollection);

                groupBy.ElementAt(depth).ParallelizedLinq = true;

                sourceCollection = res;
            }

            var elementType = GetTypeOfEnumerable(sourceCollection.Type);
            var selectType = resultType;

            Expression groupByExpr = null;

            DimensionExpression sortValueType = null;

            MemberInfo groupByMember;

            Expression currentChildExpression = null;

            if (currentChild != null)
            {
                currentChildExpression = currentChild.CreateExpression(null);
            }

            if (currentGroupBy == null && currentFunction == null && currentChild == null)
            {
                groupByExpr = Expression.NewArrayInit(sourceCollection.Type, sourceCollection);
            }
            else if ((groupByMember = GetMember(itemType, currentGroupBy)) != null && TypeImplements(GetMemberType(groupByMember), typeof(IEnumerable<>)) || (currentChildExpression != null && TypeImplements(currentChildExpression.Type, typeof(IEnumerable<>))))
            {
                //.SelectMany(m => m.Ngrams, (m, t) => new MentionMetadata { Mention = m, Id = t })
                var genericSelectManyInfos = linqExtensionClassType.GetMethods().Where(x => x.Name == "SelectMany" && x.IsGenericMethod && x.GetParameters().Length == 3);
                var extensionIndex = (linqExtensionClassType == typeof(ParallelEnumerable)) ? 0 : 1;
                var genericSelectManyInfo = genericSelectManyInfos.Skip(extensionIndex).FirstOrDefault();

                var collectionSelectorParameter = Expression.Parameter(type: elementType, name: GenerateParamName(elementType, depth));

                var sourcePath = GetSourcePath(currentDimension);

                var collectionGroupingMemberAccess = Expression.MakeMemberAccess(collectionSelectorParameter, GetField(collectionSelectorParameter.Type, sourcePath));

                var collectionSelector = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType(elementType, collectionGroupingMemberAccess.Type),
                    parameters: collectionSelectorParameter,
                    body: collectionGroupingMemberAccess
                );

                var keyType = GetTypeOfEnumerable(collectionGroupingMemberAccess.Type);

                var selectManyInfo = genericSelectManyInfo.MakeGenericMethod(elementType, keyType, metadataType);

                var resultSelectorParameters = new ParameterExpression[]
                {
                    Expression.Parameter(type: elementType, name: GenerateParamName(elementType, depth) ),
                    Expression.Parameter(type: keyType, name: GenerateParamName(keyType, depth) )
                };

                var memberBindings = new List<MemberBinding>
                {
                    Expression.Bind
                    (
                        member: metadataType.GetField("Item"),
                        expression: resultSelectorParameters[0]
                    )
                };

                string groupByTargetPath = GetTargetPath(keyType);

                var targetField = GetField(metadataType, groupByTargetPath);

                Expression sourceExpression = resultSelectorParameters[1];

                if (IsTupleType(sourceExpression.Type))
                {
                    var sourceItemInfo = sourceExpression.Type.GetProperty("Item2");

                    sourceExpression = Expression.MakeMemberAccess
                    (
                        expression:   sourceExpression,
                        member :sourceItemInfo
                    );
                }

                memberBindings.Add(Expression.Bind
                (
                    member: targetField,
                    expression: sourceExpression
                ));

                var resultSelector = Expression.Lambda
                (
                    delegateType: typeof(Func<,,>).MakeGenericType(elementType, keyType, metadataType),
                    parameters: resultSelectorParameters,
                    body: Expression.MemberInit
                    (
                        newExpression: Expression.New(type: metadataType),
                        bindings: memberBindings
                    )
                );

                var selectManyExpr = Expression.Call
                (
                    method: selectManyInfo,
                    arguments: new Expression[]
                    {
                        sourceCollection,
                        collectionSelector,
                        resultSelector
                    }
                );

                //.GroupBy(md => md.Id, md => md.Mention)
                var keySelectorParameter = Expression.Parameter(type: metadataType, name: "md");
                var resultSelectorParameter = Expression.Parameter(type: metadataType, name: "md");

                var keySelectorBody = Expression.MakeMemberAccess(expression: keySelectorParameter, member: GetField(metadataType, groupByTargetPath));
                var resultSelectorBody = Expression.MakeMemberAccess(expression: resultSelectorParameter, member: metadataType.GetField("Item"));

                var genericGroupByInfos = linqExtensionClassType.GetMethods().Where(x => x.Name == "GroupBy" && x.IsGenericMethod && x.GetParameters().Length == 3);
                var genericGroupByInfo = genericGroupByInfos.ElementAt(1);
                var groupByInfo = genericGroupByInfo.MakeGenericMethod(metadataType, keySelectorBody.Type, resultSelectorBody.Type);

                var keySelectorLambda = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType(metadataType, keySelectorBody.Type),
                    parameters: keySelectorParameter,
                    body: keySelectorBody
                );

                var resultSelectorLambda = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType(metadataType, resultSelectorBody.Type),
                    parameters: resultSelectorParameter,
                    body: resultSelectorBody
                );

                groupByExpr = Expression.Call
                (
                    method: groupByInfo,
                    arguments: new Expression[]
                    {
                        selectManyExpr,
                        keySelectorLambda,
                        resultSelectorLambda
                    }
                );
            }
            else
            {
                //.GroupBy(m => m.OccurredOn.Ticks - 28189283)
                var keySelectorParameter = Expression.Parameter(type: itemType, name: "m");

            
                Expression keySelectorBody = null;

                if (currentFunction != null)
                {
                    keySelectorBody = MakeFunctionCallExpression(currentDimension, itemType, keySelectorParameter);
                }
                else if(currentGroupBy != null)
                {
                    var sourcePath = GetSourcePath(currentDimension);
                    var targetMember = GetMember(itemType, sourcePath);
                    keySelectorBody = Expression.MakeMemberAccess(expression: keySelectorParameter, member: targetMember);
                }
                else if (currentChild != null)
                {
                    keySelectorBody = currentChild.CreateExpression(null);
                }
                else
                {
                    throw new BermudaExpressionGenerationException("Don't know how to handle for group by:" + currentDimension);
                }
                
                var genericGroupByInfos = linqExtensionClassType.GetMethods().Where(x => x.Name == "GroupBy" && x.IsGenericMethod && x.GetParameters().Length == 2);
                var genericGroupByInfo = genericGroupByInfos.FirstOrDefault();
                var groupByInfo = genericGroupByInfo.MakeGenericMethod(elementType, keySelectorBody.Type);

                keySelectorBody = ParameterRebinder.ReplaceParameters(keySelectorBody, "x", keySelectorParameter);

                var groupByLambda = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType(elementType, keySelectorBody.Type),
                    parameters: keySelectorParameter,
                    body: keySelectorBody
                );

                groupByExpr = Expression.Call
                (
                    method: groupByInfo,
                    arguments: new Expression[]
                    {
                        sourceCollection,
                        groupByLambda
                    }
                );
            }




            var inferredParameterType = typeof(EnumMetadata<>).MakeGenericType(GetTypeOfEnumerable(groupByExpr.Type));
            var groupingParameter = Expression.Parameter(type: inferredParameterType, name: MakeNumericName("gmd", depth));

            if (groupBy.Length > depth)
            {
                groupBy.ElementAt(depth).GroupingEnumParameter = groupingParameter;
            }

            //var loldas = temp == groupBy.ElementAt(depth);

            var enumType = GetTypeOfEnumerable(groupByExpr.Type);
            var enumMetadataType = typeof(EnumMetadata<>).MakeGenericType(enumType);


            //.AsParallel()
            //var genericAsParallelInfo = typeof(ParallelEnumerable).GetMethods().FirstOrDefault(x => x.Name == "AsParallel" && x.IsGenericMethod && x.GetParameters().Length == 1);
            //var asParallelInfo0 = genericAsParallelInfo.MakeGenericMethod(enumType);
            //groupByExpr = Expression.Call(method: asParallelInfo0, arg0: groupByExpr);

            //.Select(g => new GroupMetadata { Group = g, Value = g.Whatever() })
            var selectLinqClass = groupByExpr.Type.IsArray ? LinqExtensionClassType : linqExtensionClassType;
            var genericSelectInfos = selectLinqClass.GetMethods().Where(x => x.Name == "Select" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericSelectInfo = genericSelectInfos.FirstOrDefault();
            var selectInfo0 = genericSelectInfo.MakeGenericMethod(enumType, enumMetadataType);
            var selectGroupParam = Expression.Parameter(type: enumType, name: "g");

            var selectGroupMemberBindings = new List<MemberBinding>();

            //Group = g, Value
            selectGroupMemberBindings.Add(Expression.Bind(member: enumMetadataType.GetField("Enum"), expression: selectGroupParam));

            //Value = g.Whatever()
            if (currentDimension != null && currentDimension.Ordering != null && currentDimension.Ordering.Function != null)
            {
                sortValueType = currentDimension.Ordering;
                if (sortValueType != null)
                {
                    selectGroupMemberBindings.Add(MakeAggregateFunctionCallExpression(selectGroupParam, sortValueType, null, 0, enumMetadataType, "Value"));
                }
            }

            // = new GroupMetadata{}
            var selectInit = Expression.MemberInit
            (
                newExpression: Expression.New(type: enumMetadataType),
                bindings: selectGroupMemberBindings
            );

            var selectMetadataLambda = Expression.Lambda
            (
                parameters: selectGroupParam,
                body: selectInit
            );

            //.Select(...)
            var selectExpr = Expression.Call
            (
                method: selectInfo0,
                arg0: groupByExpr,
                arg1: selectMetadataLambda
            );

            Expression selectSourceExpression = selectExpr;

            //.OrderByDescending(g => g.Value)
            if (currentDimension != null && currentDimension.Ordering != null && (currentDimension.Ordering.Function != null || currentDimension.Ordering.Source != null))
            {
                var orderFuncName = currentDimension.OrderDescending ? "OrderByDescending" : "OrderBy";
                var genericOrderByInfo = linqExtensionClassType.GetMethods().FirstOrDefault(x => x.Name == orderFuncName && x.IsGenericMethod && x.GetParameters().Length == 2);
                var orderByInfo = genericOrderByInfo.MakeGenericMethod(enumMetadataType, typeof(long));
                var groupOrderParam = Expression.Parameter(type: enumMetadataType, name: "gg");
                var orderByExpr = Expression.Call
                (
                    method: orderByInfo,
                    arg0: selectExpr,
                    arg1: Expression.Lambda(parameters: groupOrderParam, body: Expression.MakeMemberAccess(expression: groupOrderParam, member: enumMetadataType.GetField("Value")))
                );

                selectSourceExpression = orderByExpr;
            }

            //.Take(5)
            if (currentDimension.Take.HasValue)
            {
                selectSourceExpression = AppendTake(linqExtensionClassType, enumMetadataType, selectSourceExpression, currentDimension.Take.Value);
            }

            if (remDepth <= 1)
            {
                //if (currentGroupBy == GroupByTypes.None) return collectionParameter;

                //.Select(gmd2 => new InferredType { Id = gmd.Group.Key, Id2 = gmd2.Group.Key, TargetPath = gmd2.SourcePath })
                var selectParameterType = GetTypeOfEnumerable(selectSourceExpression.Type);
                var selectInfo = genericSelectInfo.MakeGenericMethod(selectParameterType, resultType);

                var selectMemberBindings = new List<MemberAssignment>();

                //gmd2
                var parentGroupBy = groupBy.ElementAt(depth);
                var parentGroupParameter = parentGroupBy.GroupingEnumParameter;

                //gmd.Group
                var actualGroupAccess = Expression.MakeMemberAccess(parentGroupParameter, parentGroupParameter.Type.GetField("Enum"));
                //gmd.Value
                var groupValueAccess = Expression.MakeMemberAccess(parentGroupParameter, parentGroupParameter.Type.GetField("Value"));

                var lastGroupBy = groupBy.LastOrDefault();
                var computedValueForGroup = lastGroupBy.Ordering; // lastGroupBy.IsDateTime ? null : lastGroupBy.OrderBy;

                var countSelect = selects.FirstOrDefault(x => string.Equals(x.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase));

                if (countSelect != null)
                {
                    //CountAlias = 
                    var countBinding = MakeAggregateFunctionCallExpression(actualGroupAccess, new DimensionExpression { Function = CountAggregateString, Target = GetTargetPath(countSelect) }, computedValueForGroup != null && computedValueForGroup.Function == CountAggregateString ? groupValueAccess : null, 0, resultType, null);
                    selectMemberBindings.Add(countBinding);
                }
                else if (selects.Any(x => IsCountRequiredForAggregate(x.Function)))
                {
                    //_Count = 
                    var countBinding = MakeAggregateFunctionCallExpression(actualGroupAccess, new DimensionExpression { Function = CountAggregateString, Target = CountTargetPath }, computedValueForGroup != null && computedValueForGroup.Function == CountAggregateString ? groupValueAccess : null, 0, resultType, null);
                    selectMemberBindings.Add(countBinding);
                }

                //Value =
                foreach (var select in selects.Where(x => !x.IsStar && !string.Equals(x.Function, CountAggregateString, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (select.IsBasedOnGrouping) continue;

                    if (select.IsFunctionCall)
                    {
                        var otherBinding = MakeAggregateFunctionCallExpression(actualGroupAccess, select, computedValueForGroup != null && computedValueForGroup.Equals(select) ? groupValueAccess : null, 0, resultType, null);

                        //it's not an aggregate... that's a problem
                        if (otherBinding == null) throw new BermudaExpressionGenerationException("Non aggregate function call in an aggregate query not allowed: " + select);

                        selectMemberBindings.Add(otherBinding);
                    }
                    else
                    {
                        var targetPath = GetTargetPath(select);

                        //var sourceField = GetMember(itemType, select.Source, false);

                        var targetFieldInfo = GetField(resultType, targetPath);

                        var actualValue = Convert.ChangeType(select.Source, select.SourceType);
                        selectMemberBindings.Add(Expression.Bind
                        (
                            targetFieldInfo, Expression.Constant(actualValue)
                        ));                        
                    }
                }

                //Id = gmd.Group.Key, Id2 = gmd2.Group.Key
                AddStarSelectColumns(groupBy, selects, resultType, selectMemberBindings);

                //gmd => new Datapoint{...}
                var selectLambda = Expression.Lambda
                (
                    parameters: parentGroupParameter,
                    body: Expression.MemberInit
                    (
                        newExpression: Expression.New(type: resultType),
                        bindings: selectMemberBindings
                    )
                );

                //.Select(...)
                var result = Expression.Call
                (
                    method: selectInfo,
                    arg0: selectSourceExpression,
                    arg1: selectLambda
                );

                return result;
            }
            else if (remDepth >= 2)
            {
                //var newCollectionParameter = Expression.Parameter(type: enumMetadataType, name: MakeNumericName("gmd", depth + 1));
                //groupBy.ElementAt(depth).GroupingEnumParameter = newCollectionParameter;

                var currentParameter = groupBy.ElementAt(depth).GroupingEnumParameter;

                var nestedExpression = MakeGroupByExpressionEx(currentParameter, groupBy, selects, depth + 1, itemType, metadataType, groupingType, resultType);

                var nestedExpressionLambda = Expression.Lambda
                (
                    parameters: currentParameter,
                    body: nestedExpression
                );


                //.SelectMany(gmd => Recurse())
                var sourceElementType = GetTypeOfEnumerable(selectSourceExpression.Type);
                var genericSelectManyInfos = linqExtensionClassType.GetMethods().Where(x => x.Name == "SelectMany" && x.IsGenericMethod && x.GetParameters().Length == 2);
                var genericSelectManyInfo = genericSelectManyInfos.Skip(0).FirstOrDefault();
                var selectManyInfo = genericSelectManyInfo.MakeGenericMethod(sourceElementType, selectType);

                var selectManyRecursiveExpr = Expression.Call
                (
                    method: selectManyInfo,
                    arg0: selectSourceExpression,
                    arg1: nestedExpressionLambda
                );

                return selectManyRecursiveExpr;

            }

            throw new Exception("not supposed to happen");
        }

        private static Expression AppendTake(Type linqExtensionClassType, Type itemType, Expression selectSourceExpression, int take)
        {
            var genericTakeInfo = linqExtensionClassType.GetMethods().FirstOrDefault(x => x.Name == "Take" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var takeInfo = genericTakeInfo.MakeGenericMethod(itemType);
            var takeExpr = Expression.Call(method: takeInfo, arg0: selectSourceExpression, arg1: Expression.Constant(take));

            selectSourceExpression = takeExpr;
            return selectSourceExpression;
        }

        public static bool IsTupleType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>);
        }

        private static Expression AppendAsParallel(Type linqExtensionClassType, Expression sourceCollection)
        {
            var genericAsParallelInfos = linqExtensionClassType.GetMethods().Where(x => x.Name == "AsParallel" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericAsParallelInfo = genericAsParallelInfos.FirstOrDefault();
            var asParallelInfo = genericAsParallelInfo.MakeGenericMethod(GetTypeOfEnumerable(sourceCollection.Type));

            var res = Expression.Call
            (
                method: asParallelInfo,
                arg0: sourceCollection
            );
            return res;
        }

        private static void AddStarSelectColumns(DimensionExpression[] groupBy, DimensionExpression[] selects, Type resultType, List<MemberAssignment> selectMemberBindings)
        {
            var starSelect = selects.FirstOrDefault(x => x.IsStar);

            int i = 0;
            foreach (var g in groupBy)
            {
                if (!(starSelect != null || g.IsAutoSelect)) continue;

                //if (g.Child != null)
                //{

                //}
                //else 
                    
                if (g.Source != null || g.Function != null || g.LinkedSelect != null || g.IsAutoSelect)
                {
                    var actualEnumAccess = Expression.MakeMemberAccess
                    (
                        expression: g.GroupingEnumParameter,
                        member: g.GroupingEnumParameter.Type.GetField("Enum")
                    );

                    var keyPropertyInfo = actualEnumAccess.Type.GetProperty("Key");

                    var keyAccess = Expression.MakeMemberAccess
                    (
                        expression: actualEnumAccess,
                        member: keyPropertyInfo
                    );

                    //var groupByTargetPath = GetTargetPath(keyPropertyInfo.PropertyType);
                    //var targetMemberName = MakeNumericName(groupByTargetPath, i);
                    var targetMemberName = GetTargetPath(g);

                    if (g.LinkedSelect != null)
                    {
                        targetMemberName = GetTargetPath(g.LinkedSelect);
                    }

                    //var isNumeric = keyPropertyInfo.PropertyType == typeof(long);

                    var field = GetField(resultType, targetMemberName);

                    var resultMemberAssignment = Expression.Bind
                    (
                        member: field,
                        expression: keyAccess
                    );

                    selectMemberBindings.Add(resultMemberAssignment);

                    //if (!isNumeric)
                    //{
                    //    var getHashCall = Expression.Call(instance: keyAccess, method: keyAccess.Type.GetMethod("GetHashCode"));

                    //    var setIdForStringGrouping = Expression.Bind
                    //    (
                    //        member: resultType.GetField( GetTargetPath(g) + "_Hash" ),
                    //        expression: Expression.Convert( type:typeof(long), expression: getHashCall )
                    //    );

                    //    selectMemberBindings.Add(setIdForStringGrouping);
                    //}
                }
                i++;
            }
            
        }

        public static Expression MakeFunctionCallExpression(DimensionExpression currentDimension, Type itemType, ParameterExpression keySelectorParameter)
        {
            //var functionName = currentDimension.Function ?? currentDimension.LinkedSelect.Function;

            if (currentDimension.Function == null && currentDimension.LinkedSelect != null) currentDimension = currentDimension.LinkedSelect;

            if ( string.Equals(currentDimension.Function, "convert", StringComparison.InvariantCultureIgnoreCase) )
            {
                if (currentDimension.Arguments.Count != 2) throw new BermudaExpressionGenerationException("Convert requires 2 arguments");
                Expression conv = MakeConvertExpression(currentDimension, itemType, keySelectorParameter);
                if (currentDimension.IsNegated) 
                    conv = Expression.Negate(conv);
                if (currentDimension.IsNotted) 
                    conv = Expression.Not(conv);
                return conv;
            }

            if (string.Equals(currentDimension.Function, "cast", StringComparison.InvariantCultureIgnoreCase))
            {
                if (currentDimension.Arguments.Count != 1) throw new BermudaExpressionGenerationException("Cast requires 1 argument");
                Expression conv = MakeConvertExpression(currentDimension, itemType, keySelectorParameter, currentDimension.Arguments.First().Target);
                if (currentDimension.IsNegated) 
                    conv = Expression.Negate(conv);
                if (currentDimension.IsNotted) 
                    conv = Expression.Not(conv);
                return conv;
            }

            Expression keySelectorBody;
            currentDimension.IsDateTime = true;
            var functionInfo = typeof(UtilityExtensions).GetMethods().Where(x => string.Equals(x.Name, currentDimension.Function, StringComparison.InvariantCultureIgnoreCase) && currentDimension.Arguments.Count == x.GetParameters().Length).FirstOrDefault();
            //keySelectorBody = Expression.MakeMemberAccess(expression: keySelectorBody, member: typeof(DateTime).GetProperty("Ticks"));

            if (functionInfo == null) throw new BermudaExpressionGenerationException("Function " + currentDimension.Function + "(" + string.Join(",", Enumerable.Range(0, currentDimension.Arguments.Count).Select(n => "arg" + n)) + ") not supported.");

            var functionArguments = new List<Expression>();

            var methodParams = functionInfo.GetParameters();

            if (currentDimension.Arguments.Count != methodParams.Length) throw new BermudaExpressionGenerationException("Number of arguments does not match the number of parameters for function " + currentDimension.Function);

            for (int i = 0; i < methodParams.Length; i++)
            {
                var actualArg = currentDimension.Arguments[i];
                var parameterType = methodParams[i].ParameterType;

                if (actualArg.IsFunctionCall)
                {
                    Expression functionCall = MakeFunctionCallExpression(actualArg, itemType, keySelectorParameter);
                    
                    if (functionCall.Type != parameterType) 
                        functionCall = Expression.Convert(functionCall, parameterType);
                    functionArguments.Add(functionCall);
                }
                else if (actualArg.Source != null)
                {
                    if (actualArg.IsQuoted)
                    {
                        functionArguments.Add(Expression.Constant(actualArg.Source));
                        continue;
                    }

                    //see if the provided string is a column name
                    var targetMember = GetMember(itemType, actualArg.Source, false);

                    if (targetMember != null)
                    {
                        keySelectorBody = Expression.MakeMemberAccess(expression: keySelectorParameter, member: targetMember);
                        functionArguments.Add(keySelectorBody);
                    }
                    else
                    {
                        var actualValue = parameterType == typeof(string) ? actualArg.Source : Convert.ChangeType(actualArg.Source, parameterType);
                        functionArguments.Add(Expression.Constant(actualValue));
                    }
                }
                else if (actualArg.Child != null) 
                {
                    var childExpr = actualArg.Child.CreateExpression(null);

                    if (childExpr.Type != parameterType)
                        childExpr = Expression.Convert(childExpr, parameterType);

                    functionArguments.Add(childExpr);
                }
            }

            keySelectorBody = Expression.Call
            (
                method: functionInfo,
                arguments: functionArguments
            );

            if (currentDimension.IsNegated) 
                keySelectorBody = Expression.Negate(keySelectorBody);
            if (currentDimension.IsNotted) 
                keySelectorBody = Expression.Not(keySelectorBody);

            return keySelectorBody;
        }

        private static Expression MakeConvertExpression(DimensionExpression currentDimension, Type itemType, ParameterExpression keySelectorParameter, string targetType = null)
        {
            if (currentDimension.Arguments.Count != 2 && targetType == null) throw new BermudaExpressionGenerationException("Convert function requires 2 arguments");
            if (currentDimension.Arguments.Count != 1 && targetType != null) throw new BermudaExpressionGenerationException("Cast function requires 1 argument");

            string lowerType;

            if (targetType == null)
            {
                targetType = currentDimension.Arguments[1].ToString();
                lowerType = targetType.ToLower();
            }
            else
            {
                lowerType = targetType.ToLower();
            }

            var firstArg = currentDimension.Arguments.FirstOrDefault();

            Expression firstArgExpr = MakeDimensionExpression(itemType, keySelectorParameter, firstArg);
            Type conversionTargetType = null;

            switch (lowerType)
            {
                case "sql_bigint": conversionTargetType = typeof(long); break;
                case "integer": conversionTargetType = typeof(int); break;
                case "date": conversionTargetType = typeof(DateTime); break;
                default: throw new BermudaExpressionGenerationException("Unknown target type: " + targetType); 
            }

            if (conversionTargetType == firstArgExpr.Type) 
                return firstArgExpr;

            var convexpr = Expression.Convert(firstArgExpr, conversionTargetType);

            return convexpr;
            //throw new BermudaExpressionGenerationException("Invalid cast arguments: " + firstArg + " to " + targetType);
        }

        private static Expression MakeDimensionExpression(Type itemType, ParameterExpression keySelectorParameter, DimensionExpression firstArg)
        {
            Expression firstArgExpr;
            bool dontNegate = false;

            if (firstArg.Child != null)
            {
                dontNegate = true;
                firstArgExpr = firstArg.CreateExpression(null);
            }
            else if (firstArg.Function != null)
            {
                firstArgExpr = MakeFunctionCallExpression(firstArg, itemType, keySelectorParameter);
            }
            else
            {
                var sourcePath = firstArg.Source;
                var member = GetMember(itemType, sourcePath, false);

                if (member == null)
                {
                    var stringVersion = firstArg.Source;

                    long num;
                    double num2;

                    if (long.TryParse(stringVersion, out num)) return Expression.Constant( num );
                    if (double.TryParse(stringVersion, out num2)) return Expression.Constant(num2);

                    firstArgExpr = Expression.Constant(stringVersion);
                }
                else
                {
                    firstArgExpr = Expression.MakeMemberAccess(keySelectorParameter, member);
                }
            }

            if (!dontNegate)
            {
                if (firstArg.IsNegated)
                {
                    firstArgExpr = Expression.Negate(firstArgExpr);
                }
                else if (firstArg.IsNotted)
                {
                    firstArgExpr = Expression.Not(firstArgExpr);
                }
            }

            return firstArgExpr;
        }

    }
}
