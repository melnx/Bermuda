using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using Bermuda.Interface;
using Bermuda.ExpressionGeneration;

namespace Bermuda.ExpressionGeneration
{
    public partial class ReduceExpressionGeneration
    {
        static ConcurrentDictionary<string, Expression> expressionCache = new ConcurrentDictionary<string,Expression>();
        static ConcurrentDictionary<string, object> funcCache = new ConcurrentDictionary<string,object>();
        static string[] OneToManyGroupByTypes = null;


        static readonly Type PLinqExtensionClassType = typeof(ParallelEnumerable);
        static readonly Type PLinqEnumType = typeof(ParallelQuery<>);
        static readonly Type LinqExtensionClassType = typeof(Enumerable);
        static readonly Type LinqEnumType = typeof(IEnumerable<>);
        static readonly string CountTargetPath = "_Count";
        static readonly string CountAggregateString = "Count";


        public static FieldInfo GetField(Type elementType, string name, bool throwError = true, bool inferPlurals = true)
        {
            if (name == null) return null;

            var fields = elementType.GetFields();
            string lower = name.ToLower();
            foreach (var f in fields)
            {
                var lowerName = f.Name.ToLower();

                if (lowerName == lower || (inferPlurals && lower + "s" == lowerName)) return f;
            }

            if (throwError) throw new BermudaExpressionGenerationException("Unknown field " + name + " for type " + elementType.Name);
            return null;
        }

        public static MemberInfo GetMember(Type elementType, string name, bool throwError = true, bool inferPlurals = true)
        {
            if (name == null) return null;

            var fields = elementType.GetMembers();
            string lower = name.ToLower();
            foreach (var f in fields)
            {
                var lowerName = f.Name.ToLower();

                if ( lowerName == lower || (inferPlurals && lower + "s" == lowerName)) return f;
            }

            if (throwError) throw new BermudaExpressionGenerationException("Unknown member " + name + " for type " + elementType.Name);
            return null;
        }


       
        //public static object GetFunc(Expression filter, string select, IEnumerable<ReduceDimension> groupBy, Type itemType, Type resultType)
        //{
        //    var hash = (filter==null?0:filter.ToString().GetHashCode()) + "|" + select + "|" + string.Join(",", groupBy.Select(x => x.GetChecksum()));
        //    object func;

        //    if (funcCache.TryGetValue(hash, out func))
        //    {
        //        return func;
        //    }

        //    var expr = GetMapreduceExpression(filter, select, groupBy, itemType, resultType);
        //    var compileMethod = expr.GetType().GetMethod("Compile");

        //    funcCache[hash] = func = compileMethod.Invoke( expr, new object[0]);

        //    return func;
        //}

        public static Expression GetWhereExpression(Expression filter, Type itemType)
        {
            return GetMapreduceExpression(filter, null, null, itemType, null, null, "sc");
        }

     
        public static bool IsCollectionType(Type type)
        {
            return type != typeof(string) && null != type.GetInterface("IEnumerable`1");
        }

        
        static string[] _aggregateFunctions = { "sum", "count", "max", "min", "average" };
        public static bool IsAggregateFunction(string p)
        {
            return p != null && _aggregateFunctions.Contains(p.ToLower());
        }
        
