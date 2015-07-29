using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Core.Thrift
{
    partial class ThriftMention
    {
        DateTime? date;
        public DateTime OccurredOn
        {
            get
            {
                return (date ?? (date = new DateTime(OccurredOnTicks))).Value;
            }
        }

        public int Id;
        public DateTime UpdatedOn;
    }
}
