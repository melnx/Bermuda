using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI;
using Simba.DotNetDSI.DataEngine;
using Bermuda.ODBC.Driver.DataEngine;

namespace Bermuda.ODBC.Driver
{
    public class BStatement : DSIStatement
    {
        #region Fields

        /// <summary>
        /// the driver properties from connections string
        /// </summary>
        private BProperties m_Properties { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">The parent connection.</param>
        public BStatement(BConnection connection, BProperties properties) : base(connection)
        {
            LogUtilities.LogFunctionEntrance(Connection.Log, connection);
            m_Properties = properties;
        }

        #endregion // Constructor

        #region Methods

        /// <summary>
        /// Factory method for creating IDataEngines.
        /// </summary>
        /// <returns>A new IDataEngine instance.</returns>
        public override IDataEngine CreateDataEngine()
        {
            LogUtilities.LogFunctionEntrance(Connection.Log);
            return new BDataEngine(this, m_Properties);
        }

        #endregion // Methods
    }
}
