using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI;

namespace Bermuda.ODBC.Driver
{
    public class BDriver : DSIDriver
    {

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public BDriver() : base()
        {
            LogUtilities.LogFunctionEntrance(Log);
            SetDriverPropertyValues();

            // SAMPLE: Adding resource managers here allows you to localize the Simba DotNetDSI and/or ADO.Net components.
            //Simba.DotNetDSI.Properties.Resources.ResourceManager.AddResourceManager(
            //    new System.Resources.ResourceManager("Simba.UltraLight.Properties.SimbaDotNetDSI", GetType().Assembly));
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// Get the driver-wide vendor name. This property should always return the same value
        /// for a given IDriver implementation.
        /// </summary>
        public override System.String VendorName
        {
            get
            {
                return "EvoApp";
            }
        }
        
        #endregion // Properties

        #region Methods

        /// <summary>
        /// Factory method for creating IEnvironments.
        /// </summary>
        /// <returns>A new IEnvironment instance.</returns>
        public override Simba.DotNetDSI.IEnvironment CreateEnvironment()
        {
            LogUtilities.LogFunctionEntrance(Log);
            return new BEnvironment(this);
        }

        /// <summary>
        /// Overrides some of the default driver properties.
        /// </summary>
        private void SetDriverPropertyValues()
        {
            // TODO(ODBC) #02: Set the driver properties.
            // TODO(ADO)  #03: Set the driver properties.
            SetProperty(DriverPropertyKey.DSI_DRIVER_DRIVER_NAME, "Bermuda.ODBC.Diver");
        }

        #endregion // Methods
    }
}


    