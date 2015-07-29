using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ODBC.Driver
{
    public class BProperties
    {
        public enum ResultTypes
        {
            Normal = 0,
            FakeData = 1
        }

        public string Server { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Catalog { get; set; }
        public int RowsToFetch { get; set; }
        public ResultTypes ResultType { get; set; }
    }
}
