using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

//this is here so that we can compile
//needs to be changed to implement a generic type
using Bermuda.ExpressionGeneration;
using System.Reflection;
using Bermuda.Interface;

namespace Bermuda.ExpressionGeneration
{
    public partial class SelectorExpression : DoubleNodeTree
    {
        public ExpressionTreeBase Middle { get; private set; }

        public void SetMiddle(ExpressionTreeBase child)
        {
            Middle = child;
            if (Middle != null) Child.SetParent(this);
        }

        public string Target;
        public SelectorTypes NodeType { get; private set; }

        public ModifierTypes Modifier { get; private set; }

        public SelectorExpression() : base()
        {
        }

        public SelectorExpression(SelectorTypes field, ModifierTypes modifier) : this()
        {
            NodeType = field;
            Modifier = modifier;
        }

        public SelectorExpression(SelectorTypes field, ModifierTypes modifier, ExpressionTreeBase left) : this()
        {
            NodeType = field;
            Modifier = modifier;
            SetLeft( left );
        }

        public string ToString(bool ignoreSelector)
        {
            if (NodeType == SelectorTypes.Unspecified || ignoreSelector)
            {
                return Right.ToString() + (Target == null ? null : " as " + Target);
            }
            return String.Format("{0}{1}{2}{3}", Left, ConvertModifier(Modifier), Right, Target == null ? null : " as " + Target);


        }

        public override string ToString()
        {
            return ToString(false);
        }

        private string ConvertModifier(ModifierTypes type)
        {
            switch (type)
            {
                case ModifierTypes.Like:
                    return ":";
                case ModifierTypes.Equals:
                    return "=";
                case ModifierTypes.GreaterThan:
                    return ">";
                case ModifierTypes.LessThan:
                    return "<";
                case ModifierTypes.Add:
                    return "+";
                case ModifierTypes.Subtract:
                    return "-";
                case ModifierTypes.Multiply:
                    return "*";
                case ModifierTypes.Divide:
                    return "/";
                default:
                    return ":";
            }
        }

