using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ODBC.Driver
{
    internal static class Driver
    {
        /// <summary>
        /// The connection key to use when looking up the UID in the connection string.
        /// </summary>
        public const string B_UID_KEY = "UID";

        /// <summary>
        /// The connection key to use when looking up the PWD in the connection string.
        /// </summary>
        public const string B_PWD_KEY = "PWD";

        /// <summary>
        /// The connection key to use when looking up the Catalog in the connection string.
        /// </summary>
        public const string B_CATALOG_KEY = "Catalog";

        /// <summary>
        /// The connection key to use when looking up the Server in the connection string.
        /// </summary>
        public const string B_SERVER_KEY = "Server";

        /// <summary>
        /// The connection key to use when looking up the rows to fetch in the connection string.
        /// </summary>
        public const string B_ROWS_TO_FETCH_KEY = "RowsToFetch";

        /// <summary>
        /// The connection key to use when looking up the result type in the connection string.
        /// </summary>
        public const string B_RESULT_TYPE_KEY = "ResultType";

        /// <summary>
        /// The connection key to use when looking up the LNG in the connection string.
        /// </summary>
        //public const string B_LNG_KEY = "LNG";

        /// <summary>
        /// The faked catalog for the hardcoded data.
        /// </summary>
        //public const string B_CATALOG = "BandwidthSF";

        /// <summary>
        /// The faked schema for the hardcoded data.
        /// </summary>
        public const string B_SCHEMA = "Bermuda";

        /// <summary>
        /// The faked table for the hardcoded data.
        /// </summary>
        //public const string B_TABLE = "Mentions";

        /// <summary>
        /// The faked custom metadata result for the hardcoded data.
        /// </summary>
        //public const string B_CUSTOMMETADATARESULT = "SampleCustomSchema";
    }
}
