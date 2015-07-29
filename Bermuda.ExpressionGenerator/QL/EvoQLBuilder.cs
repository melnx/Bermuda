using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using Bermuda.ExpressionGeneration;

namespace Bermuda.ExpressionGeneration
{
    public class EvoQLBuilder
    {
        //public static Expression GetFilterLambda(string query, Type itemType)
        //{
        //    EvoQLBuilder builder = new EvoQLBuilder(query);

        //    ParameterExpression parameterExpression;
        //    EvoQLExpression parse;
            
        //    return builder.GetFilterLambda(out parse, out parameterExpression);
        //}

        //public static Expression GetFilterLambdaWithParameters(string query, Type itemType)
        //{
        //    EvoQLBuilder builder = new EvoQLBuilder(query);

        //    ParameterExpression parameterExpression;
        //    EvoQLExpression parse;

        //    return builder.GetFilterLambdaWithParameters(out parse, out parameterExpression);
        //}

        public static Expression GetWhereExpression(string query, Type itemType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;

            var filterexpr = builder.CreateFilterExpression(out parse, out parameterExpression, itemType);

            var queryexpr = ReduceExpressionGeneration.GetWhereExpression(filterexpr, itemType);

            return queryexpr;
            //return new ExpressionResult { Expression = queryexpr, Collections = GetCollections(parse) }; 
        }

        public CollectionExpression[] GetCollections()
        {
            var expr = ParseQuery();

            var get = expr.Tree as GetExpression;

            if (get == null) return null;

            if (get._collections == null) return null;

            return get._collections.ToArray();
        }
        

        public static Expression GetReduceExpression(string query, Type itemType, bool includeFilter = false)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;

            var filterexpr = builder.CreateFilterExpression(out parse, out parameterExpression, itemType);

            var get = parse.Tree as GetExpression;

            var targetType = itemType;

            var currentGet = get;

            var sourceExpr = includeFilter ? filterexpr : null;

            var lulz = RecursivelyGetSubselects(get, filterexpr, includeFilter, itemType);

            //Expression reduceexpr = ReduceExpressionGeneration.GetMapreduceExpression( sourceExpr, get._selects.ToArray(), get._dimensions.Any() ? get._dimensions.ToArray() : null, targetType, get, null);

            return lulz;
            //return new ExpressionResult{ Expression = reduceexpr, Collections = GetCollections(parse) };
        }

        static Expression RecursivelyGetSubselects(GetExpression get, Expression filterexpr, bool includeFilter, Type itemType, int depth = 0)
        {
            if (get.Subselect == null)
            {
                var sourceExpr = includeFilter ? filterexpr : null;
                Expression reduceexpr = ReduceExpressionGeneration.GetMapreduceExpression(sourceExpr, get._selects.ToArray(), get._dimensions.Any() ? get._dimensions.ToArray() : null, itemType, get, null, "sc" + depth);

                return reduceexpr;
            }
            else
            {
                var sub = RecursivelyGetSubselects(get.Subselect, filterexpr, includeFilter, itemType, depth+1);

                var lambda = sub as LambdaExpression;

                var body = lambda.Body;
                var param = lambda.Parameters.FirstOrDefault();

                var resultType = body.Type;

                var targetType = ReduceExpressionGeneration.GetTypeOfEnumerable(resultType);

                Expression reduceexpr = ReduceExpressionGeneration.GetMapreduceExpression(null, get._selects.ToArray(), get._dimensions.Any() ? get._dimensions.ToArray() : null, targetType, get, body, "sc" + depth);

                //reduceexpr = ParameterRebinder.ReplaceParameters(reduceexpr, "sc", param);

                //return reduceexpr;

                var reducebody = (reduceexpr as LambdaExpression).Body;

                var newlambda = Expression.Lambda(reducebody, param);

                return newlambda;
            }
        }

