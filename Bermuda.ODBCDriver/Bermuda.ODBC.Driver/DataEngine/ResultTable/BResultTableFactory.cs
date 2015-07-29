using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI;
using Simba.DotNetDSI.DataEngine;

namespace Bermuda.ODBC.Driver.DataEngine.ResultTable
{
    public static class BResultTableFactory
    {
        public static IResult CreateResultTable(ILogger log, string sql, BProperties properties)
        {
            switch (properties.ResultType)
            {
                case BProperties.ResultTypes.Normal:
                    return new BResultTable(log, sql, properties);
                case BProperties.ResultTypes.FakeData:
                    return new BFakeResultTable(log, sql, properties);
                default:
                    return null;
            }
        }
    }
}