        public override Expression CreateExpression(object context)
        {
            //return null;
            //var literalRight = Right as LiteralExpression;
            //var valueRight = Right as ValueExpression;
            //var dimensionRight = Right as DimensionExpression;

            //var literalLeft = Left as LiteralExpression;
            //var valueLeft = Left as ValueExpression;
            //var dimensionLeft = Left as DimensionExpression;

            var leftExpression = Left == null ? null : Left.CreateExpression(new DimensionCreateExpressionParameter { QuoteUnknownIdentifiers = Right == null, Left = Left, Right = Right });
            var rightExpression = Right == null ? null : Right.CreateExpression(new DimensionCreateExpressionParameter { QuoteUnknownIdentifiers = true, Left = Left, Right = Right });

            var asLeftRange = leftExpression as NewArrayExpression;
            var asRightRange = rightExpression as NewArrayExpression;

            if (asLeftRange != null) throw new BermudaExpressionGenerationException("Ranges are only supported on the right hand side");

            if (asRightRange == null && leftExpression != null && rightExpression != null && leftExpression.Type != rightExpression.Type)
            {
                var asLeftLiteral = leftExpression as ConstantExpression;
                Expression asLeftDimension = leftExpression;
                var asRightLiteral = rightExpression as ConstantExpression;
                Expression asRightDimension = rightExpression;

                //if (asLeftDimension == null) asLeftDimension = leftExpression as MethodCallExpression;
                //if (asRightDimension == null) asRightDimension = asRightDimension as MethodCallExpression;

                try
                {
                    if (asLeftLiteral == null && asRightLiteral == null)
                    {
                        rightExpression = Expression.Convert(rightExpression, leftExpression.Type);
                    }
                    else if (asLeftDimension != null && asRightLiteral != null)
                    {
                        var targetType = leftExpression.Type;
                        if (ReduceExpressionGeneration.IsCollectionType(targetType)) targetType = ReduceExpressionGeneration.GetTypeOfEnumerable(targetType);
                        if (ReduceExpressionGeneration.IsTupleType(targetType)) targetType = targetType.GetGenericArguments().Last();

                        long num;
                        if (targetType == typeof(long) && asRightLiteral.Value is string && !long.TryParse(asRightLiteral.Value as string, out num))
                        {
                            var lookupKey = leftExpression.ToString().Split('.').LastOrDefault();

                            if (!string.IsNullOrWhiteSpace(lookupKey))
                            {
                                num = GetLookup(lookupKey, asRightLiteral.Value as string);
                            }
                        }

                        rightExpression = Expression.Constant(Convert.ChangeType(asRightLiteral.Value, targetType));
                    }
                    else if (asLeftLiteral != null && asRightDimension != null)
                    {
                        var targetType = rightExpression.Type;
                        if (ReduceExpressionGeneration.IsCollectionType(targetType)) targetType = ReduceExpressionGeneration.GetTypeOfEnumerable(targetType);
                        if (!ReduceExpressionGeneration.IsTupleType(targetType))
                        {
                            targetType = targetType.GetGenericArguments().Last();
                        }

                        leftExpression = Expression.Constant(Convert.ChangeType(asLeftLiteral.Value, targetType));
                    }
                    else if (asLeftLiteral != null && asRightLiteral != null)
                    {
                        leftExpression = Expression.Constant(Convert.ChangeType(asLeftLiteral.Value, rightExpression.Type));
                    }
                    else throw new BermudaExpressionGenerationException("Not supposed to happen");
                }
                catch(Exception ex)
                {
                    if (ex is BermudaExpressionGenerationException) throw ex;
                    //throw new BermudaExpressionGenerationException("Failed to convert: " + ex.Message);                    
                    Root.AddWarning(ex.ToString());
                    return null;
                }
            }

            //if (literalRight == null && valueRight == null)
            //{
            //    throw new Exception("The selector must specify an expression child or a literal child");
            //}


            //object rightValue = literalRight != null ? (object)literalRight.Value : (object)valueRight.Value;
            //object leftValue = literalLeft != null ? (object)literalLeft.Value : (object)valueLeft.Value;
            
            var getExpression = (GetExpression)Root;

            Type elementType = Root.ElementType;

            var xParam = Expression.Parameter(elementType, "x");

            //freeform strings inside the query
            switch(NodeType)
            {
                case SelectorTypes.Unspecified:
                {
                    if (Parent is DimensionExpression) return rightExpression;

                    if( Right != null)
                    {
                        var name = Right.ToString();
                        var boolField = ReduceExpressionGeneration.GetField(elementType, name, false);

                        if( boolField != null)
                        {
                            Expression boolFieldAccess = Expression.MakeMemberAccess(xParam, boolField);

                            return boolFieldAccess;
                        }
                        //var isTrue = Expression.Equal(boolFieldAccess, Expression.Constant(true));
                    }

                    return CreateUnspecifiedStringExpression(rightExpression, elementType, xParam);
                }

                case SelectorTypes.Unknown:
                {
                    if (leftExpression == null || rightExpression == null) return null;

                    switch (Modifier)
                    {
                        case ModifierTypes.Contains:
                            return CreateCollectionContainsExpression(rightExpression, xParam, leftExpression);

                        case ModifierTypes.In:
                            return CreateInExpression(rightExpression, xParam, leftExpression);

                        case ModifierTypes.Like:
                        case ModifierTypes.Colon:

                            if (leftExpression.Type == typeof(string))
                            {
                                return CreateStringFieldContainsExpression(leftExpression, rightExpression);
                            }
                            else if (ReduceExpressionGeneration.IsCollectionType(leftExpression.Type))
                            {
                                goto case ModifierTypes.Contains;
                            }
                            else
                            {
                                if (asRightRange != null) goto case ModifierTypes.InRange;
                                else goto case ModifierTypes.Equals;
                            }

                        case ModifierTypes.InRange:
                            return CreateInRangeExpression(leftExpression, asRightRange);

                        case ModifierTypes.Equals:
                            return Expression.Equal(leftExpression, rightExpression);

                        case ModifierTypes.GreaterThan:
                            if( asRightRange != null ) goto case ModifierTypes.InRange;
                            return Expression.GreaterThan(leftExpression, rightExpression);

                        case ModifierTypes.LessThan:
                            if( asRightRange != null ) goto case ModifierTypes.InRange;
                            return Expression.LessThan(leftExpression, rightExpression);
                        
                        case ModifierTypes.Add:
                            return Expression.Add(leftExpression, rightExpression);
                        case ModifierTypes.Subtract:
                            return Expression.Add(leftExpression, rightExpression);
                        case ModifierTypes.Multiply:
                            return Expression.Add(leftExpression, rightExpression);
                        case ModifierTypes.Divide:
                            return Expression.Add(leftExpression, rightExpression);

                        default:
                            
                            throw new BermudaExpressionGenerationException("Unsupported Operator " + Modifier + " for Operands " + leftExpression + " and " + rightExpression);
                            
                    }
                }
            }

            return null;
        }

