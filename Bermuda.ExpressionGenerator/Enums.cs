using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public enum GroupByTypes
    {
        None,
        [UseInterval] Date,
        Type,
        [OneToMany] Tags,
        [OneToMany] Datasources,
        [OneToMany] Themes
    }

    public enum IntervalTypes
    {
        None,
        Second,
        Minute,
        QuarterHour,
        Hour,
        Day,
        Week,
        Month,
        Quarter,
        Year,
    }

    public enum SelectTypes
    {
        None,
        Count,
        Sentiment
    }

    public enum OrderTypes
    {
        Ascending,
        Descending
    }

    /*
    public class SelectDescriptor
    {
        public string SourcePath;
        public string TargetPath;
        public string Function;
        public bool Star;
        public Type SourceType;
        public List<string> Arguments;

        public override bool Equals(object obj)
        {
            var asSelDesc = obj as SelectDescriptor;
            if (asSelDesc == null) return false;

            return asSelDesc.Function == Function && asSelDesc.SourcePath == SourcePath;
        }

        internal string GetChecksum()
        {
            return SourcePath + "|" + TargetPath + "|" + Function;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string result;

            result = SourcePath;
            if (Function != null) result = Function + "(" + result + ")";
            if (TargetPath != SourcePath && TargetPath != null) result += " as " + TargetPath;

            return result;
        }
    }

    public class PagingDescriptor
    {
        public string Ordering;
        public bool OrderDescending;
        public int? Skip;
        public int? Take;
    }

    public class CollectionDescriptor
    {
        public string Name;
        public string Alias;

        public override string ToString()
        {
            string result;

            result = Name;
            if (Alias != null && Alias != Name) result += " as " + Alias;

            return result;
        }
    }

    public class GroupByDescriptor
    {
        public string GroupBy;
        public SelectDescriptor OrderBy;
        public bool OrderDescending;
        public int? Skip;
        public int? Take;
        //public string Interval;
        public ParameterExpression GroupingEnumParameter;
        public bool ParallelizedLinq;
        public bool IsDateTime;
        public string Alias;
        public string Function;
        public List<GroupByDescriptor> Arguments;

        //public string GroupByPropertyPath
        //{
        //    get
        //    {
        //        return GroupBy + Interval + (Interval.HasValue ? "Ticks" : null);
        //    }
        //}

        internal string GetChecksum()
        {
            return GroupBy + "|" + OrderBy + "|" + OrderDescending + "|" + Skip + "|" + Take + "|" + Function;
        }
    }*/

    public class OneToManyAttribute : Attribute{}
    public class UseIntervalAttribute : Attribute { }

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
