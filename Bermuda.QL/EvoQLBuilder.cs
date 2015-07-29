using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.QL;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using Bermuda.Entities;
using Bermuda.Entities.ExpressionGeneration;

namespace Bermuda.DomainLayer
{
    public class EvoQLBuilder
    {
        public static Expression GetFilterLambda(string query, Type itemType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;
            
            return builder.GetFilterLambda(out parse, out parameterExpression);
        }

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

            var filterexpr = builder.GetFilterExpression(out parse, out parameterExpression);

            var queryexpr = ReduceExpressionGeneration.GetMapreduceExpression(filterexpr, null, null, itemType);

            return queryexpr; 
        }

        public static Expression GetReduceExpression(string query, Type itemType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;

            var filterexpr = builder.GetFilterExpression(out parse, out parameterExpression);

            var get = parse.Tree as GetExpression;

            //var interval = get.Interval == null ? null : new IntervalTypes?( TryParseEnum(get.Interval, IntervalTypes.None) );
            //var select = TryParseEnum(get.Select, SelectTypes.Count);

            var groupBySortDirection = get.GroupByDescending.HasValue ? (get.GroupByDescending == true ? new OrderTypes?(OrderTypes.Descending): new OrderTypes?(OrderTypes.Ascending)) : null;
            var groupOverSortDirection = get.GroupOverDescending.HasValue ? (get.GroupOverDescending == true ? new OrderTypes?(OrderTypes.Descending) : new OrderTypes?(OrderTypes.Ascending)) : null;

            var dimensions = new ReduceDimension[]
            {
                new ReduceDimension { GroupBy = get.GroupBy, OrderBy = new SelectDescriptor{ SourcePath = get.GroupByOrderBy }, Order = groupBySortDirection, Take = get.GroupByTake, Interval = get.GroupByInterval },
                new ReduceDimension { GroupBy = get.GroupOver, OrderBy = new SelectDescriptor{ SourcePath = get.GroupOverOrderBy } , Order = groupOverSortDirection, Take = get.GroupOverTake, Interval = get.GroupOverInterval }
            };

            //foreach (var d in dimensions) if (d.GroupBy == GroupByTypes.Date) d.Interval = interval;

            var aggregate = get.Select == "Count" ? "Count" : "Sum";
            var sourcePath = get.Select == "Count" ? null : get.Select;

            var reduceexpr = ReduceExpressionGeneration.GetMapreduceExpression(filterexpr, new SelectDescriptor { TargetPath = get.Select, SourcePath = sourcePath, Aggregate = aggregate }, dimensions, itemType);

            return reduceexpr;
        }

        public static Expression GetMergeExpression(string query, Type itemType)
        {
            EvoQLBuilder builder = new EvoQLBuilder(query);

            ParameterExpression parameterExpression;
            EvoQLExpression parse;

            var filterexpr = builder.GetFilterExpression(out parse, out parameterExpression);

            var get = parse.Tree as GetExpression;

            var isCount = get.Select == "Count";
            var select = new SelectDescriptor { SourcePath = get.Select, Aggregate = isCount ? "Count" : "Sum", TargetPath = isCount ? "_Count" : get.Select };

            var mapreduceexpr = GetReduceExpression(query, itemType);
            var resultType = mapreduceexpr.GetType().GetProperty("ReturnType").GetValue(mapreduceexpr, null) as Type;
            var resultElementType = ReduceExpressionGeneration.GetTypeOfEnumerable(resultType);

            //Expression mergeExpr = ReduceExpressionGeneration.GetMergeExpression(select, resultElementType);

            var mergeexpr = ReduceExpressionGeneration.GetMergeInvocationExpression(select, resultType);

            return mergeexpr;
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


        private Expression GetFilterExpression(out EvoQLExpression parse, out ParameterExpression parameter)
        {
            parse = new EvoQLExpression(_query);

            if (parse.HadErrors)
            {
                string errorString = "EvoQL Error:\n";
                foreach (string error in parse.Errors)
                {
                    errorString += error + "\n";
                }
                throw new EvoQLException(errorString);
            }

            parse.Tree.Init();

            var root = parse.Tree;

            if (root is GetExpression)
            {
                var get = (GetExpression)root;

                var expression = get.CreateExpression(null);

                parameter = Expression.Parameter(typeof(Mention), "x");

                if (expression == null) return null;
                return ParameterRebinder.ReplaceParameters(expression, "x", parameter);
            }

            parameter = null;
            return null;
        }



        private Expression GetFilterLambda(out EvoQLExpression parse, out ParameterExpression parameter)
        {
            var expression = GetFilterExpression(out parse, out parameter);

            if (expression == null) return null;

            var param = Expression.Parameter(typeof(Mention), "x");
            Expression<Func<Mention, bool>> lambda = Expression.Lambda<Func<Mention, bool>>(ParameterRebinder.ReplaceParameters(expression, "x", param), param);

            return lambda;
        }

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

        private IEnumerable<Mention> AttachMethod(IEnumerable<Mention> source, string methodName, object parameter, ParameterExpression parameterExpression)
        {
            Type elementType = typeof(Mention);

            source = (IEnumerable<Mention>)typeof(Enumerable).GetMethods().Single(method => method.Name == methodName && method.GetParameters().Length == 2 && method.GetParameters().Skip(1).Single().ParameterType == parameter.GetType()).MakeGenericMethod(elementType).Invoke(null, new object[] { source, parameter });

            return source;
        }

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
    }

    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        private readonly ParameterExpression _replace;

        private readonly string _replaceName;

        public ParameterRebinder(ParameterExpression replace, string replaceName)
        {
            _replace = replace;
            _replaceName = replaceName;
        }

        public static Expression ReplaceParameters(Expression exp, string replaceName, ParameterExpression replacement)
        {
            return new ParameterRebinder(replacement, replaceName).Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (p.Name == _replaceName)
            {
                return base.VisitParameter(_replace);
            }
            else
            {
                return base.VisitParameter(p);
            }
        }

    }
}
