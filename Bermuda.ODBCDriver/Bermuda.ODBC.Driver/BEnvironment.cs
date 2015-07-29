using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI;

namespace Bermuda.ODBC.Driver
{
    public class BEnvironment : DSIEnvironment
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="driver">The parent IDriver.</param>
        public BEnvironment(IDriver driver)
            : base(driver)
        {
            LogUtilities.LogFunctionEntrance(Driver.Log, driver);
        }

        /// <summary>
        /// Factory method for creating IConnections.
        /// </summary>
        /// <returns>A new IConnection instance.</returns>
        public override Simba.DotNetDSI.IConnection CreateConnection()
        {
            LogUtilities.LogFunctionEntrance(Driver.Log);
            return new BConnection(this);
        }
    }
}
