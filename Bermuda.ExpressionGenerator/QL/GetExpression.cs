using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Bermuda.ExpressionGeneration
{
    public abstract partial class EnumerableBaseExpression : RootExpression
    {
        public DimensionExpression Ordering { get; internal set; }

        public bool OrderDescending { get; internal set; }

        public int? Skip { get; internal set; }

        public int? Take { get; internal set; }
    }

    public partial class GetExpression : EnumerableBaseExpression
    {
        public GetExpression()
        {
         
        }       
        
        public List<GetTypes> _types = new List<GetTypes>();
        public List<CollectionExpression> _collections = new List<CollectionExpression>();
        public List<DimensionExpression> _selects = new List<DimensionExpression>();
        public List<DimensionExpression> _dimensions = new List<DimensionExpression>();
        public GetExpression Subselect;

        public void AddDimension(ExpressionTreeBase dim)
        {
            dim.SetParent(this);
            _dimensions.Add(dim as DimensionExpression);
        }

        public void AddSelect(ExpressionTreeBase sel)
        {
            sel.SetParent(this);
            _selects.Add(sel as DimensionExpression);
        }

        public void AddCollection(string col)
        {
            var expr = new CollectionExpression { Source = col };
            expr.SetParent(this);
            _collections.Add(expr);
        }

        public void AddCollection(string col, string alias)
        {
            var expr = new CollectionExpression { Source = col, Target = alias };
            expr.SetParent(this);
            _collections.Add(expr);
        }

        public void SetFrom(string name)
        {
            FromSourceString = name;
        }

        public void SetFrom(GetExpression source)
        {
            _fromSource = source;
        }

        public string FromSourceString { get; internal set; }
        public GetExpression _fromSource { get; internal set; }

        public HavingExpression Having { get; internal set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" SELECT ");

            sb.Append(string.Join(", ", _selects));

            sb.Append(" FROM ");

            if (Subselect != null)
            {
                sb.Append("(");
                sb.Append(Subselect);
                sb.Append(")");
            }
            else if(_collections != null)
            {
                sb.Append(string.Join(", ", _collections));
            }

            if (_dimensions.Any())
            {
                sb.Append(" GROUP BY ");

                sb.Append(string.Join(", ", _dimensions));
            }

            if (Having != null)
            {
                sb.Append(Having);
            }

            sb.Append(Child);

            return sb.ToString();
        }

        private string GetToString()
        {
            string types = String.Join(",", _types.Select(x => x.ToString()));

            StringBuilder builder = new StringBuilder();

            builder.Append("GET ");
            builder.Append(types);

            if (Child != null)
            {
                string condition;
                if (Child is ConditionGroup)
                {
                    condition = ((ConditionGroup)Child).ToString(true);
                }
                else
                {
                    condition = Child.ToString();
                }
                builder.Append(String.Format(" WHERE {0}", condition));
            }

            return builder.ToString();
        } 

        public override Expression CreateExpression(object context)
        {
            if (Child == null) return null;
            return Child.CreateExpression(null);
        }

        internal void AddGet(string p)
        {
            return;
        }

        internal void SetHaving(HavingExpression having)
        {
            Having = having;
            having.SetParent(this);
        }

        internal void RemoveSelect(DimensionExpression selLol)
        {
            if(selLol == null) return;
            else if (_selects.Contains(selLol)) _selects.Remove(selLol); 
            else throw new Exception("Could not remove select from clause");
        }

        public void AddSelects(IEnumerable<ExpressionTreeBase> argList)
        {
            var debug = string.Join(",", argList);

            foreach (DimensionExpression arg in argList)
            {
                AddSelect(arg);
            }

            //var kk = "CAST(TRUNCATE(QUARTER(Date),0) AS INTEGER)";

            var args = argList.ToArray();
            var roots = argList.SelectMany(x => x.GetChildren() ).ToArray();

            //var matchingroot = roots.Where(x => x.ToString() == kk).FirstOrDefault();

            var root = this.Root;
        }
    }

}