        private Expression CreateInExpression(Expression rightExpression, ParameterExpression xParam, Expression leftExpression)
        {
            var asArray = rightExpression as NewArrayExpression;

            
            var conditions = asArray.Expressions.OfType<ConstantExpression>().Select
            (
                x => Expression.Equal( leftExpression, Expression.Constant( Convert.ChangeType( x.Value, leftExpression.Type ) ) )
            );

            var chained = ReduceExpressionGeneration.ChainOrExpressionCollection(conditions);

            return chained;
        }

        private long GetLookup(string lookupKey, string p)
        {
            throw new BermudaExpressionGenerationException("Could not find \"" + p + "\" in lookup named \"" + lookupKey + "\"");
        }

        private Expression CreateInRangeExpression(Expression leftExpression, NewArrayExpression asRightRange)
        {
            var lower = (asRightRange.Expressions[0] as ConstantExpression).Value;
            var upper = (asRightRange.Expressions[1] as ConstantExpression).Value;

            lower = ChangeType(lower, leftExpression.Type);
            upper = ChangeType(upper, leftExpression.Type);

            if (Modifier == ModifierTypes.LessThan)
            {
                return Expression.LessThan(leftExpression, Expression.Constant(lower));
            }
            if (Modifier == ModifierTypes.GreaterThan)
            {
                return Expression.GreaterThan(leftExpression, Expression.Constant(upper));
            }
            else
            {
                if (lower.Equals(upper))
                {
                    return Expression.Equal(leftExpression, Expression.Constant(lower));
                }
                else
                {
                    return Expression.AndAlso
                    (
                        Expression.LessThanOrEqual(leftExpression, Expression.Constant(upper)),
                        Expression.GreaterThanOrEqual(leftExpression, Expression.Constant(lower))
                    );
                }
            }
        }

        private static Expression CreateStringFieldContainsExpression(Expression leftExpression, Expression rightExpression)
        {
            /*
            var indexOfMethod = typeof(string).GetMethods().FirstOrDefault(x => x.Name == "IndexOf" && x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType == typeof(StringComparison) );

            return Expression.AndAlso
            (
                Expression.ReferenceNotEqual(leftExpression, Expression.Constant(null)),
                Expression.GreaterThanOrEqual
                (
                    Expression.Call
                    (
                        leftExpression,
                        indexOfMethod,
                        arg0: rightExpression,
                        arg1: Expression.Constant(StringComparison.OrdinalIgnoreCase)
                    ),
                    Expression.Constant(0)
                )
            );
            */

            var containsMethod = typeof(string).GetMethod("Contains"); 
            return Expression.AndAlso
            (
                Expression.ReferenceNotEqual(leftExpression, Expression.Constant(null)),
                Expression.Call
                (
                    method: containsMethod,
                    instance: leftExpression,
                    arguments: new Expression[]{ rightExpression }
                )
            );
        }

