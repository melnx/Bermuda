using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.AdminLibrary.Interfaces
{
    /// <summary>
    /// ICloudEntity - represents a cloud based instance, db, etc.
    /// </summary>
    public interface ICloudEntity
    {
        #region Properties
        string Id { get; set; }
        string Name { get; set; }

        #endregion Properties

        #region Methods

        #endregion Methods
    }
}
