using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class CatalogDataTable : DataTable, ICatalogDataTable  
    {

        #region Variables and Properties

        /// <summary>
        /// the parent catalog
        /// </summary>
        public ICatalog Catalog { get; set; }

        #endregion

        #region Constructor

        public CatalogDataTable(ICatalog catalog, ITableMetadata table_metadata)
            : base(table_metadata)
        {
            Catalog = catalog;
        }

        #endregion

    }
}