        private static Expression CreateUnspecifiedStringExpression(Expression leftExpression, Type elementType, ParameterExpression xParam)
        {
            var stringFields = elementType.GetFields().Where(x => x.FieldType == typeof(string));

            //Attribute.GetCustomAttributes(x, typeof(BermudaTextSearchMemberAttribute)) != null
            var stringContainsExpressions = stringFields.Where(x => x.Name == "Name" || x.Name == "Description").Select(x => CreateStringFieldContainsExpression(Expression.MakeMemberAccess(xParam, x), leftExpression ) );
            var chainOrredExpression = ReduceExpressionGeneration.ChainOrExpressionCollection(stringContainsExpressions);

            return chainOrredExpression;
        }

        public object ChangeType(object o, Type targetType)
        {
            double numeric;
            if (targetType == typeof(DateTime) && o is string && double.TryParse(o as string, out numeric))
            {
                return DateTime.UtcNow.AddDays(numeric);
            }
            
            return Convert .ChangeType(o, targetType);
        }

        private static Expression CreateCollectionContainsExpression(Expression rightExpression, ParameterExpression xParam, Expression leftExpression)
        {
            var itemType = ReduceExpressionGeneration.GetTypeOfEnumerable(leftExpression.Type);

            Expression rhs = null;

            if (ReduceExpressionGeneration.IsTupleType(itemType))
            {
                var anyMethodInfosGeneric = typeof(Enumerable).GetMethods().Where(x => x.Name == "Any" && x.GetParameters().Length == 2);
                var anyMethodInfoGeneric = anyMethodInfosGeneric.FirstOrDefault();
                var anyMethodInfo = anyMethodInfoGeneric.MakeGenericMethod(itemType);

                var item2Info = itemType.GetProperty("Item2");

                var itemParam = Expression.Parameter(itemType, "tpl");

                var item2Access = Expression.MakeMemberAccess(itemParam, item2Info);

                var equalsExpr = Expression.Equal(item2Access, rightExpression);
                
                var lambda = Expression.Lambda
                (
                    delegateType: typeof(Func<,>).MakeGenericType( itemType, typeof(bool) ),
                    parameters: itemParam,
                    body: equalsExpr
                );

                rhs = Expression.Call
                (
                    method: anyMethodInfo,
                    arg0: leftExpression,
                    arg1: lambda
                );

            }
            else
            {
                var containsMethodInfosGeneric = typeof(Enumerable).GetMethods().Where(x => x.Name == "Contains" && x.GetParameters().Length == 2);
                var containsMethodInfoGeneric = containsMethodInfosGeneric.FirstOrDefault();
                var containsMethodInfo = containsMethodInfoGeneric.MakeGenericMethod(itemType);

                //var actualValue = Convert.ChangeType(Child, itemType);

                rhs = Expression.Call
                (
                    method: containsMethodInfo,
                    arg0: leftExpression,
                    arg1: rightExpression
                );
                //var targetMemberAccess = Expression.MakeMemberAccess(xParam, targetField);
            }

            //return rhs;

            
            var result = Expression.AndAlso
            (
                Expression.ReferenceNotEqual(leftExpression, Expression.Constant(null)),    
                rhs
            );

            return result;
            
        }

