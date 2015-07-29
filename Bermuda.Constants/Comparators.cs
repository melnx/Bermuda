using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Constants
{
    public static class Comparators
    {
        public const string EQUAL = "=";
        public const string NOT_EQUAL = "<>";
        public const string GREATER_THAN = ">";
        public const string GREATER_THAN_EQUAL_TO = ">=";
        public const string LESS_THAN = "<";
        public const string LESS_THAN_EQUAL_TO = "<=";

        public static string GetNegatedComparator(string comparator)
        {
            switch (comparator)
            {
                case EQUAL:
                    return NOT_EQUAL;
                case NOT_EQUAL:
                    return EQUAL;
                case GREATER_THAN:
                    return LESS_THAN_EQUAL_TO;
                case GREATER_THAN_EQUAL_TO:
                    return LESS_THAN;
                case LESS_THAN:
                    return GREATER_THAN_EQUAL_TO;
                case LESS_THAN_EQUAL_TO:
                    return GREATER_THAN;
                default:
                    return "";
            }
        }
    }
}
