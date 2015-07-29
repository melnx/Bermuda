using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public partial class SelectExpression : GetExpression
    {
        
        public override string ToString()
        {
            return "SELECT";

            /*
            var sb = new StringBuilder();

            sb.Append("SELECT ");
            sb.Append(string.Join(",", Selects));
            sb.Append(" FROM ");
            if (_fromSource != null)
            {
                sb.Append("(");
                sb.Append(_fromSource.ToString());
                sb.Append(")");
            }
            else if (Collections2 != null && Collections2.Any())
            {
                sb.Append(" ");
                sb.Append(Collections2.First());
                sb.Append(" ");

                foreach (var col in Collections2.Skip(1))
                {
                    sb.Append(" JOIN " + col);
                }
            }

            if (Child != null)
            {
                sb.Append(" WHERE ");
                sb.Append(Child.ToString());
            }

            if (Ordering != null)
            {
                sb.Append(" ORDERED BY ");
                sb.Append(Ordering);
                if (OrderDescending == true) sb.Append(" DESC ");
                else if (OrderDescending == false) sb.Append(" ASC ");
            }

            if (Skip != null && Take != null)
            {
                sb.Append(" LIMIT ");
                sb.Append(Skip);
                sb.Append(",");
                sb.Append(Take);
            }
            else if (Take != null)
            {
                sb.Append(" LIMIT ");
                sb.Append(Take);
            }

            return sb.ToString();*/
        }
    }
}