        /*
        public Expression CreateExpressionOld(object context)
        {
            LiteralExpression child = Child as LiteralExpression;
            ValueExpression valueChild = Child as ValueExpression;
            if (child == null && valueChild == null)
            {
                throw new Exception();
            }
            object value = child != null ? (object)child.Value : (object)valueChild.Value;
            Expression childExpression = Child.CreateExpression(Type);

            var getExpression = (GetExpression)Root;

            switch (Type)
            {
                case SelectorTypes.Domain:
                    getExpression.Domain = child.Value;
                    return null;
                case SelectorTypes.Notes:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => x.Description.Contains(child.Value));
                case SelectorTypes.Subject:
                case SelectorTypes.Name:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => x.Name == child.Value);
                case SelectorTypes.Unspecified:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => (x.Name != null && x.Name.Contains(child.Value)) || (x.Description != null && x.Description.Contains(child.Value)));
                case SelectorTypes.Description:
                    return GetExpression<Mention>(x => x.Description.Contains(child.Value));
                case SelectorTypes.FromDate:
                    return Expression.GreaterThanOrEqual(GetExpression<Mention, DateTime>(x => x.Date), child.ConvertExpression<DateTime>());
                case SelectorTypes.Type:
                    return GetExpression<Mention>(x => x.Type == child.Value);
                case SelectorTypes.Id:
                    int id = (int)value;
                    return GetExpression<Mention>(x => x.Id == id);
                case SelectorTypes.Created:
                case SelectorTypes.Date:
                case SelectorTypes.On:
                {
                    var results = child.Convert<DateTime[]>("DateTimeRange");

                    Expression exp;

                    if (Type == SelectorTypes.Created)
                    {
                        exp = GetExpression<Mention, DateTime>(x => x.CreatedOn);
                    }
                    else
                    {
                        exp = GetExpression<Mention, DateTime>(x => x.Date); 
                    }

                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1]) return Expression.Equal(exp, Expression.Constant(results[0]));
                            else return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThan(exp, Expression.Constant(results[1])));
                    }
                }    

                case SelectorTypes.ToDate:
                case SelectorTypes.Until:
                
                    return Expression.LessThanOrEqual(GetExpression<Mention, DateTime>(x => x.Date), child.ConvertExpression<DateTime>());
                
                case SelectorTypes.Tag:
                
                    return CreateTagExpression(value, valueChild != null);
                

                
                case SelectorTypes.DataSource:
                    return Expression.Constant(false);
                    //return CreateDatasourceExpression(value, valueChild != null);

                case SelectorTypes.Theme:
                    return Expression.Constant(false);
                    //return CreatePhraseExpression(value, valueChild != null);

                case SelectorTypes.Influence:
                case SelectorTypes.KloutScore:
                case SelectorTypes.Followers:
                case SelectorTypes.Sentiment:
                {
                    double[] results = child.Convert<double[]>("Sentiment");
                    Expression exp = null;

                    switch(Type )
                    {
                        case SelectorTypes.Sentiment: exp = GetExpression<Mention, double>(x => x.Sentiment); break;
                    }

                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1])
                            {
                                return Expression.Equal(exp, Expression.Constant(results[0]));
                            }
                            else
                            {
                                return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThanOrEqual(exp, Expression.Constant(results[1])));
                            }
                    }
                }

                case SelectorTypes.InstanceType:
                case SelectorTypes.Hour:
                case SelectorTypes.Month:
                case SelectorTypes.Minute:
                case SelectorTypes.Year:
                case SelectorTypes.Day:
                {
                    var results = child.Convert<double[]>("NumberRange").Select(x => (int)x).ToArray();

                    Expression exp = null;

                    if (Type == SelectorTypes.Year)
                    {
                        exp = GetExpression<Mention, int>(x => x.Date.Year); 
                    }
                    else if (Type == SelectorTypes.Day)
                    {
                        exp = GetExpression<Mention, int>(x => x.Date.Day); 
                    }
                    else if (Type == SelectorTypes.Month)
                    {
                        exp = GetExpression<Mention, int>(x => x.Date.Month); 
                    }
                    else if (Type == SelectorTypes.Minute)
                    {
                        exp = GetExpression<Mention, int>(x => x.Date.Minute); 
                    }
                    else if (Type == SelectorTypes.Hour)
                    {
                        exp = GetExpression<Mention, int>(x => x.Date.Hour); 
                    }
                    
                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1])
                            {
                                return Expression.Equal(exp, Expression.Constant(results[0]));
                            }
                            else
                            {
                                return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThanOrEqual(exp, Expression.Constant(results[1])));
                            }
                    }
                }

                case SelectorTypes.TagCount:
                {
                    var results = child.Convert<double[]>("NumberRange").Select(x => (int)x).ToArray();
               
                    Expression exp;
                   
                    exp = GetExpression<Mention, int>(x => x.Tags.Count());
                    
                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            return Expression.Equal(exp, Expression.Constant(results[0]));
                    }
                }

                default:
                    return Expression.Constant(true);
            }
        }*/



        internal void SetModifierType(ModifierTypes modifierType)
        {
            Modifier = modifierType;
        }

        internal void SetNodeType(SelectorTypes nodeType)
        {
            NodeType = nodeType;
        }
    }
}
