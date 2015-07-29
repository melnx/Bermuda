using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class Bucket : IBucket
    {

        #region Variables and Properties

        /// <summary>
        /// the parent catalog for bucket
        /// </summary>
        public ICatalog Catalog { get; set; }

        /// <summary>
        /// the mode for this bucket
        /// </summary>
        public Int64 BucketMod { get; set; }

        /// <summary>
        /// the collection of data tables for this bucket
        /// </summary>
        public Dictionary<string, IBucketDataTable> BucketDataTables { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with parent
        /// </summary>
        /// <param name="catalog"></param>
        public Bucket(ICatalog catalog)
        {
            Catalog = catalog;
            BucketDataTables = new Dictionary<string, IBucketDataTable>();
        }

        #endregion

    }
}
