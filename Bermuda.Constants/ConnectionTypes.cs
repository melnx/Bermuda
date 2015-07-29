using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Constants
{
    public enum ConnectionTypes
    {
        Unknown = 0,
        SQLServer = 1,
        Oracle = 2,
        ODBC = 3,
        FileSystem = 4,
        S3 = 5
    }
}
