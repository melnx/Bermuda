using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.DomainLayer;

namespace Bermuda.QL
{
    public partial class GetExpression
    {
        

        public override Expression CreateExpression(object context)
        {
            if (Child == null) return null;
            return Child.CreateExpression(null);
        }
    }

    public partial class SetExpression
    {


        public override Expression CreateExpression(object context)
        {
            return null;
        }
    }
}
