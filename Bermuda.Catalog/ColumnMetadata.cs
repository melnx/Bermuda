using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Runtime.Serialization;

namespace Bermuda.Catalog
{
    [DataContract]
    public class ColumnMetadata : IColumnMetadata
    {
        #region Variables and Properties

        /// <summary>
        /// the parent table meta data
        /// </summary>
        public ITableMetadata TableMetadata { get; set; }

        /// <summary>
        /// the name of the column
        /// </summary>
        [DataMember]
        public string ColumnName { get; set; }

        /// <summary>
        /// the column type
        /// </summary>
        [DataMember]
        public string ColumnTypeSerializer
        {
            get { return ColumnType == null ? null : ColumnType.AssemblyQualifiedName; }
            set { ColumnType = value == null ? null : Type.GetType(value); }
        }
        public Type ColumnType { get; set; }

        /// <summary>
        /// the column is nullable
        /// </summary>
        [DataMember]
        public bool Nullable { get; set; }

        /// <summary>
        /// the column is visible
        /// </summary>
        [DataMember]
        public bool Visible { get; set; }

        /// <summary>
        /// the column length for strings
        /// </summary>
        [DataMember]
        public int ColumnLength { get; set; }

        /// <summary>
        /// the column precision for double, floating point, decimal, ...
        /// </summary>
        [DataMember]
        public int ColumnPrecision { get; set; }

        /// <summary>
        /// the field mapping from query to column
        /// </summary>
        [DataMember]
        public string FieldMapping { get; set; }

        /// <summary>
        /// the position of the column
        /// </summary>
        [DataMember]
        public int OrdinalPosition { get; set; }

        //fileprocessorstuff
        [DataMember]
        public int FixedWidthStartIndex { get; set; }

        [DataMember]
        public int FixedWidthLength { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with parent
        /// </summary>
        /// <param name="table_metadata"></param>
        public ColumnMetadata(ITableMetadata table_metadata)
        {
            Init(table_metadata);
        }

        /// <summary>
        /// init the column
        /// </summary>
        /// <param name="table_metadata"></param>
        public void Init(ITableMetadata table_metadata)
        {
            TableMetadata = table_metadata;
        }

        #endregion
    }
}
