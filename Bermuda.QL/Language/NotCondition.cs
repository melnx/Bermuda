using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.QL
{
    public sealed partial class NotCondition : ConditionalExpression
    {
        protected override string ChildToString(bool includeUnimportant)
        {
            return "NOT:";
        }
    }
}
