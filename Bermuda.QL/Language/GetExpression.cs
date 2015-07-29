using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Bermuda.QL
{
    public partial class SetExpression : RootExpression
    {
        public List<SetterExpression> Setters = new List<SetterExpression>();

        public void AddSetter(SetterExpression setter) 
        {
            Setters.Add(setter);
        }
    }

    public partial class GetExpression : RootExpression
    {
        public GetExpression()
        {
            Types = new ReadOnlyCollection<GetTypes>(_types);
        }

        public ReadOnlyCollection<GetTypes> Types { get; private set; }

        public List<GetTypes> _types = new List<GetTypes>();

        public void AddType(GetTypes type)
        {
            _types.Add(type);
        }

        public void SetTypes(IEnumerable<GetTypes> types)
        {
            _types.Clear();
            _types.AddRange(types);
        }

        public string GroupBy { get; internal set; }

        public string GroupOver { get; internal set; }

        public string Select { get; internal set; }

        public string Ordering { get; internal set; }

        public string GroupOverInterval { get; internal set; }

        public string GroupByInterval { get; internal set; }

        public bool IsChart { get; internal set; }

        public string Domain { get; internal set; }

        public bool OrderDescending { get; internal set; }

        public int? Skip { get; internal set; }

        public int? Take { get; internal set; }

        public bool? GroupOverDescending { get; internal set; }

        public bool? GroupByDescending { get; internal set; }

        public int? GroupOverTake { get; internal set; }

        public int? GroupByTake { get; internal set; }

        public string GroupOverOrderBy { get; internal set; }

        public string GroupByOrderBy { get; internal set; }

        public override string ToString()
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
    }

    public class EvoQLException : Exception
    {
        public EvoQLException(string message)
            : base(message)
        {

        }
    }
}
