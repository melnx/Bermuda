using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.ExpressionGeneration.Converters;

namespace Bermuda.ExpressionGeneration
{
    public partial class DimensionExpression : EnumerableBaseExpression
    {
        public string Source { get; internal set; }

        public List<string> SourcePath { get; internal set; }

        public string Target { get; internal set; }

        public string Function { get; internal set; }

        public Type SourceType { get; internal set; }

        public bool IsStar
        {
            get
            {
                return Source != null && Source.EndsWith("*");
            }
        }

        #region groupby
        public bool IsDateTime;
        public bool ParallelizedLinq;
        public System.Linq.Expressions.ParameterExpression GroupingEnumParameter;
        public bool IsAutoSelect;
        public DimensionExpression LinkedSelect;
        internal InExpression InClause;
        #endregion

        #region select
        public DimensionExpression LinkedGroupBy;
        public bool IsBasedOnGrouping;
        public bool IsNotted;
        public bool IsNegated;
        public bool IsLinkReversed;
        public bool IsFunctionCall;

        public bool IsAggregate
        {
            get
            {
                if (Child == null) return ReduceExpressionGeneration.IsAggregateFunction(Function);

                var childAsSelector = Child as SelectorExpression;

                if (childAsSelector != null)
                {
                    var right = childAsSelector.Right;
                    var rightAsDimension = right as DimensionExpression;

                    if (rightAsDimension != null)
                    {
                        return ReduceExpressionGeneration.IsAggregateFunction(rightAsDimension.Function);
                    }
                }

                return false;
            }
        }
        #endregion

        public DimensionExpression()
        {
            StarFields = new List<string>();
            Arguments = new List<DimensionExpression>();
            SourcePath = new List<string>();
        }

        public override IEnumerable<ExpressionTreeBase> GetChildren()
        {
            if (Child != null)
            {
                foreach (var c in Child.GetChildren())
                {
                    yield return c;
                }
            }

            foreach (var child in Arguments)
            {
                yield return child;
                foreach (var subchild in child.GetChildren())
                {
                    yield return subchild;
                }
            }
        }

        public List<string> StarFields { get; private set; }
        public List<DimensionExpression> Arguments { get; set; }

        public bool IsQuoted { get; set; }

        public override string ToString()
        {
            if (IsStar) return "*";

            var builder = new StringBuilder();

            bool skipTarget = false;

            if (Child != null)
            {
                builder.Append(Child.ToString());
            }
            else
            {
                if (Function != null)
                {
                    builder.Append(Function);
                    builder.Append("(");
                    builder.Append(string.Join(",", Arguments));
                    builder.Append(")");
                }
                else if(Source != null)
                {
                    builder.Append(Source);
                }
                else if(LinkedSelect != null)
                {
                    builder.Append(LinkedSelect);
                    skipTarget = true;
                }
            }

            if (Target != null && !skipTarget)
            {
                builder.Append(" AS ");
                builder.Append(Target);
            }

            return builder.ToString();
        }

        internal string GetChecksum()
        {
            var result = Source + "|" + Target + "|" + OrderDescending + "|" + Skip + "|" + Take + "|" + Function + "|" + SourceType + "|";
            result += (Arguments == null ? null : string.Join(",", Arguments.Select(x => x.GetChecksum())));
            result += "|";
            result += (Ordering == null ? null : Ordering.GetChecksum());

            var selChild = Child as SelectorExpression;

            if (selChild != null)
            {
                var dimChild = selChild.Child as DimensionExpression;
                if (dimChild != null)
                {
                    result += "[" + dimChild.GetChecksum() + "]";
                }
            }

            return result;
        }

        public override Expression CreateExpression(object context)
        {
            var param = context as DimensionCreateExpressionParameter;

            if (Child != null)
            {
                var result = Child.CreateExpression(context);

                if (IsNegated) 
                    result = Expression.Negate(result);
                if (IsNotted) 
                    result = Expression.Not(result);

                return result;
            }
            if (!IsFunctionCall)
            {
                var elementType = Root.ElementType;

                var path = Source;

                var targetField = ReduceExpressionGeneration.GetField(elementType, path, false);

                if (targetField == null)
                {
                    var asContext = context as DimensionCreateExpressionParameter;
                    if (asContext != null)
                    {
                        long longResult = 0;
                        double doubleResult = 0;
                        if (long.TryParse(Source, out longResult)) return Expression.Constant(longResult);
                        else if (double.TryParse(Source, out doubleResult)) return Expression.Constant(doubleResult);
                        else if (!asContext.QuoteUnknownIdentifiers && !IsQuoted) return null;

                        Expression cons = Expression.Constant( Convert.ChangeType(Source, SourceType) );

                        if (IsNegated) 
                            cons = Expression.Negate(cons);
                        if (IsNotted) 
                            cons = Expression.Not(cons);

                        return cons;
                    }
                    return null;
                }

                var xParam = Expression.Parameter(elementType, "x");

                if (targetField.FieldType == typeof(string) || targetField.FieldType == typeof(bool))
                {
                    return Expression.MakeMemberAccess(xParam, targetField);
                }
                else if (targetField.FieldType == typeof(long) || targetField.FieldType == typeof(double) || targetField.FieldType == typeof(int) || targetField.FieldType == typeof(float) || targetField.FieldType == typeof(DateTime))
                {                
                    Expression memberAccessExpr = Expression.MakeMemberAccess(xParam, targetField);

                    //if (IsNegated) 
                      //  memberAccessExpr = Expression.Negate(memberAccessExpr);

                    return memberAccessExpr;
                }
                else if (ReduceExpressionGeneration.IsCollectionType(targetField.FieldType))
                {
                    return Expression.MakeMemberAccess(xParam, targetField);
                }
                else
                {
                    var res = Expression.MakeMemberAccess(xParam, targetField);
                    return res;
                    //throw new NotImplementedException("Unsupported field Type: " + targetField.FieldType.Name);
                }
            }
            else
            {
                
                var elementType = Root.ElementType;
                var xParam = Expression.Parameter(elementType, "x");
                var asDim = (param.Left ?? param.Right) as DimensionExpression;
                var functionCall = ReduceExpressionGeneration.MakeFunctionCallExpression( asDim, elementType, xParam);

                return functionCall;
                //throw new NotImplementedException("Not supported functions in where logic");
            }
        }

        internal void AddArgument(DimensionExpression dim)
        {
            dim.SetParent(this);
            Arguments.Add(dim);
        }

        internal void AddArguments(IEnumerable<ExpressionTreeBase> iEnumerable)
        {
            var debug = string.Join(",", iEnumerable);

            foreach (DimensionExpression arg in iEnumerable)
            {
                AddArgument(arg);
            }
        }

        internal void CopyFrom(DimensionExpression s)
        {
            this.Function = s.Function;
            this.Target = s.Target;
            this.SetChild(s.Child);
            this.AddArguments(s.Arguments);
        }
    }

    public class DimensionCreateExpressionParameter
    {
        public int Index;
        public SelectorTypes SelectorType;
        public ModifierTypes ModifierType;
        public bool QuoteUnknownIdentifiers;
        public ExpressionTreeBase Left;
        public ExpressionTreeBase Right;
    }
}
