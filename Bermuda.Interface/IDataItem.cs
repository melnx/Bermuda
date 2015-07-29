using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Interface
{
    public interface IDataItem
    {
        /// <summary>
        /// the primary key for the data item
        /// </summary>
        Int64 PrimaryKey { get; }
    }
}
