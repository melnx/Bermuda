using System;
using System.Net;
using System.Collections.Generic;

namespace Bermuda.QL
{
    public partial class RangeExpression : LiteralExpression
    {
        public string LowerValue { get; set; }

        public string UpperValue { get; set; }

        public RangeExpression(string value)
            : base(value)
        {
            string[] parts = value.Split(new string[] { ".." }, StringSplitOptions.None);
            LowerValue = parts[0];
            UpperValue = parts[1];
        }

        public override string ToString()
        {
            return String.Format("{0}..{1}", LowerValue, UpperValue);
        }
    }
}
