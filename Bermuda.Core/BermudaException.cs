using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Core
{
    public class BermudaException : Exception
    {
        public BermudaException(string ex) : base(ex)
        {

        }
    }
}