        private static Type GetTargetTypeForGroupByDimension(Type itemType, DimensionExpression g)
        {
            Type memberType = null;

            if (g.Source != null)
            {
                var member = GetMember(itemType, g.Source);
                memberType = GetMemberType(member);
                if (memberType == typeof(DateTime)) memberType = typeof(long);

                if (IsCollectionType(memberType))
                    memberType = GetTypeOfEnumerable(memberType);
            }
            else if (g.IsFunctionCall)
            {
                var methodInfo = typeof(UtilityExtensions).GetMethods().Where(x => string.Equals(x.Name, g.Function, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                memberType = methodInfo.ReturnType;
            }
            else if (g.Child != null)
            {
                var childExpr = g.Child.CreateExpression(null);
                memberType = childExpr.Type;
            }
            else
            {
                throw new Exception("Not able to infer the return type of " + g);
            }
            return memberType;
        }

        private static MethodInfo GetFunctionInfo(string name)
        {
            return typeof(UtilityExtensions).GetMethods().Where(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        private static string GetTargetPath(DimensionExpression g)
        {
            if ( string.IsNullOrWhiteSpace(g.Target))
            {
                if (g.Child != null)
                {
                    var childAsSelector = g.Child as SelectorExpression;

                    if (childAsSelector != null)
                    {
                        var childTargePath = GetTargetPath(childAsSelector);
                        return childTargePath;
                    }
                    else
                    {
                        throw new Exception("Child does not provide an alias");
                    }
                }
                else
                {
                    if (g.Source != null) return g.Source;
                    if (g.Function != null) return "_" + g.Function;
                }
            }
            
            return g.Target;
        }

        private static string GetTargetPath(SelectorExpression s)
        {
            if (string.IsNullOrWhiteSpace(s.Target))
            {
                var dimChild = s.Child as DimensionExpression;
                if (dimChild != null)
                {
                    var result = GetTargetPath(dimChild);
                    return result;
                }
            }

            return s.Target;
        }

        public static AttributeType GetAttribute<AttributeType>(Enum value) where AttributeType : Attribute
        {
            var field = value.GetType().GetField(value.ToString());

            var attribute = (AttributeType)Attribute.GetCustomAttribute(field, typeof(AttributeType));

            return attribute;
        }

        public static AttributeType GetAttribute<AttributeType>(Type type) where AttributeType : Attribute
        {
            return Attribute.GetCustomAttribute(type, typeof(AttributeType)) as AttributeType;
        }

        static Expression MakeMapreduceExpression(Expression filter, DimensionExpression[] selects, DimensionExpression[] groupBy, Type itemType, Type resultType, GetExpression get, Expression source, string paramname)
        {
            var elementType = itemType;

            //if (OneToManyGroupByTypes == null) OneToManyGroupByTypes = typeof(GroupByTypes).GetEnumValues().OfType<GroupByTypes>().Where(x => GetAttribute<OneToManyAttribute>(x) != null).ToArray();

            var collectionParameter = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), paramname);

            //var associatedTypes = GetAttribute<AssociatedTypesAttribute>(elementType);
            //if (associatedTypes == null) throw new Exception("Unknown associated types, please declare an AssociatedTypes attribute for " + elementType.Name);

            //Expression lambdaBody = MakeSelectExpression(filter, groupBy, interval, select, collectionParameter, elementType, associatedTypes.MetadataType, associatedTypes.GroupingType, typeof(TOut));

            var metadataType = typeof(ItemMetadata<>).MakeGenericType(itemType);

            Expression lambdaBody = MakeSelectExpressionEx(filter, groupBy, selects, source ?? collectionParameter, elementType, metadataType, null, resultType);

            if (get != null && get.Take.HasValue)
            {
                lambdaBody = AppendTake(typeof(Enumerable), resultType, lambdaBody, get.Take.Value);
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(elementType), typeof(IEnumerable<>).MakeGenericType(resultType));

            var lambda = Expression.Lambda
            (
                delegateType: delegateType, 
                parameters: collectionParameter,
                body: lambdaBody
            );

            return lambda;
        }

     

        public static bool TypeImplements(Type type, Type genericType)
        {
            if( type == null || type == typeof(string) ) return false;
            return type.GetInterfaces().Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == genericType);
        }

        public static Type GetMemberType(MemberInfo member)
        {
            var asProperty = member as PropertyInfo;
            var asField = member as FieldInfo;

            if (asProperty != null) return asProperty.PropertyType;
            else if (asField != null) return asField.FieldType;

            throw new Exception("Not a field or property");
        }

        private static bool IsCountRequiredForAggregate(string p)
        {
            if (p == null) return false;
            var lower = p.ToLower();
            return lower == "average" || p == "count";
        }

        //private static Expression ParseInterval(string p)
        //{
        //    long n;

        //    switch (p)
        //    {
        //        case "Second": n = TimeSpan.FromSeconds(1).Ticks; break;
        //        case "Minute": n = TimeSpan.FromMinutes(1).Ticks; break;
        //        case "QuarterHour": n = TimeSpan.FromMinutes(15).Ticks; break;
        //        case "Hour": n = TimeSpan.FromHours(1).Ticks; break;
        //        case "Day": n = TimeSpan.FromDays(1).Ticks; break;
        //        case "Week": n = TimeSpan.FromDays(7).Ticks; break;
        //        case "Month": n = TimeSpan.FromDays(30).Ticks; break;
        //        case "Quarter": n = TimeSpan.FromDays(1).Ticks; break;
        //        case "Year": n = TimeSpan.FromDays(1).Ticks; break;
        //        default: n = int.Parse(p); break;
        //    }

        //    return Expression.Constant(n);
        //}

        private static string GetTargetPath(Type keyType)
        {
            if (IsTupleType(keyType))
            {
                return GetTargetPath( keyType.GetGenericArguments().Last() );
            }

            string groupByTargetPath;
            if (keyType == typeof(long)) groupByTargetPath = "Id";
            else if (keyType == typeof(string)) groupByTargetPath = "Text";
            else throw new Exception("Only supported grouping by longs and strings");
            return groupByTargetPath;
        }

        private static string MakeNumericName(string str, int n)
        {
            return n == 0 ? str : str + (n + 1);
        }
       
        /*
        private static Expression MakeSelectExpression(Expression filter, IEnumerable<ReduceDimension> groupBy, IntervalTypes interval, SelectDescriptor select, ParameterExpression collectionParameter, Type itemType, Type metadataType, Type groupingType, Type resultType)
        {
            int metadataDepth = 0;

            var elementEnum = MakeWhereExpression(filter, collectionParameter, groupBy, itemType);

            var elementEnumExpression = MakeGroupByExpression(groupBy, interval, elementEnum, ref metadataDepth, itemType, metadataType, groupingType);

            var enumType = GetTypeOfEnumerable(elementEnumExpression.Type);
            //var elementType = GetTypeOfEnumerable(enumType);
            //var keyType = GetGroupingKeyType(enumType);

            var genericSelectInfo = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name == "Select" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var selectInfo = genericSelectInfo.MakeGenericMethod(enumType, resultType);

            var mentionEnumParameter = Expression.Parameter( type: enumType, name: "g" );

            var selectMemberBindings = new List<MemberBinding>();

            selectMemberBindings.Add(GetFieldBindingForValueSelect(mentionEnumParameter, new SelectDescriptor { Aggregate = CountAggregateString, TargetPath = "Count" }, null, metadataDepth, resultType, null));

            if (select.Aggregate != "Count") selectMemberBindings.Add(GetFieldBindingForValueSelect(mentionEnumParameter, select, null, metadataDepth, resultType, null));
            //else selectMemberBindings.Add( Expression.Bind(member: resultType.GetField("IsCount"), expression:Expression.Constant(true)));

            for (int i = 0; i < groupBy.Count(); i++) AddSelectMemberBinding(groupBy, i, selectMemberBindings, mentionEnumParameter, groupingType, resultType);

            var delegateType = typeof(Func<,>).MakeGenericType(mentionEnumParameter.Type, resultType);

            var callExpression = Expression.Call
            ( 
                method: selectInfo, 
                arguments: new Expression[]
                {
                    elementEnumExpression,
                    Expression.Lambda
                    ( 
                        delegateType: delegateType, 
                        parameters: mentionEnumParameter,
                        body: Expression.MemberInit
                        (
                            newExpression: Expression.New(type: resultType),
                            bindings: selectMemberBindings
                        )
                    )
                }
            );

            return callExpression;
        }*/

        public static Expression ChainAndExpressionCollection(IEnumerable<Expression> conditions)
        {
            var count = conditions.Count();
            if (count == 1) return conditions.FirstOrDefault();
            else if( count >= 2 ) return Expression.AndAlso( conditions.First(), ChainAndExpressionCollection(conditions.Skip(1)) );
            else return Expression.Constant(true);
        }

        public static Expression ChainOrExpressionCollection(IEnumerable<Expression> conditions)
        {
            var count = conditions.Count();
            if (count == 1) return conditions.FirstOrDefault();
            else if (count >= 2) return Expression.OrElse(conditions.First(), ChainOrExpressionCollection(conditions.Skip(1)));
            else return Expression.Constant(true);
        }

        private static Type GetGroupingKeyType(Type type)
        {
            return type.GetGenericArguments()[0];
        }

        private static void AddSelectMemberBinding(DimensionExpression[] groupBy, int groupByIndex, List<MemberBinding> selectMemberBindings, ParameterExpression groupParameter, Type groupingType, Type resultType)
        {
            string targetPath = "Id" + (groupByIndex > 0 ? (groupByIndex + 1).ToString() : null);
            string sourcePath = targetPath;
            var activeGroupBy = groupBy.Skip(groupByIndex).First();
            if (activeGroupBy.Source == null) return;

            var groupKeyType = GetGroupingKeyType(groupParameter.Type);

            if (groupKeyType == groupingType)
            {
                selectMemberBindings.Add(Expression.Bind
                (
                    member: GetField(resultType, targetPath),
                    expression: Expression.MakeMemberAccess
                    (
                        expression: Expression.MakeMemberAccess( expression: groupParameter, member: groupParameter.Type.GetProperty("Key")),
                        member: groupKeyType.GetField(sourcePath)
                    )
                ));   
            }
            else
            {
                selectMemberBindings.Add(Expression.Bind
                (
                    member: GetField(resultType, targetPath),
                    expression: Expression.MakeMemberAccess( expression: groupParameter, member: groupParameter.Type.GetProperty("Key"))
                ));
            }
        }

        private static MemberAssignment MakeAggregateFunctionCallExpression(Expression enumParameter, DimensionExpression select, Expression sourceExpression, int metadataDepth, Type resultType, string targetPathOverride)
        {
            var elementType = GetTypeOfEnumerable(enumParameter.Type);

            if( select.Function == "Count" )
            {
                var targetPath = targetPathOverride ?? GetTargetPath(select);

                var genericCountInfo = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name == "LongCount" && x.IsGenericMethod && x.GetParameters().Length == 1);
                var countInfo = genericCountInfo.MakeGenericMethod(elementType);

                var memberInfo = GetMember(resultType, targetPath);

                if( memberInfo == null ) throw new Exception("Unknown target path for " + resultType + ": " + targetPath);

                var actualExpression = sourceExpression ?? Expression.Call(method: countInfo, arguments: enumParameter);
                var targetType = GetMemberType(memberInfo);

                return Expression.Bind
                (
                    member: memberInfo,
                    expression: actualExpression.Type != targetType ? Expression.Convert( type: targetType, expression: actualExpression) : actualExpression
                );  
            }
            else
            {
                var targetPath = targetPathOverride ?? GetTargetPath(select);

                if (targetPath == null) throw new Exception("Both target and source path required for aggregate: " + select.Function);

                var mentionParameter = Expression.Parameter(type: elementType, name: GenerateParamName(elementType, metadataDepth));

                var firstArgument = select.Arguments.FirstOrDefault();

                //if (firstArgument.Function == null)
                //{

                Expression memberAccessBody = null;

                if (firstArgument.Function != null)
                {
                    var targetFieldInfo0 = GetField(resultType, targetPath);

                    var functionCall = MakeFunctionCallExpression(firstArgument, elementType, mentionParameter);

                    memberAccessBody = functionCall;
                }
                else
                {
                    string fieldName = firstArgument.Source;
                    memberAccessBody = GetMemberAccessRecursively(mentionParameter, fieldName, select.SourceType, metadataDepth, metadataDepth);
                }

                var targetFieldInfo = GetField(resultType, targetPath);

                var targetType = targetFieldInfo.FieldType;

                var averageInfo = GetAggregateFunction(select.Function, elementType, memberAccessBody.Type);

                if (averageInfo == null) return null;

                Expression functionCallBody = null;

                var averageLambda = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType(elementType, memberAccessBody.Type),
                    body: functionCallBody ?? memberAccessBody,
                    parameters: new ParameterExpression[] { mentionParameter }
                );

                var actualExpression = sourceExpression ?? Expression.Call(method: averageInfo, arguments: new Expression[] { enumParameter, averageLambda });

                var bindingres = Expression.Bind
                (
                    member: targetFieldInfo,
                    expression: actualExpression.Type != targetType ? Expression.Convert(type: targetType, expression: actualExpression) : actualExpression
                );

                return bindingres;
                
            }

