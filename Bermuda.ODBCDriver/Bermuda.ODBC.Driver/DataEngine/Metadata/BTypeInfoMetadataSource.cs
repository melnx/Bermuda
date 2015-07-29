using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    
    /// <summary>
    /// UltraLight sample metadata table for types supported by the DSI implementation.
    ///
    /// This source contains the following output columns as defined by SimbaEngine:
    ///     DATA_TYPE_NAME
    ///     DATA_TYPE
    ///     COLUMN_SIZE
    ///     LITERAL_PREFIX
    ///     LITERAL_SUFFIX
    ///     CREATE_PARAM
    ///     NULLABLE
    ///     CASE_SENSITIVE
    ///     SEARCHABLE
    ///     UNSIGNED_ATTRIBUTE
    ///     FIXED_PREC_SCALE
    ///     AUTO_UNIQUE
    ///     LOCAL_TYPE_NAME
    ///     MINIMUM_SCALE
    ///     MAXIMUM_SCALE
    ///     SQL_DATA_TYPE
    ///     SQL_DATETIME_SUB
    ///     NUM_PREC_RADIX
    ///     INTERVAL_PRECISION
    ///     USER_DATA_TYPE
    /// </summary>
    class BTypeInfoMetadataSource : IMetadataSource
    {
        #region Fields

        /// <summary>
        /// Supported data types.
        /// </summary>
        private List<TypeInfo> m_DataTypes = new List<TypeInfo>();

        /// <summary>
        /// Is fetching underway.
        /// </summary>
        private bool m_IsFetching = false;

        /// <summary>
        /// The current row in this source.
        /// </summary>
        private int m_Current = 0;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">The logger to use for this metadata source.</param>
        public BTypeInfoMetadataSource(ILogger log)
        {
            LogUtilities.LogFunctionEntrance(log, log);

            Log = log;

            // Not using the restrictions, allow the SQLEngine to do filtering.
            InitializeDataTypes();
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// The logger to use for this metadata source.
        /// </summary>
        private ILogger Log
        {
            get;
            set;
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Close the metadata source's internal cursor. After this method is called, GetMetadata()
        /// and MoveToNextRow() will not be called again.
        /// </summary>
        public void CloseCursor()
        {
            LogUtilities.LogFunctionEntrance(Log);
            m_IsFetching = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting 
        /// unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            CloseCursor();
        }

        /// <summary>
        /// Fills in out_data with the data for a given column in the current row.
        /// </summary>
        /// <param name="columnTag">The column to retrieve data from.</param>
        /// <param name="offset">The number of bytes in the data to skip before copying.</param>
        /// <param name="maxSize">The maximum number of bytes of data to return.</param>
        /// <param name="out_data">The data to be returned.</param>
        /// <returns>True if there is more data in the column; false otherwise.</returns>
        public bool GetMetadata(
            MetadataSourceColumnTag columnTag,
            long offset,
            long maxSize,
            out object out_data)
        {
            LogUtilities.LogFunctionEntrance(Log, columnTag, offset, maxSize, "out_data");
            switch (columnTag)
            {
                case MetadataSourceColumnTag.DATA_TYPE_NAME:
                {
                    out_data = m_DataTypes[m_Current].TypeName;
                    return false;
                }

                case MetadataSourceColumnTag.DATA_TYPE:
                {
                    out_data = (short)m_DataTypes[m_Current].DataType;
                    return false;
                }

                case MetadataSourceColumnTag.COLUMN_SIZE:
                {
                    out_data = m_DataTypes[m_Current].ColumnSize;
                    return false;
                }

                case MetadataSourceColumnTag.LITERAL_PREFIX:
                {
                    out_data = m_DataTypes[m_Current].LiteralPrefix;
                    return false;
                }

                case MetadataSourceColumnTag.LITERAL_SUFFIX:
                {
                    out_data = m_DataTypes[m_Current].LiteralSuffix;
                    return false;
                }

                case MetadataSourceColumnTag.CREATE_PARAM:
                {
                    out_data = m_DataTypes[m_Current].CreateParams;
                    return false;
                }

                case MetadataSourceColumnTag.NULLABLE:
                {
                    out_data = (short)m_DataTypes[m_Current].Nullable;
                    return false;
                }

                case MetadataSourceColumnTag.CASE_SENSITIVE:
                {
                    out_data = (short)(m_DataTypes[m_Current].CaseSensitive ? 1 : 0);
                    return false;
                }

                case MetadataSourceColumnTag.SEARCHABLE:
                {
                    out_data = (short)m_DataTypes[m_Current].Searchable;
                    return false;
                }

                case MetadataSourceColumnTag.UNSIGNED_ATTRIBUTE:
                {
                    out_data = (short)(m_DataTypes[m_Current].UnsignedAttr ? 1 : 0);
                    return false;
                }

                case MetadataSourceColumnTag.FIXED_PREC_SCALE:
                {
                    out_data = m_DataTypes[m_Current].FixedPrecScale;
                    return false;
                }

                case MetadataSourceColumnTag.AUTO_UNIQUE:
                {
                    out_data = (short)(m_DataTypes[m_Current].AutoUnique ? 1 : 0);
                    return false;
                }
                    
                case MetadataSourceColumnTag.LOCAL_TYPE_NAME:
                {
                    out_data = m_DataTypes[m_Current].TypeName;
                    return false;
                }

                case MetadataSourceColumnTag.MINIMUM_SCALE:
                {
                    out_data = m_DataTypes[m_Current].MinScale;
                    return false;
                }
                    
                case MetadataSourceColumnTag.MAXIMUM_SCALE:
                {
                    out_data = m_DataTypes[m_Current].MaxScale;
                    return false;
                }
                    
                case MetadataSourceColumnTag.SQL_DATA_TYPE:
                {
                    out_data = (short)m_DataTypes[m_Current].SqlDataType;
                    return false;
                }
                    
                case MetadataSourceColumnTag.SQL_DATETIME_SUB:
                {
                    out_data = m_DataTypes[m_Current].SqlDatetimeSub;
                    return false;
                }
                    
                case MetadataSourceColumnTag.NUM_PREC_RADIX:
                {
                    out_data = m_DataTypes[m_Current].NumPrecRadix;
                    return false;
                }

                case MetadataSourceColumnTag.INTERVAL_PRECISION:
                {
                    out_data = m_DataTypes[m_Current].IntervalPrecision;
                    return false;
                }

                case MetadataSourceColumnTag.USER_DATA_TYPE:
                {
                    out_data = Simba.DotNetDSI.Constants.UDT_STANDARD_SQL_TYPE;
                    return false;
                }

                default:
                {
                    throw ExceptionBuilder.CreateException(
                        "Column Not Found",
                        columnTag.ToString());
                }
            }
        }

        /// <summary>
        /// Indicates that the cursor should be moved to before the first row.
        /// </summary>
        /// <returns>True if there are more rows; false otherwise.</returns>
        public bool MoveToBeforeFirstRow()
        {
            LogUtilities.LogFunctionEntrance(Log);
            m_IsFetching = true;
            m_Current = 0;

            return m_Current < m_DataTypes.Count;
        }

        /// <summary>
        /// Indicates that the cursor should be moved to the next row.
        /// </summary>
        /// <returns>True if there are more rows; false otherwise.</returns>
        public bool MoveToNextRow()
        {
            LogUtilities.LogFunctionEntrance(Log);
            if (m_IsFetching)
            {
                m_Current++;
            }
            else
            {
                m_IsFetching = true;
                m_Current = 0;
            }            

            return m_Current < m_DataTypes.Count;
        }

        /// <summary>
        /// Initializes the list of driver supported types.
        /// </summary>
        private void InitializeDataTypes()
        {
            LogUtilities.LogFunctionEntrance(Log);

            TypeInfo typeInfo = TypeInfo.CreateTypeInfo(SqlType.Char, "CHAR", 255);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            typeInfo.CreateParams = "LENGTH";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.VarChar, "VARCHAR", 510);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            typeInfo.CreateParams = "MAX LENGTH";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.LongVarChar, "LONGVARCHAR", 65500);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            typeInfo.CreateParams = "LENGTH";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Bit, "BIT", 1);
            typeInfo.Searchable = Searchable.PredicateBasic;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Decimal, "NUMERIC", 17);
            typeInfo.CreateParams = "PRECISION,SCALE";
            typeInfo.Searchable = Searchable.PredicateBasic;
            typeInfo.UnsignedAttr = false;
            typeInfo.MinScale = 0;
            typeInfo.MaxScale = 15;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Double, "DOUBLE", 15);
            typeInfo.Searchable = Searchable.PredicateBasic;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Real, "REAL", 7);
            typeInfo.Searchable = Searchable.PredicateBasic;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.TinyInt, "TINYINT", 3);
            typeInfo.Searchable = Searchable.PredicateBasic;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.SmallInt, "SMALLINT", 5);
            typeInfo.Searchable = Searchable.PredicateBasic;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Integer, "INTEGER", 10);
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Type_Timestamp, "TIMESTAMP", 30);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Type_Date, "DATE", 10);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.Type_Time, "TIME", 8);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.WChar, "WCHAR", 255);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            typeInfo.CreateParams = "LENGTH";
            typeInfo.CaseSensitive = true;
            m_DataTypes.Add(typeInfo);

            typeInfo = TypeInfo.CreateTypeInfo(SqlType.WVarChar, "WVARCHAR", 510);
            typeInfo.LiteralPrefix = "\'";
            typeInfo.LiteralSuffix = "\'";
            typeInfo.CreateParams = "MAX LENGTH";
            typeInfo.CaseSensitive = true;
            m_DataTypes.Add(typeInfo);
        }

        #endregion // Methods
    }
}
