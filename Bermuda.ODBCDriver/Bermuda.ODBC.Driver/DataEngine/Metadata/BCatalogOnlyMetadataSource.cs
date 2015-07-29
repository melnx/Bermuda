using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI;
using Simba.DotNetDSI.DataEngine;
using Bermuda.Interface.Connection.External;
//using Bermuda.Core;
//using Bermuda.Core.Connection.External;

namespace Bermuda.ODBC.Driver.DataEngine.Metadata
{
    class BCatalogOnlyMetadataSource : IMetadataSource
    {
        #region Fields

        /// <summary>
        /// Is fetching underway.
        /// </summary>
        private bool m_Fetching = false;

        /// <summary>
        /// The current row in this source.
        /// </summary>
        private int m_Current = 0;

        /// <summary>
        /// the list of catalogs
        /// </summary>
        private List<string> m_Catalogs = new List<string>();

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
        public BCatalogOnlyMetadataSource(ILogger log, BProperties properties)
        {
            LogUtilities.LogFunctionEntrance(log, log);
            Log = log;
            m_Properties = properties;

            try
            {
                //get the client connection
                using (var client = ExternalServiceClient.GetClient(m_Properties.Server))
                {
                    //get the catalogs
                    string[] catalogs = client.GetMetadataCatalogs();

                    //copy results
                    m_Catalogs.AddRange(catalogs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }

            //m_Catalogs.Add(Driver.B_CATALOG);
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
            m_Fetching = false;
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
                    out_data = m_Catalogs[m_Current];
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
            m_Fetching = true;
            m_Current = 0;

            return m_Current < m_Catalogs.Count;
        }

        /// <summary>
        /// Indicates that the cursor should be moved to the next row.
        /// </summary>
        /// <returns>True if there are more rows; false otherwise.</returns>
        public bool MoveToNextRow()
        {
            if (m_Fetching)
            {
                m_Current++;
            }
            else
            {
                m_Fetching = true;
                m_Current = 0;
            }

            return m_Current < m_Catalogs.Count;
        }

        #endregion // Methods
    }
}
