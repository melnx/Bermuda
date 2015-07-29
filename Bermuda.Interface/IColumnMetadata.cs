using System;
namespace Bermuda.Interface
{
    public interface IColumnMetadata
    {
        void Init(ITableMetadata table_metadata);
        int ColumnLength { get; set; }
        string ColumnName { get; set; }
        int ColumnPrecision { get; set; }
        Type ColumnType { get; set; }
        bool Visible { get; set; }
        string ColumnTypeSerializer { get; set; }
        string FieldMapping { get; set; }
        bool Nullable { get; set; }
        ITableMetadata TableMetadata { get; set; }
        int OrdinalPosition { get; set; }

        //fileprocessor additions
        int FixedWidthStartIndex { get; set; }
        int FixedWidthLength { get; set; }
    }
}
