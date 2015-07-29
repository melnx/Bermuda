using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using Bermuda.Interface.Connection.External;
using Bermuda.Interface;
//using Bermuda.Core;
//using Bermuda.Core.Connection.External;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    /// <summary>
    /// Class describing a single table column.
    /// </summary>
    class ColumnInfo
    {
        public string m_TableCatalog;
        public string m_TableSchema;
        public string m_TableName;
        public string m_ColumnName;
        public SqlType m_DataType;
        public int m_ColumnSize;
        public int m_BufferLength;
        public short m_DecimalDigits;
        public Nullability m_Nullable;
        public string m_Remarks;
        public string m_ColumnDef;
        public int m_CharOctetLength;
        public int m_OrdinalPosition;
    };
    
    /// <summary>
    /// UltraLight sample metadata table for types supported by the DSI implementation.
    ///
    /// This source contains the following output columns as defined by SimbaEngine:
    ///     CATALOG_NAME
    ///     SCHEMA_NAME
    ///     TABLE_NAME
    ///     COLUMN_NAME
    ///     DATA_TYPE
    ///     DATA_TYPE_NAME
    ///     COLUMN_SIZE
    ///     BUFFER_LENGTH
    ///     DECIMAL_DIGITS
    ///     NUM_PREC_RADIX
    ///     NULLABLE
    ///     REMARKS
    ///     COLUMN_DEF
    ///     SQL_DATA_TYPE
    ///     SQL_DATETIME_SUB
    ///     CHAR_OCTET_LENGTH
    ///     ORDINAL_POSITION
    ///     IS_NULLABLE
    ///     USER_DATA_TYPE
    /// </summary>
    class BColumnsMetadataSource : IMetadataSource
    {
        #region Fields

        /// <summary>
        /// Supported data types.
        /// </summary>
        private List<ColumnInfo> m_Columns = new List<ColumnInfo>();

        /// <summary>
        /// Is fetching underway.
        /// </summary>
        private bool m_IsFetching = false;

        /// <summary>
        /// The current row in this source.
        /// </summary>
        private int m_Current = 0;

        /// <summary>
        /// the driver properties from connections string
        /// </summary>
        private BProperties m_Properties { get; set; }

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">The logger to use for this metadata source.</param>
        public BColumnsMetadataSource(ILogger log, BProperties properties)
        {
            LogUtilities.LogFunctionEntrance(log, log);
            Log = log;
            m_Properties = properties;

            //InitializeData();
            //List<ColumnInfo> column_infos = new List<ColumnInfo>();
            //column_infos.AddRange(m_Columns);
            //m_Columns.Clear();
            
            try
            {
                //get the client connection
                using (var client = ExternalServiceClient.GetClient(m_Properties.Server))
                {
                    //get the columns
                    ColumnMetadataResult[] columns = client.GetMetadataColumns();

                    //copy results
                    columns.ToList().Where(c => c.Visible).ToList().ForEach(c =>
                    {
                        //if(c.Table == "Mentions" &&
                        //    (
                        //        c.Column == "Id" ||
                        //        c.Column == "Name" || 
                        //        c.Column == "Description" ||
                        //        c.Column == "Type" ||
                        //        c.Column == "Sentiment" ||
                        //        c.Column == "Influence" ||
                        //        c.Column == "IsDisabled" ||
                        //        c.Column == "OccurredOn" ||
                        //        c.Column == "CreatedOn" ||
                        //        c.Column == "UpdatedOn" ||
                        //        c.Column == "Guid" ||
                        //        c.Column == "Author" ||
                        //        c.Column == "Followers" ||
                        //        c.Column == "Klout" ||
                        //        c.Column == "Comments" 
                        //    ))
                        {
                            m_Columns.Add(
                                GetColumnInfo(
                                    c.Catalog, 
                                    c.Table, 
                                    c.Column, 
                                    c.DataType, 
                                    c.ColumnLength,
                                    c.Nullable, 
                                    c.OrdinalPosition, 
                                    c.ColumnPrecision));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            
            
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
                case MetadataSourceColumnTag.CATALOG_NAME:
                {
                    out_data = m_Columns[m_Current].m_TableCatalog;
                    return false;
                }

                case MetadataSourceColumnTag.SCHEMA_NAME:
                {
                    out_data = m_Columns[m_Current].m_TableSchema;
                    return false;
                }

                case MetadataSourceColumnTag.TABLE_NAME:
                {
                    out_data = m_Columns[m_Current].m_TableName;
                    return false;
                }

                case MetadataSourceColumnTag.COLUMN_NAME:
                {
                    out_data = m_Columns[m_Current].m_ColumnName;
                    return false;
                }

                case MetadataSourceColumnTag.DATA_TYPE:
                {
                    out_data = (short)m_Columns[m_Current].m_DataType;
                    return false;
                }

                case MetadataSourceColumnTag.DATA_TYPE_NAME:
                {
                    out_data = TypeUtilities.GetTypeName(m_Columns[m_Current].m_DataType).Substring(4);
                    return false;
                }

                case MetadataSourceColumnTag.COLUMN_SIZE:
                {
                    out_data = m_Columns[m_Current].m_ColumnSize;
                    return false;
                }

                case MetadataSourceColumnTag.BUFFER_LENGTH:
                {
                    out_data = m_Columns[m_Current].m_BufferLength;
                    return false;
                }

                case MetadataSourceColumnTag.DECIMAL_DIGITS:
                {
                    out_data = m_Columns[m_Current].m_DecimalDigits;
                    return false;
                }

                case MetadataSourceColumnTag.NUM_PREC_RADIX:
                {
                    SqlType sqlType = m_Columns[m_Current].m_DataType;

                    if (TypeUtilities.IsExactNumericType(sqlType) ||
                        TypeUtilities.IsIntegerType(sqlType) ||
                        TypeUtilities.IsApproximateNumericType(sqlType))
                    {
                        out_data = (short)10;
                    }
                    else
                    {
                        out_data = null;
                    }

                    return false;
                }

                case MetadataSourceColumnTag.NULLABLE:
                {
                    out_data = (short)m_Columns[m_Current].m_Nullable;
                    return false;
                }

                case MetadataSourceColumnTag.REMARKS:
                {
                    out_data = m_Columns[m_Current].m_Remarks;
                    return false;
                }

                case MetadataSourceColumnTag.COLUMN_DEF:
                {
                    out_data = m_Columns[m_Current].m_ColumnDef;
                    return false;
                }

                case MetadataSourceColumnTag.SQL_DATA_TYPE:
                {
                    out_data = (short)TypeUtilities.GetVerboseTypeFromConciseType(
                        m_Columns[m_Current].m_DataType);
                    return false;
                }

                case MetadataSourceColumnTag.SQL_DATETIME_SUB:
                {
                    short dateTimeSub = TypeUtilities.GetIntervalCodeFromConciseType(
                        m_Columns[m_Current].m_DataType);

                    if (0 == dateTimeSub)
                    {
                        out_data = null;
                    }
                    else
                    {
                        out_data = dateTimeSub;
                    }

                    return false;
                }

                case MetadataSourceColumnTag.CHAR_OCTET_LENGTH:
                {
                    if (TypeUtilities.IsCharacterOrBinaryType(m_Columns[m_Current].m_DataType))
                    {
                        out_data = m_Columns[m_Current].m_CharOctetLength;
                    }
                    else
                    {
                        out_data = null;
                    }

                    return false;
                }

                case MetadataSourceColumnTag.ORDINAL_POSITION:
                {
                    out_data = m_Current + 1;
                    return false;
                }

                case MetadataSourceColumnTag.IS_NULLABLE:
                {
                    if (Nullability.Nullable == m_Columns[m_Current].m_Nullable)
                    {
                        out_data = "YES";
                    }
                    else
                    {
                        out_data = "NO";
                    }

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

            return m_Current < m_Columns.Count;
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

            return m_Current < m_Columns.Count;
        }

        /// <summary>
        /// Initializes the list of columns for the hardcoded Person table.
        /// </summary>
        //private void InitializeData()
        //{
        //    LogUtilities.LogFunctionEntrance(Log);

        //    //Mentions
        //    //////////////////////////////////
        //    string Table = "Mentions";
        //    ColumnInfo colInfo;

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //Name
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Name";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100;
        //    colInfo.m_BufferLength = 100;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Name";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //Description
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Description";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100000;
        //    colInfo.m_BufferLength = 100000;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Description";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100000;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //Type
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Type";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100;
        //    colInfo.m_BufferLength = 100;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Type";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    // Sentiment
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Sentiment";
        //    colInfo.m_DataType = SqlType.Double;
        //    colInfo.m_ColumnSize = 15;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 10;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Sentiment";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 5;
        //    m_Columns.Add(colInfo);

        //    //Influence
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Influence";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Influence";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 6;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 7;
        //    m_Columns.Add(colInfo);

        //    //OccurredOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "OccurredOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "OccurredOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 8;
        //    m_Columns.Add(colInfo);

        //    //CreatedOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "CreatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "CreatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 9;
        //    m_Columns.Add(colInfo);

        //    //UpdateOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "UpdatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "UpdatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 10;
        //    m_Columns.Add(colInfo);

        //    //Guid
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Guid";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100000;
        //    colInfo.m_BufferLength = 100000;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Guid";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100000;
        //    colInfo.m_OrdinalPosition = 11;
        //    m_Columns.Add(colInfo);

        //    //Author
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Author";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100000;
        //    colInfo.m_BufferLength = 100000;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Author";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100000;
        //    colInfo.m_OrdinalPosition = 12;
        //    m_Columns.Add(colInfo);

        //    //Followers
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Followers";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Followers";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 13;
        //    m_Columns.Add(colInfo);

        //    //Klout
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Klout";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Klout";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 14;
        //    m_Columns.Add(colInfo);

        //    //Comments
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Comments";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Comments";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 15;
        //    m_Columns.Add(colInfo);

        //    //Tags
        //    ////////////////
        //    Table = "Tags";

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //Name
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Name";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100;
        //    colInfo.m_BufferLength = 100;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Name";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //CreatedOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "CreatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "CreatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    //Datasources
        //    /////////////////////////////
        //    Table = "Datasource";

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //Name
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Name";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100;
        //    colInfo.m_BufferLength = 100;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Name";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //CreatedOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "CreatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "CreatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //type
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Type";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Type";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    //Value
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Value";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100000;
        //    colInfo.m_BufferLength = 100000;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Value";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100000;
        //    colInfo.m_OrdinalPosition = 5;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 6;
        //    m_Columns.Add(colInfo);

        //    //Themes
        //    /////////////////////////////
        //    Table = "Themes";

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //Text
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Text";
        //    colInfo.m_DataType = SqlType.VarChar;
        //    colInfo.m_ColumnSize = 100;
        //    colInfo.m_BufferLength = 100;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Text";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 100;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //TagMentions
        //    ////////////////////////////////
        //    Table = "TagMentions";

        //    //MentionId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "MentionId";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "MentionId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //TagId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "TagId";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "TagId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //Id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    //UpdateOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "UpdatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "UpdatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 5;
        //    m_Columns.Add(colInfo);

        //    //DatasourceMentions
        //    ////////////////////////////////////
        //    Table = "DatasourceMentions";

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //MentionId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "MentionId";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "MentionId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //DatasourceId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "DatasourceId";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "DatasourceId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    //UpdateOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "UpdatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "UpdatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 5;
        //    m_Columns.Add(colInfo);

        //    //tag mentions
        //    //////////////////////////////////
        //    Table = "TagMentions";

        //    //id
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "Id";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "Id";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 1;
        //    m_Columns.Add(colInfo);

        //    //MentionId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "MentionId";
        //    colInfo.m_DataType = SqlType.Integer;
        //    colInfo.m_ColumnSize = 10;
        //    colInfo.m_BufferLength = 4;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "MentionId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 2;
        //    m_Columns.Add(colInfo);

        //    //ThemeId
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "ThemeId";
        //    colInfo.m_DataType = SqlType.BigInt;
        //    colInfo.m_ColumnSize = 19;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "ThemeId";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 3;
        //    m_Columns.Add(colInfo);

        //    //IsDisabled
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "IsDisabled";
        //    colInfo.m_DataType = SqlType.Bit;
        //    colInfo.m_ColumnSize = 1;
        //    colInfo.m_BufferLength = 1;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.NoNulls;
        //    colInfo.m_Remarks = "IsDisabled";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 4;
        //    m_Columns.Add(colInfo);

        //    //UpdateOn
        //    colInfo = new ColumnInfo();
        //    colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    colInfo.m_TableName = Table;
        //    colInfo.m_ColumnName = "UpdatedOn";
        //    colInfo.m_DataType = SqlType.Type_Timestamp;
        //    colInfo.m_ColumnSize = 30;
        //    colInfo.m_BufferLength = 8;
        //    colInfo.m_DecimalDigits = 0;
        //    colInfo.m_Nullable = Nullability.Nullable;
        //    colInfo.m_Remarks = "UpdatedOn";
        //    colInfo.m_ColumnDef = null;
        //    colInfo.m_CharOctetLength = 0;
        //    colInfo.m_OrdinalPosition = 5;
        //    m_Columns.Add(colInfo);


        //    //// First column in the Person table: Name
        //    //colInfo = new ColumnInfo();
        //    //colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    //colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    //colInfo.m_TableName = Table;
        //    //colInfo.m_ColumnName = "Name";
        //    //colInfo.m_DataType = SqlType.WVarChar;
        //    //colInfo.m_ColumnSize = 100;
        //    //colInfo.m_BufferLength = 100;
        //    //colInfo.m_DecimalDigits = 0;
        //    //colInfo.m_Nullable = Nullability.Nullable;
        //    //colInfo.m_Remarks = "Name column remarks";
        //    //colInfo.m_ColumnDef = null;
        //    //colInfo.m_CharOctetLength = 100;
        //    //colInfo.m_OrdinalPosition = 1;
        //    //m_Columns.Add(colInfo);

        //    //// Second column in the Person table: Integer
        //    //colInfo = new ColumnInfo();
        //    //colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    //colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    //colInfo.m_TableName = Table;
        //    //colInfo.m_ColumnName = "Integer";
        //    //colInfo.m_DataType = SqlType.Integer;
        //    //colInfo.m_ColumnSize = 10;
        //    //colInfo.m_BufferLength = 4;
        //    //colInfo.m_DecimalDigits = 0;
        //    //colInfo.m_Nullable = Nullability.Nullable;
        //    //colInfo.m_Remarks = "Integer column remarks";
        //    //colInfo.m_ColumnDef = null;
        //    //colInfo.m_CharOctetLength = 0;
        //    //colInfo.m_OrdinalPosition = 2;
        //    //m_Columns.Add(colInfo);

        //    //// Third column in the Person table: Numeric
        //    //colInfo = new ColumnInfo();
        //    //colInfo.m_TableCatalog = Driver.B_CATALOG;
        //    //colInfo.m_TableSchema = Driver.B_SCHEMA;
        //    //colInfo.m_TableName = Table;
        //    //colInfo.m_ColumnName = "Numeric";
        //    //colInfo.m_DataType = SqlType.Numeric;
        //    //colInfo.m_ColumnSize = 4;
        //    //colInfo.m_BufferLength = 6;
        //    //colInfo.m_DecimalDigits = 1;
        //    //colInfo.m_Nullable = Nullability.Nullable;
        //    //colInfo.m_Remarks = "Numeric column remarks";
        //    //colInfo.m_ColumnDef = null;
        //    //colInfo.m_CharOctetLength = 0;
        //    //colInfo.m_OrdinalPosition = 3;
        //    //m_Columns.Add(colInfo);
        //}

        private ColumnInfo GetColumnInfo(
            string catalog, 
            string table, 
            string column, 
            string data_type, 
            int length, 
            bool nullable, 
            int ordinal, 
            int precision)
        {
            Type type = null;
            try
            {
                type = Type.GetType(data_type);
            }
            catch (Exception)
            {
                object obj = new object();
                type = obj.GetType();
            }

            ColumnInfo info = new ColumnInfo();

            info.m_TableCatalog = catalog;
            info.m_TableSchema = Driver.B_SCHEMA;
            info.m_TableName = table;
            info.m_ColumnName = column;
            info.m_DataType = TypeMetadataHelper.GetSqlType(type);
            info.m_Nullable = nullable ? Nullability.Nullable : Nullability.NoNulls;
            info.m_Remarks = string.Format("{0}.{1}.{2}", catalog, table, column);
            info.m_ColumnDef = null;
            info.m_OrdinalPosition = ordinal;
            info.m_CharOctetLength = length;
            info.m_ColumnSize = TypeMetadataHelper.GetColumnSize(type, length);
            info.m_DecimalDigits = (short)precision;
            info.m_BufferLength = TypeMetadataHelper.GetBufferLength(type, length, precision);

            return info;
        }

        #endregion // Methods
    }
}