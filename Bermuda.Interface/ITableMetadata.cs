using System;
using System.Collections.Generic;
namespace Bermuda.Interface
{
    public interface ITableMetadata
    {
        void Init(ICatalogMetadata catalog_metadata);
        ICatalogMetadata CatalogMetadata { get; set; }
        Dictionary<string, IColumnMetadata> ColumnsMetadata { get; set; }
        Type DataType { get; set; }
        string Filter { get; set; }
        int MaxSaturationItems { get; set; }
        string ModField { get; set; }
        string OrderBy { get; set; }
        string PrimaryKey { get; set; }
        string Query { get; set; }
        bool ReferenceTable { get; set; }
        string SaturationDeleteComparator { get; set; }
        string SaturationDeleteField { get; set; }
        Type SaturationDeleteType { get; set; }
        string SaturationDeleteTypeSerializer { get; set; }
        object SaturationDeleteValue { get; set; }
        int SaturationFrequency { get; set; }
        string SaturationUpdateComparator { get; set; }
        string SaturationUpdateField { get; set; }
        Type SaturationUpdateType { get; set; }
        string SaturationUpdateTypeSerializer { get; set; }
        string SaturationPurgeField { get; set; }
        Type SaturationPurgeType { get; set; }
        string SaturationPurgeTypeSerializer { get; set; }
        string SaturationPurgeOperation { get; set; }
        int SaturationPurgePercent { get; set; }
        string TableName { get; set; }

        //fileprocesssor additions
        bool IsFixedWidth { get; set; }
        string[] ColumnDelimiters { get; set; }
        int HeaderRowCount { get; set; }
        string[] LineDelimiters { get; set; }
    }
}
