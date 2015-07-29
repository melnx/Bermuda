using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using Bermuda.Interface.Connection.External;
using Bermuda.Interface;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    /// <summary>
    /// UltraLight sample metadata table for tables.
    /// 
    /// This source contains the following output columns as defined by SimbaEngine:
    ///     CATALOG_NAME 
    ///     SCHEMA_NAME
    ///     TABLE_NAME
    ///     TABLE_TYPE
    ///     REMARKS
    /// </summary>
    class BTablesMetadataSource : IMetadataSource
    {
        private class TableMetadata
        {
            public string Catalog { get; set; }
            public string Schema { get { return Driver.B_SCHEMA; } }
            public string Table { get; set; }
            public string TableType { get { return "TABLE"; } }
            public string Remarks { get; set; }
        }

        #region Fields

        /// <summary>
        /// Is fetching underway.
        /// </summary>
        private bool m_IsFetching = false;

        /// <summary>
        /// The current row in this source.
        /// </summary>
        private int m_Current = 0;

        /// <summary>
        /// the list of tables to return
        /// </summary>
        private List<TableMetadata> m_Tables = new List<TableMetadata>();

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
        public BTablesMetadataSource(ILogger log, BProperties properties)
        {
            LogUtilities.LogFunctionEntrance(log, log);
            Log = log;
            m_Properties = properties;

            try
            {
                //get the client connection
                using (var client = ExternalServiceClient.GetClient(m_Properties.Server))
                {
                    //get the tables
                    TableMetadataResult[] tables = client.GetMetadataTables();

                    //copy results
                    tables.ToList().ForEach(t =>
                        {
                            m_Tables.Add(new TableMetadata()
                            {
                                Catalog = t.Catalog,
                                Table = t.Table,
                                Remarks = t.Table
                            });
                        });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }


            ////Mentions
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "Mentions",
            //    Remarks = "Mentions"
            //});
            ////Tags
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "Tags",
            //    Remarks = "Tags"
            //});
            ////Datasources
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "Datasources",
            //    Remarks = "Datasources"
            //});
            ////Themes
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "Themes",
            //    Remarks = "Themes"
            //});
            ////TagMentions
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "TagMentions",
            //    Remarks = "TagMentions"
            //});
            ////DatasourceMentions
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "DatasourceMentions",
            //    Remarks = "DatasourceMentions"
            //});
            ////ThemeMentions
            //m_Tables.Add(new TableMetadata()
            //{
            //    Catalog = Driver.B_CATALOG,
            //    Table = "ThemeMentions",
            //    Remarks = "ThemeMentions"
            //});
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
                    out_data = m_Tables[m_Current].Catalog;
                    return false;
                }

                case MetadataSourceColumnTag.SCHEMA_NAME:
                {
                    out_data = m_Tables[m_Current].Schema;
                    return false;
                }

                case MetadataSourceColumnTag.TABLE_NAME:
                {
                    out_data = m_Tables[m_Current].Table;
                    return false;
                }

                case MetadataSourceColumnTag.TABLE_TYPE:
                {
                    out_data = m_Tables[m_Current].TableType;
                    return false;
                }

                case MetadataSourceColumnTag.REMARKS:
                {
                    out_data = m_Tables[m_Current].Remarks;
                    return false;
                }

                default:
                {
                    throw ExceptionBuilder.CreateException(
                        "Column Metadata Not Found",
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

            return m_Current < m_Tables.Count;
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
            return m_Current < m_Tables.Count;
        }

        #endregion // Methods
    }
}
