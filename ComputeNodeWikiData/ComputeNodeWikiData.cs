using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using Bermuda.Catalog;
using Bermuda.Constants;
using System.Runtime.Serialization;

namespace ComputeNodeWikiData
{
    [DataContract]
    public class ComputeNodeWikiData : ComputeNode
    {
        public ComputeNodeWikiData(Int64 index, Int64 bucket_count, Int64 compute_node_count)
            :base(index, bucket_count, compute_node_count)
        {
            
        }

        ICatalog catalog = null;
        //protected override IEnumerable<ICatalog> GetCatalogs()
        //{
        //    if (catalog == null)
        //    {
        //        catalog = this.InitializeCatalog();
        //    }

        //    return new List<ICatalog>() {catalog};
        //}
        
        ICatalog InitializeCatalog()
        {
            ICatalog catalog = new Catalog(this);
            catalog.CatalogName = "WikipediaData";
            //catalog.ConnectionString = ConnectionString;
            catalog.ConnectionType = ConnectionTypes.S3;
            catalog.CatalogMetadata = new CatalogMetadata(catalog);

            ITableMetadata tableMetadata = new TableMetadata(catalog.CatalogMetadata);
            tableMetadata.DataType = typeof(WikipediaHourlyPageStats);
            tableMetadata.ModField = "PrimaryKey";
            tableMetadata.PrimaryKey = "PrimaryKey";
            tableMetadata.ReferenceTable = false;
            tableMetadata.SaturationFrequency = 30000;
            tableMetadata.SaturationPurgeField = "RecordedOn";
            tableMetadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            tableMetadata.SaturationPurgePercent = 5;
            tableMetadata.SaturationPurgeType = typeof(DateTime);
            tableMetadata.SaturationUpdateField = "RecordedOn";
            tableMetadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            tableMetadata.SaturationUpdateType = typeof(DateTime);
            tableMetadata.TableName = "PageStats";

            ColumnMetadata col;
            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "PrimaryKey",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "RecordedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "RecordedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "ProjectCode",
                ColumnType = typeof(string),
                FieldMapping = "ProjectCode",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "PageName",
                ColumnType = typeof(string),
                FieldMapping = "PageName",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "PageViews",
                ColumnType = typeof(Int32),
                FieldMapping = "PageViews",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            col = new ColumnMetadata(tableMetadata)
            {
                ColumnName = "PageSizeKB",
                ColumnType = typeof(Int32),
                FieldMapping = "PageSizeKB",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tableMetadata.ColumnsMetadata.Add(col.ColumnName, col);

            catalog.CatalogMetadata.Tables.Add(tableMetadata.TableName, tableMetadata);
            return catalog;
        }
    }
}
