using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Runtime.Serialization;

namespace Bermuda.Catalog
{
    [DataContract]
    public class TableMetadata : ITableMetadata
    {
        #region Variables and Properties

        /// <summary>
        /// the parent catalog metadata
        /// </summary>
        public ICatalogMetadata CatalogMetadata { get; set; }

        /// <summary>
        /// the name of the table entity
        /// </summary>
        [DataMember]
        public string TableName { get; set; }

        /// <summary>
        /// the type to use for this table
        /// </summary>
        [DataMember]
        public string DataTypeSerializer
        {
            get { return DataType == null ? null : DataType.AssemblyQualifiedName; }
            set { DataType = value == null ? null : Type.GetType(value); }
        }
        public Type DataType { get; set; }

        /// <summary>
        /// the maximum number of items to saturate at once
        /// </summary>
        [DataMember]
        public int MaxSaturationItems { get; set; }

        /// <summary>
        /// the base query for this table
        /// </summary>
        [DataMember]
        public string Query { get; set; }

        /// <summary>
        /// the base filter for this table
        /// </summary>
        [DataMember]
        public string Filter { get; set; }

        /// <summary>
        /// the base order by for this table
        /// </summary>
        [DataMember]
        public string OrderBy { get; set; }

        /// <summary>
        /// the field to update/add data for a table
        /// </summary>
        [DataMember]
        public string SaturationUpdateField { get; set; }

        /// <summary>
        /// the compare operator to update/add
        /// </summary>
        [DataMember]
        public string SaturationUpdateComparator { get; set; }

        /// <summary>
        /// the system type of the update variable
        /// </summary>
        [DataMember]
        public string SaturationUpdateTypeSerializer
        {
            get { return SaturationUpdateType == null ? null : SaturationUpdateType.AssemblyQualifiedName; }
            set { SaturationUpdateType = value == null ? null : Type.GetType(value); }
        }
        public Type SaturationUpdateType { get; set; }

        /// <summary>
        /// the expression to delete data for a table
        /// </summary>
        [DataMember]
        public string SaturationDeleteField { get; set; }

        /// <summary>
        /// the compare operator to delete data
        /// </summary>
        [DataMember]
        public string SaturationDeleteComparator { get; set; }

        /// <summary>
        /// the value to determine if an item should be deleted
        /// </summary>
        [DataMember]
        public object SaturationDeleteValue { get; set; }

        /// <summary>
        /// the syste type of the delete variable
        /// </summary>
        [DataMember]
        public string SaturationDeleteTypeSerializer
        {
            get { return SaturationDeleteType == null ? null : SaturationDeleteType.AssemblyQualifiedName; }
            set { SaturationDeleteType = value == null ? null : Type.GetType(value); }
        }
        public Type SaturationDeleteType { get; set; }

        /// <summary>
        /// the expression to purge data for a table
        /// </summary>
        [DataMember]
        public string SaturationPurgeField { get; set; }

        /// <summary>
        /// the operation to purge data
        /// </summary>
        [DataMember]
        public string SaturationPurgeOperation { get; set; }

        /// <summary>
        /// the system type of the purge variable
        /// </summary>
        [DataMember]
        public string SaturationPurgeTypeSerializer
        {
            get { return SaturationPurgeType == null ? null : SaturationPurgeType.AssemblyQualifiedName; }
            set { SaturationPurgeType = value == null ? null : Type.GetType(value); }
        }
        public Type SaturationPurgeType { get; set; }

        /// <summary>
        /// the percentage of data to purge from this table when space is needed
        /// </summary>
        [DataMember]
        public int SaturationPurgePercent { get; set; }


        /// <summary>
        /// how often to saturate this table
        /// </summary>
        [DataMember]
        public int SaturationFrequency { get; set; }

        /// <summary>
        /// the defined primary key for the table entity
        /// </summary>
        [DataMember]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// indicates that this is a reference table we do not mod it
        /// </summary>
        [DataMember]
        public bool ReferenceTable { get; set; }

        /// <summary>
        /// the field to use for mod distribution
        /// </summary>
        [DataMember]
        public string ModField { get; set; }

        /// <summary>
        /// the column metadata definitions
        /// </summary>
        [DataMember]
        public Dictionary<string, IColumnMetadata> ColumnsMetadata { get; set; }

        //fileprocessor stuff
        [DataMember]
        public bool IsFixedWidth { get; set; }

        [DataMember]
        public string[] ColumnDelimiters { get; set; }

        [DataMember]
        public int HeaderRowCount { get; set; }

        [DataMember]
        public string[] LineDelimiters { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with parent
        /// </summary>
        /// <param name="catalog_metadata"></param>
        public TableMetadata(ICatalogMetadata catalog_metadata)
        {
            Init(catalog_metadata);
        }

        /// <summary>
        /// init the table
        /// </summary>
        /// <param name="catalog_metadata"></param>
        public void Init(ICatalogMetadata catalog_metadata)
        {
            CatalogMetadata = catalog_metadata;
            if(ColumnsMetadata == null)
                ColumnsMetadata = new Dictionary<string, IColumnMetadata>();
        }

        #endregion

    }
}
