using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public class EvoQLException : Exception
    {
        public EvoQLException(string message)
            : base(message)
        {

        }
    }
}
