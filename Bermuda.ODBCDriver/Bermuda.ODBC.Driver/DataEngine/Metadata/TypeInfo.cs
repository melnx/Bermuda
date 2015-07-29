using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    /// Class describing the default information for a single SQL type.
    internal class TypeInfo
    {
        public string TypeName
        {
            get;
            set;
        }

        public SqlType DataType
        {
            get;
            set;
        }

        public int ColumnSize
        {
            get;
            set;
        }

        public string LiteralPrefix
        {
            get;
            set;
        }

        public string LiteralSuffix
        {
            get;
            set;
        }

        public string CreateParams
        {
            get;
            set;
        }

        public Nullability Nullable
        {
            get;
            set;
        }

        public bool CaseSensitive
        {
            get;
            set;
        }

        public Searchable Searchable
        {
            get;
            set;
        }

        public bool UnsignedAttr
        {
            get;
            set;
        }

        public short FixedPrecScale
        {
            get;
            set;
        }

        public bool AutoUnique
        {
            get;
            set;
        }

        public short MinScale
        {
            get;
            set;
        }

        public short MaxScale
        {
            get;
            set;
        }

        public short NumPrecRadix
        {
            get;
            set;
        }

        public short IntervalPrecision
        {
            get;
            set;
        }

        public short SqlDatetimeSub
        {
            get;
            set;
        }

        public SqlType SqlDataType
        {
            get;
            set;
        }

        public static TypeInfo CreateTypeInfo(SqlType type, string typeName, int columnSize)
        {
            TypeInfo typeInfo = new TypeInfo();

            typeInfo.DataType = type;
            typeInfo.TypeName = typeName;
            typeInfo.ColumnSize = columnSize;
            typeInfo.LiteralPrefix = null;
            typeInfo.LiteralSuffix = null;
            typeInfo.CreateParams = null;
            typeInfo.Nullable = Nullability.Nullable;
            typeInfo.CaseSensitive = false;
            typeInfo.Searchable = Searchable.Searchable;
            typeInfo.UnsignedAttr = true;
            typeInfo.FixedPrecScale = 0;
            typeInfo.AutoUnique = false;
            typeInfo.MinScale = 0;
            typeInfo.MaxScale = 0;
            typeInfo.NumPrecRadix = 0;
            typeInfo.IntervalPrecision = 0;
            typeInfo.SqlDatetimeSub = 0;
            typeInfo.SqlDataType = type;

            return typeInfo;
        }
    }
}
