using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Constants;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class BucketDataTable : ReferenceDataTable, IBucketDataTable
    {
        #region Variables and Properties

        /// <summary>
        /// the parent bucket
        /// </summary>
        public IBucket Bucket { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with parent and table definition
        /// </summary>
        /// <param name="bucket"></param>
        public BucketDataTable(IBucket bucket, ITableMetadata table_metadata)
            :base(bucket.Catalog, table_metadata)
        {
            Bucket = bucket;
            LastSaturation = default(DateTime);
        }

        #endregion

        #region Methods

        public override string ConstructQuery()
        {
            //base query
            StringBuilder sb = new StringBuilder("SELECT");

            //handle top
            if (TableMetadata.MaxSaturationItems > 0)
                sb.AppendFormat(" TOP {0} {1}", TableMetadata.MaxSaturationItems, TableMetadata.Query);
            else
                sb.AppendFormat(" {0}", TableMetadata.Query);

            //mod
            sb.AppendFormat(" WHERE {0} % {1} = {2}", TableMetadata.ModField, Bucket.Catalog.ComputeNode.GlobalBucketCount, Bucket.BucketMod);

            //base filter
            if (!string.IsNullOrWhiteSpace(TableMetadata.Filter))
                sb.AppendFormat(" AND ({0})", TableMetadata.Filter);

            //update value
            if (this.TableMetadata.SaturationUpdateType == typeof(DateTime))
            {
                sb.AppendFormat(
                    " AND {0} {1} '{2} {3}'", 
                    TableMetadata.SaturationUpdateField, 
                    TableMetadata.SaturationUpdateComparator,
                    ((DateTime)LastUpdateValue).ToShortDateString(), 
                    ((DateTime)LastUpdateValue).ToShortTimeString());
            }
            else if (this.TableMetadata.SaturationUpdateType == typeof(string))
            {
                sb.AppendFormat(
                    " AND {0} {1} '{2}'", 
                    TableMetadata.SaturationUpdateField, 
                    TableMetadata.SaturationUpdateComparator,
                    LastUpdateValue.ToString());
            }
            else
            {
                sb.AppendFormat(
                    " AND {0} {1} {2}",
                    TableMetadata.SaturationUpdateField,
                    TableMetadata.SaturationUpdateComparator,
                    LastUpdateValue.ToString());
            }
            //order by
            if (!string.IsNullOrWhiteSpace(TableMetadata.OrderBy))
                sb.AppendFormat(" ORDER BY {0}", TableMetadata.OrderBy);

            return sb.ToString();
        }

        public string GetRelationshipQuery(IRelationshipMetadata rel, List<IDataItem> parents)
        {
            //base query
            StringBuilder sb = new StringBuilder("SELECT");

            //get base query
            sb.AppendFormat(" {0}", rel.RelationTable.Query);

            //mod
            sb.AppendFormat(" WHERE {0} % {1} = {2}", rel.RelationTable.ModField, Bucket.Catalog.ComputeNode.GlobalBucketCount, Bucket.BucketMod);

            //base filter
            if (!string.IsNullOrWhiteSpace(rel.RelationTable.Filter))
                sb.AppendFormat(" AND ({0})", rel.RelationTable.Filter);

            //handle deleted
            if (!string.IsNullOrWhiteSpace(rel.RelationTable.SaturationDeleteField))
            {
                IColumnMetadata col_delete = rel.RelationTable.ColumnsMetadata.Values.Where(c => c.FieldMapping == rel.RelationTable.SaturationDeleteField).FirstOrDefault();
                if(col_delete.ColumnType == typeof(DateTime))
                {
                    sb.AppendFormat(
                        " AND {0} {1} {2} {3}", 
                        col_delete.ColumnName, 
                        Comparators.GetNegatedComparator(rel.RelationTable.SaturationDeleteComparator), 
                        ((DateTime)rel.RelationTable.SaturationDeleteValue).ToShortDateString(),
                        ((DateTime)rel.RelationTable.SaturationDeleteValue).ToShortTimeString());
                }
                else if (col_delete.ColumnType == typeof(string))
                {
                    sb.AppendFormat(
                        " AND {0} {1} '{2}'",
                        col_delete.ColumnName,
                        Comparators.GetNegatedComparator(rel.RelationTable.SaturationDeleteComparator),
                        rel.RelationTable.SaturationDeleteValue.ToString());
                }
                else if (col_delete.ColumnType == typeof(bool))
                {
                    sb.AppendFormat(
                        " AND {0} {1} {2}",
                        col_delete.ColumnName,
                        Comparators.GetNegatedComparator(rel.RelationTable.SaturationDeleteComparator),
                        ((bool)rel.RelationTable.SaturationDeleteValue) ? 1 : 0);
                }
                else
                {
                    sb.AppendFormat(
                        " AND {0} {1} {2}", 
                        col_delete.ColumnName, 
                        Comparators.GetNegatedComparator(rel.RelationTable.SaturationDeleteComparator), 
                        rel.RelationTable.SaturationDeleteValue);
                }
            }
            //handle parent relation
            StringBuilder parent_keys = new StringBuilder();
            foreach (var item in parents)
            {
                object parent_key = item.GetType().GetField(rel.ParentField).GetValue(item);
                if (parent_keys.Length == 0)
                    parent_keys.AppendFormat("{0}", parent_key);
                else
                    parent_keys.AppendFormat(",{0}", parent_key);
            }
            IColumnMetadata col_parent = rel.RelationTable.ColumnsMetadata.Values.Where(c => c.FieldMapping == rel.ParentRelationshipField).FirstOrDefault();
            sb.AppendFormat(" AND {0} in ({1})", col_parent.ColumnName, parent_keys.ToString());

            //order by parent key
            sb.AppendFormat(" ORDER BY {0}", col_parent.ColumnName);
            
            return sb.ToString();
        }

        /// <summary>
        /// return if we can saturate this table
        /// </summary>
        /// <returns></returns>
        public override bool CanSaturate()
        {
            //parse the relationship in which we are the relation table
            foreach (var rel in Catalog.CatalogMetadata.Relationships.Values.Where(r => r.RelationTableName == TableMetadata.TableName).ToList())
            {
                //get the parent table for this bucket
                IBucketDataTable parent = Bucket.BucketDataTables[rel.ParentTableName];

                //check that it has saturated
                if (!parent.Saturated)
                    return false;
            }
            return true;
        }

        #endregion

        //IBucket IBucketDataTable.Bucket
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
    }
}
