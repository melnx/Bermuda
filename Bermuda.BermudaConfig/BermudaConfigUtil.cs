using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Bermuda.Interface;
using Bermuda.Catalog;

namespace Bermuda.BermudaConfig
{
    public static class BermudaConfigUtil
    {
        public static ObservableCollection<string> GetSystemTypes()
        {
            ObservableCollection<string> SystemTypes = new ObservableCollection<string>();
            SystemTypes.Add("System.Char");
            SystemTypes.Add("System.Byte");
            SystemTypes.Add("System.Int16");
            SystemTypes.Add("System.Int32");
            SystemTypes.Add("System.Int64");
            SystemTypes.Add("System.Double");
            SystemTypes.Add("System.Single");
            SystemTypes.Add("System.Decimal");
            SystemTypes.Add("System.String");
            SystemTypes.Add("System.Single");
            SystemTypes.Add("System.DateTime");
            SystemTypes.Add("System.Boolean");
            SystemTypes.Add("IdCollection");
            return SystemTypes;
        }

        public static ObservableCollection<string> GetComparators()
        {
            ObservableCollection<string> Comparators = new ObservableCollection<string>();
            Comparators.Add(Constants.Comparators.EQUAL);
            Comparators.Add(Constants.Comparators.NOT_EQUAL);
            Comparators.Add(Constants.Comparators.GREATER_THAN);
            Comparators.Add(Constants.Comparators.GREATER_THAN_EQUAL_TO);
            Comparators.Add(Constants.Comparators.LESS_THAN);
            Comparators.Add(Constants.Comparators.LESS_THAN_EQUAL_TO);
            return Comparators;
        }

        public static ObservableCollection<string> GetBoolValues()
        {
            ObservableCollection<string> BoolValues = new ObservableCollection<string>();
            BoolValues.Add("True");
            BoolValues.Add("False");
            return BoolValues;
        }

        public static ObservableCollection<string> GetPurgeOperations()
        {
            ObservableCollection<string> PurgeOperations = new ObservableCollection<string>();
            PurgeOperations.Add(Constants.PurgeOperations.PURGE_OP_NONE);
            PurgeOperations.Add(Constants.PurgeOperations.PURGE_OP_SMALLEST);
            PurgeOperations.Add(Constants.PurgeOperations.PURGE_OP_LARGEST);
            return PurgeOperations;
        }

        public static ObservableCollection<string> GetConnectionTypes()
        {
            ObservableCollection<string> ConnectionTypes = new ObservableCollection<string>();
            ConnectionTypes.Add("SQL Server");
            ConnectionTypes.Add("Oracle");
            ConnectionTypes.Add("ODBC");
            ConnectionTypes.Add("File System");
            ConnectionTypes.Add("Amazon S3");
            return ConnectionTypes;
        }

        public static bool CopyCatalog(ICatalog from, ICatalog to)
        {
            if(!CopyProperties(from, to)) return false;
            to.CatalogMetadata = new CatalogMetadata(to);
            if (!CopyProperties(from.CatalogMetadata, to.CatalogMetadata)) return false;
            to.CatalogMetadata.Catalog = to;
            to.CatalogMetadata.Tables = new Dictionary<string, ITableMetadata>();
            from.CatalogMetadata.Tables.Values.ToList().ForEach(t =>
                {
                    ITableMetadata table = new TableMetadata(to.CatalogMetadata);
                    CopyTable(t, table);
                    to.CatalogMetadata.Tables.Add(table.TableName, table);
                });
            to.CatalogMetadata.Relationships = new Dictionary<string, IRelationshipMetadata>();
            from.CatalogMetadata.Relationships.Values.ToList().ForEach(r =>
                {
                    IRelationshipMetadata rel = new RelationshipMetadata(to.CatalogMetadata);
                    CopyRelationship(r, rel);
                    to.CatalogMetadata.Relationships.Add(rel.RelationshipName, rel);
                });
            return true;
        }

        public static bool CopyRelationship(IRelationshipMetadata from, IRelationshipMetadata to)
        {
            var parent = to.CatalogMetadata;
            if (!CopyProperties(from, to)) return false;
            to.CatalogMetadata = parent;
            return true;
        }

        public static bool CopyTable(ITableMetadata from, ITableMetadata to)
        {
            var parent = to.CatalogMetadata;
            if (!CopyProperties(from, to)) return false;
            to.CatalogMetadata = parent;
            to.ColumnsMetadata = new Dictionary<string, IColumnMetadata>();
            from.ColumnsMetadata.Values.ToList().ForEach(c =>
                {
                    IColumnMetadata col = new ColumnMetadata(to);
                    CopyColumn(c, col);
                    to.ColumnsMetadata.Add(col.ColumnName, col);
                });
            return true;
        }

        public static bool CopyColumn(IColumnMetadata from, IColumnMetadata to)
        {
            var parent = to.TableMetadata;
            if (!CopyProperties(from, to)) return false;
            to.TableMetadata = parent;
            return true;
        }

        public static bool CopyProperties(object from, object to)
        {
            //check if same type
            if (from.GetType() != to.GetType())
                return false;

            try
            {
                foreach (var p in from.GetType().GetProperties())
                {
                    p.SetValue(to, p.GetValue(from, new object[] { }), new object[] { });
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
