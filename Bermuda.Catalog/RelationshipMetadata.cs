using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Runtime.Serialization;

namespace Bermuda.Catalog
{
    [DataContract]
    public class RelationshipMetadata : IRelationshipMetadata
    {
        #region Variabled and Properties

        /// <summary>
        /// the parent catalog metadata
        /// </summary>
        public ICatalogMetadata CatalogMetadata { get; set; }

        /// <summary>
        /// the name to identify the relationship
        /// </summary>
        [DataMember]
        public string RelationshipName { get; set; }

        /// <summary>
        /// the parent of this relationship
        /// </summary>
        [DataMember]
        public string ParentTableName { get; set; }
        public ITableMetadata ParentTable 
        {
            get { return ParentTableName == null ? null : CatalogMetadata.Tables[ParentTableName]; }
            set { ParentTableName = value == null ? null : value.TableName; }
        }

        /// <summary>
        /// the parent field for this relationship
        /// </summary>
        [DataMember]
        public string ParentField { get; set; }

        /// <summary>
        /// the child of this relationship
        /// </summary>
        [DataMember]
        public string ChildTableName { get; set; }
        public ITableMetadata ChildTable 
        {
            get { return ChildTableName == null ? null : CatalogMetadata.Tables[ChildTableName]; }
            set { ChildTableName = value == null ? null : value.TableName; }
        }

        /// <summary>
        /// the child field for this relationship
        /// </summary>
        [DataMember]
        public string ChildField { get; set; }

        /// <summary>
        /// the table that defines the relationship
        /// </summary>
        [DataMember]
        public string RelationTableName { get; set; }
        public ITableMetadata RelationTable 
        {
            get { return RelationTableName == null ? null : CatalogMetadata.Tables[RelationTableName]; }
            set { RelationTableName = value == null ? null : value.TableName; }
        }

        /// <summary>
        /// the parent field in association that relates to parent table
        /// </summary>
        [DataMember]        
        public string ParentRelationshipField { get; set; }

        /// <summary>
        /// the parent collection for the children
        /// </summary>
        [DataMember]
        public string ParentChildCollection { get; set; }

        /// <summary>
        /// the child field in association that relates to child table
        /// </summary>
        [DataMember]
        public string ChildRelationshipField { get; set; }

        /// <summary>
        /// this is a distinct relationship where associations are deduped
        /// </summary>
        [DataMember]
        public bool DistinctRelationship { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with parent
        /// </summary>
        /// <param name="catalog_metadata"></param>
        public RelationshipMetadata(ICatalogMetadata catalog_metadata)
        {
            Init(catalog_metadata);
        }

        /// <summary>
        /// init the realtionship
        /// </summary>
        /// <param name="catalog_metadata"></param>
        public void Init(ICatalogMetadata catalog_metadata)
        {
            CatalogMetadata = catalog_metadata;
        }

        #endregion

    }
}