        public static Expression GetMergeExpression(string query, Type itemType, Type targetType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;

            var filterexpr = builder.CreateFilterExpression(out parse, out parameterExpression, itemType);

            var get = parse.Tree as GetExpression;

            //var selects = get._selects.Select(x => new SelectDescriptor { SourcePath = x.Source, Function = x.Function, TargetPath = x.Target } ).ToArray();

            Type resultType = targetType;

            if (resultType == null)
            {
                if (itemType == null) throw new Exception("Neither Target or Source Item Type specified");
                var mapreduceexpr = GetReduceExpression(query, itemType);
                //var mapreduceexpr = res.Expression;
                var enumType = mapreduceexpr.GetType().GetProperty("ReturnType").GetValue(mapreduceexpr, null) as Type;
                resultType = ReduceExpressionGeneration.GetTypeOfEnumerable(enumType);
            }

            //var resultElementType = ReduceExpressionGeneration.GetTypeOfEnumerable(resultType);
            //Expression mergeExpr = ReduceExpressionGeneration.GetMergeExpression(select, resultElementType);

            var mergeexpr = ReduceExpressionGeneration.GetMergeInvocationExpression( get._selects.ToArray(), get._dimensions.ToArray(), resultType);

            return mergeexpr;
            //return new ExpressionResult { Expression = mergeexpr, Collections = GetCollections(parse) };
        }

        private static EnumType TryParseEnum<EnumType>(string str, EnumType fallback) where EnumType : struct
        {
            EnumType res;
            if (str != null)
            {
                if (!Enum.TryParse(str, out res)) throw new Exception("Invalid " + typeof(EnumType).Name + ": " + str);
            }
            else
            {
                return fallback;
            }

            return res;
        }


        private Expression CreateFilterExpression(out EvoQLExpression parse, out ParameterExpression parameter, Type elementType)
        {
            parse = ParseQuery();

            var root = parse.Tree;

            if (root is GetExpression)
            {
                var get = (GetExpression)root;

                get.ElementType = elementType;

                var expression = get.CreateExpression(null);

                parameter = Expression.Parameter(elementType, "x");

                if (expression == null) return null;
                return ParameterRebinder.ReplaceParameters(expression, "x", parameter);
            }

            parameter = null;
            return null;
        }

        private EvoQLExpression ParseQuery()
        {
            var parse = new EvoQLExpression(_query);

            if (parse.HadErrors)
            {
                string errorString = "EvoQL Error For: ";
                errorString += _query;
                errorString += "\n";
                foreach (string error in parse.Errors)
                {
                    errorString += error + "\n";
                }
                throw new EvoQLException(errorString);
            }

            parse.Tree.Init();
            return parse;
        }

        //private Expression GetFilterLambda(out EvoQLExpression parse, out ParameterExpression parameter)
        //{
        //    var expression = GetFilterExpression(out parse, out parameter);

        //    if (expression == null) return null;

        //    var param = Expression.Parameter(typeof(Mention), "x");
        //    Expression<Func<Mention, bool>> lambda = Expression.Lambda<Func<Mention, bool>>(ParameterRebinder.ReplaceParameters(expression, "x", param), param);

        //    return lambda;
        //}

        //private Expression<Func<Mention, object[], bool>> GetFilterLambdaWithParameters(out EvoQLExpression parse, out ParameterExpression parameter)
        //{
        //    var expression = GetFilterExpression(out parse, out parameter);

        //    if (expression == null) return null;

        //    var param = Expression.Parameter(typeof(Mention), "x");
        //    var param2 = Expression.Parameter(typeof(object[]), "o");
        //    Expression<Func<Mention, object[], bool>> lambda = Expression.Lambda<Func<Mention, object[], bool>>( ParameterRebinder.ReplaceParameters(ParameterRebinder.ReplaceParameters(expression, "x", param),"o", param2), param, param2 );

        //    return lambda;
        //}

        string _query;
        
        public EvoQLBuilder(string query)
        {
            _query = query;
        }

        //private RootExpression _root;
        //private Expression _expression;
        //private EvoQLExpression _parse;
        //private Expression<Func<Mention, bool>> _lambda;

        public static Expression GetPagingExpression(string query, Type itemType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            var filterexpr = builder.GetPagingExpression(itemType);

            return filterexpr;
        }

        public Expression GetPagingExpression(Type itemType)
        {
            EvoQLExpression parse = ParseQuery();

            if (parse.Tree is GetExpression)
            {
                GetExpression get = (GetExpression)parse.Tree;

                var res = ReduceExpressionGeneration.GetPagingExpression(get, itemType);

                return res;
                //return new ExpressionResult { Expression = res, Collections = GetCollections(parse) };
            }

            return null;
        }

