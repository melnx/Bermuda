using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Core.BSON
{
    public class BSONMention
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<int> Tags { get; set; }
        public double Sentiment { get; set; }
        public double Influence { get; set; }
        public long OccurredOnTicks { get; set; }
        public long CreatedOnTicks { get; set; }
        public string Guid { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("BSONMention(");
            sb.Append("Name: ");
            sb.Append(Name);
            sb.Append(",Description: ");
            sb.Append(Description);
            sb.Append(",Tags: ");
            sb.Append(Tags);
            sb.Append(",Sentiment: ");
            sb.Append(Sentiment);
            sb.Append(",Influence: ");
            sb.Append(Influence);
            sb.Append(",OccurredOnTicks: ");
            sb.Append(OccurredOnTicks);
            sb.Append(",CreatedOnTicks: ");
            sb.Append(CreatedOnTicks);
            sb.Append("Guid: ");
            sb.Append(Guid);
            sb.Append(")");
            return sb.ToString();
        }
    }
}