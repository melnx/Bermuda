using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bermuda.Constants;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Data.Odbc;

namespace Bermuda.DatabaseSaturator
{
    public static class DBFactory
    {
        /// <summary>
        /// creates a connection interface
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="ConnectionType"></param>
        /// <returns></returns>
        public static IDbConnection CreateConnection(string ConnectionString, ConnectionTypes ConnectionType)
        {
            switch (ConnectionType)
            {
                case ConnectionTypes.SQLServer:
                    return new SqlConnection(ConnectionString);
                case ConnectionTypes.Oracle:
                    return new OracleConnection(ConnectionString);
                case ConnectionTypes.ODBC:
                    return new OdbcConnection(ConnectionString);
                default:
                    return null;
            }
        }

        /// <summary>
        /// create a command interface
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="ConnectionType"></param>
        /// <returns></returns>
        public static IDbCommand CreateCommand(string query, IDbConnection connection, ConnectionTypes ConnectionType)
        {
            switch (ConnectionType)
            {
                case ConnectionTypes.SQLServer:
                    return new SqlCommand(query, (SqlConnection)connection);
                case ConnectionTypes.Oracle:
                    return new OracleCommand(query, (OracleConnection)connection);
                case ConnectionTypes.ODBC:
                    return new OdbcCommand(query, (OdbcConnection)connection);
                default:
                    return null;
            }
        }


    }
}