        /*
        public IEnumerable<Mention> Filter(IEnumerable<Mention> mentions)
        {
            ParameterExpression parameterExpression;
            EvoQLExpression parse;
            
            var whereLambdaExpr = GetFilterLambda(out parse, out parameterExpression);
            var whereLambda = whereLambdaExpr.GetType().GetMethod("Compile").Invoke(whereLambdaExpr, new object[0]);

            IEnumerable<Mention> result = mentions.Where(whereLambda);

            if (parse.Tree is GetExpression)
            {
                GetExpression get = (GetExpression)parse.Tree;

                if (get.Ordering != null)
                {
                    string[] properties = get.Ordering.Split('.');

                    string methodName = get.OrderDescending ? "OrderByDescending" : "OrderBy";

                    Type type = typeof(Mention);
                    ParameterExpression parameter = parameterExpression;
                    Expression currentExpression = parameter;

                    foreach (string current in properties)
                    {
                        PropertyInfo property = type.GetProperties().First(x => x.Name.Equals(current, StringComparison.InvariantCultureIgnoreCase));
                        currentExpression = Expression.Property(currentExpression, property);
                        type = property.PropertyType;
                    }

                    Type delegateType = typeof(Func<,>).MakeGenericType(typeof(Mention), type);
                    LambdaExpression lambda = Expression.Lambda(delegateType, currentExpression, parameter);

                    result = (IEnumerable<Mention>)typeof(Enumerable).GetMethods().Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2).MakeGenericMethod(typeof(Mention), type).Invoke(null, new object[] { result, lambda });
                }
                if (get.Skip != null)
                {
                    int skip = get.Skip.Value;

                    if (skip < 0)
                    {
                        throw new ArgumentException();
                    }

                    result = AttachMethod(result, "Skip", skip, parameterExpression);
                }
                if (get.Take != null)
                {
                    int take = get.Take.Value;

                    if (take < 0)
                    {
                        throw new ArgumentException();
                    }

                    result = AttachMethod(result, "Take", take, parameterExpression);
                }

            }

            return result;
        }*/

        /*
        private IEnumerable<Mention> AttachMethod(IEnumerable<Mention> source, string methodName, object parameter, ParameterExpression parameterExpression)
        {
            Type elementType = typeof(Mention);

            source = (IEnumerable<Mention>)typeof(Enumerable).GetMethods().Single(method => method.Name == methodName && method.GetParameters().Length == 2 && method.GetParameters().Skip(1).Single().ParameterType == parameter.GetType()).MakeGenericMethod(elementType).Invoke(null, new object[] { source, parameter });

            return source;
        }*/


        //private IEnumerable<Mention> _mentions = new Mention[]{};
        //private Expression<Func<Mention, bool>> _lambda;
        
        //public IEnumerable<Mention> BuildExpression(out EvoQLExpression expression, out ParameterExpression parameter)
        //{
        //    expression = new EvoQLExpression(_query);

        //    if (expression.HadErrors)
        //    {
        //        string errorString = "EvoQL Error:\n";
        //        foreach (string error in expression.Errors)
        //        {
        //            errorString += error + "\n";
        //        }
        //        throw new EvoQLException(errorString);
        //    }

        //    expression.Tree.Init();

        //    _root = expression.Tree;
          
        //    if (expression.Tree is GetExpression)
        //    {
        //        GetExpression get = (GetExpression)expression.Tree;

        //        _expression = get.CreateExpression(null);

        //        var lambda = BuildLambda<Mention>(out parameter);
        //        var func = lambda.Compile();

             
        //    }
        //    parameter = null;

        //    return null;
        //}

        public static CollectionExpression[] GetCollections(string query, string mapreduce, string merge, string paging)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);
            EvoQLBuilder builder2 = new EvoQLBuilder(mapreduce);
            EvoQLBuilder builder3 = new EvoQLBuilder(merge);
            EvoQLBuilder builder4 = new EvoQLBuilder(paging);

            return builder.GetCollections().Union(builder2.GetCollections()).Union(builder3.GetCollections()).Union(builder4.GetCollections()).Distinct().ToArray();
        }
    }


    public class ExpressionResult
    {
        public Expression Expression;
        public String[] Collections;
    }
}