            throw new Exception("Unknown Select");
        }

        private static MethodInfo GetAggregateFunction(string name, Type elementType, Type memberType = null, bool throwError = false)
        {
            var genericAggregateInfos = typeof(Enumerable).GetMethods().Where(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase) && x.IsGenericMethod && x.GetParameters().Length == 2);
            if (memberType != null) genericAggregateInfos = genericAggregateInfos.Where(x => x.GetParameters().Last().ParameterType.GetGenericArguments().Last() == memberType);

            var genericAggregateInfo = genericAggregateInfos.FirstOrDefault();

            if (throwError && genericAggregateInfo == null) throw new BermudaExpressionGenerationException("The function " + name + " could not be resolved for type " + memberType);

            if(genericAggregateInfo == null) return null;

            var averageInfo = genericAggregateInfo.MakeGenericMethod(elementType);
            return averageInfo;
        }

        public static Type GetTypeOfEnumerable(Type type)
        {
            //if (type.BaseType == typeof(Array)) return type.BaseType.GetElementType();
            if (type.IsArray && !type.IsGenericType)
            {
                var result = type.GetElementType();
                return result;
            }

            var genericType = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();
            var enumerableTypeIndex =
                genericType == typeof(IGrouping<,>) ? 1 :
                genericType == typeof(IOrderedEnumerable<>) ? 0 :
                genericType == typeof(IEnumerable<>) ? 0 :
                genericType == typeof(List<>) ? 0 :
                genericType == typeof(ParallelQuery<>) ? 0 :
                genericType == typeof(ConcurrentDictionary<,>) ? 1 :
                0;
            
            return genericArguments[enumerableTypeIndex];
        }

        /*
        private static Expression MakeGroupByExpression(IEnumerable<ReduceDimension> groupBy, IntervalTypes interval, Expression collectionParameter, ref int maxMetadataDepth, Type itemType, Type metadataType, Type groupingType)
        {
            if (groupBy.All(x => x.GroupBy == null))
            {
                var res = Expression.NewArrayInit(collectionParameter.Type, collectionParameter);
                return res;
            }

            var groupBySourceCollection = collectionParameter;

            var elementType = GetTypeOfEnumerable(groupBySourceCollection.Type);
            var elementParameter = Expression.Parameter(type: elementType, name: GenerateParamName(elementType));

            var groupByDepthMapping = new Dictionary<ReduceDimension, int>();

            int i = 0;
            foreach (var g in groupBy.Where(x => OneToManyGroupByTypes.Contains(x.GroupBy)))
            {
                MakeSelectManyExpression(g, ref groupBySourceCollection, ref elementParameter, ref maxMetadataDepth, i, itemType, metadataType);
                groupByDepthMapping[g] = 0;
                foreach (var k in groupByDepthMapping.Keys.ToArray()) groupByDepthMapping[k] = groupByDepthMapping[k] + 1;
                i++;
            }

            foreach (var g in groupBy.Where(x => !OneToManyGroupByTypes.Contains(x.GroupBy)))
            {
                groupByDepthMapping[g] = maxMetadataDepth;
                i++;
            }

            elementType = GetTypeOfEnumerable(groupBySourceCollection.Type);

            var groupByBody = GetGroupByBody(elementParameter, groupBy, maxMetadataDepth, groupByDepthMapping);

            var genericInfo = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name == "GroupBy" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var info = genericInfo.MakeGenericMethod( elementType, groupByBody.Type );
        
            var callExpression = Expression.Call
            ( 
                method:info, 
                arguments: new Expression[]
                {
                    groupBySourceCollection,
                    Expression.Lambda
                    ( 
                        delegateType: typeof(Func<,>).MakeGenericType(elementType, groupByBody.Type), 
                        parameters: elementParameter,
                        body: groupByBody
                    )
                } 
            );

            return callExpression;
        }*/

        private static string GenerateParamName(Type type, int? i = null)
        {
            return new string(type.Name.Where(x => x >= 'A' && x <= 'Z').ToArray()).ToLower() + i;
        }

        /*
        private static void MakeSelectManyExpression(ReduceDimension groupBy, ref Expression collectionParameter, ref ParameterExpression elementParameter, ref int metadataDepth, int i, Type TIn, Type metadataType)
        {
            if (!OneToManyGroupByTypes.Contains(groupBy.GroupBy)) return;

            var propertyPath = groupBy.ToString();

            var elementType = GetTypeOfEnumerable(collectionParameter.Type);

            var selectType = GetTypeOfEnumerable(TIn.GetField(propertyPath).FieldType);

            var genericInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "SelectMany" && x.IsGenericMethod && x.GetParameters().Length == 3);
            var genericInfo = genericInfos.Skip(1).FirstOrDefault();
            var info = genericInfo.MakeGenericMethod( elementType, selectType, metadataType);

            var collectionSelectorParameter = Expression.Parameter(type: elementType, name: GenerateParamName(elementType, i) );
            var resultSelectorParameters = new ParameterExpression[]
            {
                Expression.Parameter(type: elementType, name: GenerateParamName(elementType, i) ),
                Expression.Parameter(type: selectType, name: GenerateParamName(selectType, i) )
            };

            var memberAccess = GetMemberAccessRecursively(collectionSelectorParameter, propertyPath, metadataDepth, metadataDepth);

            var collectionSelector = Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(elementType, typeof(IEnumerable<>).MakeGenericType(selectType) ),
                parameters: collectionSelectorParameter,
                body: memberAccess
            );

            var bindings = new List<MemberBinding>();

            if (elementType == metadataType)
            {
                bindings.Add(Expression.Bind
                (
                    member: metadataType.GetField("Child"),
                    expression: resultSelectorParameters[0]
                ));
            }
            else if (elementType == TIn)
            {
                bindings.Add(Expression.Bind
                (
                    member: metadataType.GetField("Mention"),
                    expression: resultSelectorParameters[0]
                ));
            }

            bindings.Add(Expression.Bind
            (
                member: metadataType.GetField("Id"),
                expression: resultSelectorParameters[1]
            ));

            var resultSelector = Expression.Lambda
            (
                delegateType: typeof(Func<,,>).MakeGenericType(elementType, selectType, metadataType),
                parameters: resultSelectorParameters,
                body: Expression.MemberInit
                (
                    newExpression: Expression.New(type: metadataType),
                    bindings: bindings
                )
            );

            var call  = Expression.Call
            (
                method: info,
                arguments: new Expression[]
                {
                    collectionParameter,
                    collectionSelector,
                    resultSelector
                }
            );

            metadataDepth++;

            elementParameter = Expression.Parameter(type: metadataType, name: GenerateParamName(metadataType, i) );
            collectionParameter = call;
        }*/

        /*
        private static Expression GetGroupByBody(ParameterExpression elementParameter, IEnumerable<ReduceDimension> groupBy, int metadataDepth, Dictionary<ReduceDimension, int> groupByDepthMapping)
        {
            var count = groupBy.Count(x => x.GroupBy != null);

            if (count == 0)
            {
                return Expression.Constant(null);
            }
            else if (count == 1)
            {
                var activeGroupBy = groupBy.OrderByDescending(x => x.GroupBy).FirstOrDefault();
                return GetMemberAccessRecursively(elementParameter, activeGroupBy, groupByDepthMapping[activeGroupBy]);
            }
            else
            {
                var targetPropertyMapping = new Dictionary<GroupByTypes, string>();

                return Expression.MemberInit
                (
                    newExpression: Expression.New(type: typeof(MentionGroup)),
                    bindings: groupBy.Select
                    (
                        (x, i) => Expression.Bind
                        (
                            member: typeof(MentionGroup).GetField( i == 0 ? "Id" : "Id" + (i + 1) ),
                            expression: GetMemberAccessRecursively(elementParameter, x, groupByDepthMapping[x])
                        )
                    )
                );
            }

            throw new Exception("Unhandled group by parameters");
        }*/

        #region figure out different types
        /*
        private static Type GetSelectDelegateType(GroupByTypes[] groupBy, Type TIn, Type TOut, Type TKey)
        {
            var count = groupBy.Count(x => x != GroupByTypes.None);

            if (count == 0)
            {
                return typeof(Func<,>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(TIn), TOut);
            }
            else
            {
                return typeof(Func<,>).MakeGenericType(typeof(IGrouping<,>).MakeGenericType(TKey, TIn), TOut);
            }
        }

        private static Type GetGroupingType(GroupByTypes[] groupBy, Type TIn, Type TGrouping)
        {
            var count = groupBy.Count(x => x != GroupByTypes.None );

            if (count == 0)
            {
                return typeof(IEnumerable<>).MakeGenericType(TIn);
            }
            else if (count == 1)
            {
                return typeof(IGrouping<,>).MakeGenericType(typeof(long), TIn);
            }
            else
            {
                return typeof(IGrouping<,>).MakeGenericType(TGrouping, TIn);
            }
        }
         */
        #endregion

        //private static Expression GetMemberAccessRecursively(ParameterExpression parameter, GroupByDescriptor groupBy, int metadataDepth)
        //{
        //    string groupByPath = groupBy.GroupBy + groupBy.Interval;  //GetGroupByPath( groupBy.GroupBy, groupBy.Interval);
        //    bool fieldBelongsToMetadata = OneToManyGroupByTypes.Contains(groupBy.GroupBy);

        //    if (fieldBelongsToMetadata) metadataDepth -= 1;

        //    return GetMemberAccessRecursively(parameter, groupByPath, metadataDepth, metadataDepth, fieldBelongsToMetadata);
        //}

        private static Expression GetMemberAccessRecursively(Expression parameter, string path, Type literalType, int remdepth, int maxdepth, bool fieldBelongsToMetadata = false)
        {
            if (remdepth == -1) return parameter;

            var expr = GetMemberAccessRecursively(parameter, path, literalType, remdepth - 1, maxdepth, fieldBelongsToMetadata);

            //var path = remdepth == maxdepth ? propertyPath : (remdepth == maxdepth - 1 && !fieldBelongsToMetadata) ? "Mention" : "Child";

            var memberInfo = GetMember(expr.Type, path, false);

            if (memberInfo != null)
            {
                var memberAccessExpression = Expression.MakeMemberAccess
                (
                    expression: expr,
                    member: memberInfo
                );

                return memberAccessExpression;
            }
            else
            {
                var convertedValue = Convert.ChangeType(path, literalType);
                var constantExpression = Expression.Constant(convertedValue);

                return constantExpression;
            }
        }

        private static MemberExpression GetMemberAccess(ParameterExpression parameter, string propertyPath)
        {
            var groupByBody = Expression.MakeMemberAccess
            (
                expression: parameter,
                member: GetMember(parameter.Type, propertyPath)
            );
            return groupByBody;
        }

        //private static string GetGroupByPath(GroupByTypes actualGroupBy, IntervalTypes? interval)
        //{
        //    if (GetAttribute<UseIntervalAttribute>(actualGroupBy) != null) return GetDatePathByInterval(interval.Value);
        //    if (OneToManyGroupByTypes.Contains(actualGroupBy)) return "Id";
        //    else return actualGroupBy.ToString();
        //}

        private static string GetDatePathByInterval(IntervalTypes interval)
        {
            return "OccurredOn" + interval + "Ticks";
        }

    }

    public class ParameterExpressionWrapper
    {
        public ParameterExpression Expr;
        public SelectTypes? Order;
    }

    public class BermudaExpressionGenerationException : Exception
    {
        public BermudaExpressionGenerationException(string message) : base(message) { }
    }
}
