using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    public static class TypeMetadataHelper
    {
        /// <summary>
        /// decode a system.type to SqlType for DSI
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SqlType GetSqlType(Type type)
        {
            if (type == typeof(Int16))
                return SqlType.SmallInt;
            else if (type == typeof(Int32) || type == typeof(int))
                return SqlType.Integer;
            else if (type == typeof(Int64) || type == typeof(long))
                return SqlType.BigInt;
            else if (type == typeof(string) || type == typeof(String))
                return SqlType.VarChar;
            else if (type == typeof(bool) || type == typeof(Boolean))
                return SqlType.Bit;
            else if (type == typeof(float))
                return SqlType.Float;
            else if (type == typeof(double) || type == typeof(Double))
                return SqlType.Double;
            else if (type == typeof(decimal) || type == typeof(Decimal))
                return SqlType.Decimal;
            else if (type == typeof(DateTime))
                return SqlType.Type_Timestamp;
            else
                return SqlType.Binary;
        }

        /// <summary>
        /// get the column size for system.type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int GetColumnSize(Type type, int length)
        {
            if (type == typeof(Int16))
                return 5;
            else if (type == typeof(Int32) || type == typeof(int))
                return 10;
            else if (type == typeof(Int64) || type == typeof(long))
                return 20;
            else if (type == typeof(string) || type == typeof(String))
                return length;
            else if (type == typeof(bool) || type == typeof(Boolean))
                return 1;
            else if (type == typeof(float))
                return 15;
            else if (type == typeof(double) || type == typeof(Double))
                return 15;
            else if (type == typeof(decimal) || type == typeof(Decimal))
                return 15;
            else if (type == typeof(DateTime))
                return 30;
            else
                return length;
        }

        /// <summary>
        /// get the buffer length required
        /// </summary>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int GetBufferLength(Type type, int length, int precision)
        {
            if (type == typeof(Int16) || type == typeof(short))
                return 2;
            else if (type == typeof(Int32) || type == typeof(int))
                return 4;
            else if (type == typeof(Int64) || type == typeof(long))
                return 20;
            else if (type == typeof(string) || type == typeof(String))
                return length;
            else if (type == typeof(bool) || type == typeof(Boolean))
                return 1;
            else if (type == typeof(float))
                return 8;
            else if (type == typeof(double) || type == typeof(Double))
                return 8;
            else if (type == typeof(decimal) || type == typeof(Decimal))
                return 8;
            else if (type == typeof(DateTime))
                return 16;
            else
                return length;
        }

        /// <summary>
        /// create the metadata for the type from a system.type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeMetadata CreateTypeMetadata(Type type)
        {
            return TypeMetadata.CreateTypeMetadata(GetSqlType(type));
        }
    }
}
