using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Runtime.Serialization;

namespace Bermuda.Catalog
{
    [DataContract]
    public class CatalogMetadata : ICatalogMetadata
    {
        #region Variables and Properties

        /// <summary>
        /// the parent catalog
        /// </summary>
        public ICatalog Catalog { get; set; }

        /// <summary>
        /// the collection of table metadata
        /// </summary>
        [DataMember]
        public Dictionary<string, ITableMetadata> Tables { get; set; }

        /// <summary>
        /// the defined relationships
        /// </summary>
        [DataMember]
        public Dictionary<string, IRelationshipMetadata> Relationships { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with parent
        /// </summary>
        /// <param name="catalog"></param>
        public CatalogMetadata(ICatalog catalog)
        {
            Init(catalog);
        }

        /// <summary>
        /// intit the catalog
        /// </summary>
        /// <param name="catalog"></param>
        public void Init(ICatalog catalog)
        {
            Catalog = catalog;
            Tables = new Dictionary<string, ITableMetadata>();
            Relationships = new Dictionary<string, IRelationshipMetadata>();
        }

        #endregion

        #region Methods



        #endregion
    }
}
