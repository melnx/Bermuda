using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using Bermuda.Constants;
using System.Runtime.Serialization;

namespace Bermuda.Catalog
{
    [DataContract]
    public class Catalog : ICatalog, IDataProvider 
    {
        #region Varialbes and Properties

        /// <summary>
        /// the parent compute node for catalog
        /// </summary>
        public IComputeNode ComputeNode { get; set; }

        /// <summary>
        /// the name of the catalog
        /// </summary>
        [DataMember]
        public string CatalogName { get; set; }

        /// <summary>
        /// the type of DB connection
        /// </summary>
        [DataMember]
        public ConnectionTypes ConnectionType { get; set; }

        /// <summary>
        /// the connection string for the catalog data
        /// </summary>
        [DataMember]
        public string ConnectionString { get; set; }

        /// <summary>
        /// the metadata to describe the catalog
        /// </summary>
        [DataMember]
        public ICatalogMetadata CatalogMetadata { get; set; }

        /// <summary>
        /// the global collections of data
        /// </summary>
        public Dictionary<string, IDataTable> CatalogDataTables { get; set; }

        /// <summary>
        /// The bucket collections
        /// </summary>
        public Dictionary<Int64, IBucket> Buckets { get; set; }

        /// <summary>
        /// this catalog has been initializes
        /// </summary>
        public bool Initialized { get; set; }

        #endregion

        #region Construtor

        /// <summary>
        /// the constructor with parent
        /// </summary>
        public Catalog(IComputeNode compute_node)
        {
            Init(compute_node);
        }

        /// <summary>
        /// init the node
        /// </summary>
        /// <param name="compute_node"></param>
        public void Init(IComputeNode compute_node)
        {
            ComputeNode = compute_node;
            CatalogDataTables = new Dictionary<string, IDataTable>();
            Buckets = new Dictionary<long, IBucket>();
            Initialized = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the buckets and catalog data table from metadata
        /// </summary>
        /// <returns></returns>
        public bool InitializeFromMetadata()
        {
            try
            {
                //catalog tables
                foreach (ITableMetadata table_metadata in CatalogMetadata.Tables.Values)
                {
                    //create our catalog level table
                    DataTable table;
                    if (table_metadata.ReferenceTable)
                        table = new ReferenceDataTable(this, table_metadata);
                    else
                        table = new CatalogDataTable(this, table_metadata);
                    
                    //add to the catalog
                    CatalogDataTables.Add(table_metadata.TableName, table);
                }
                //buckets
                for (int x = 0; x < ComputeNode.GlobalBucketCount; x++)
                {
                    //check for our mod
                    if (x % ComputeNode.ComputeNodeCount == ComputeNode.ComputeNodeIndex)
                    {
                        //make a bucket
                        Bucket bucket = new Bucket(this);
                        Buckets.Add(x, bucket);
                        bucket.BucketMod = x;

                        //parse the metadata
                        foreach (ITableMetadata table_metadata in CatalogMetadata.Tables.Values.Where(t => t.ReferenceTable == false))
                        {
                            //create our bucket level table
                            IRelationshipMetadata relationship_metadata = CatalogMetadata.Relationships.Values.Where(r => r.RelationTable == table_metadata).FirstOrDefault();
                            IBucketDataTable table;
                            if (relationship_metadata != null)
                                table = new RelationshipDataTable(bucket, table_metadata, relationship_metadata);
                            else
                                table = new BucketDataTable(bucket, table_metadata);
                            
                            //add to our bucket
                            bucket.BucketDataTables.Add(table_metadata.TableName, table);
                        }
                    }
                }
                //mark as initialized
                Initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// get the tables to saturate
        /// </summary>
        /// <returns></returns>
        public List<IReferenceDataTable> GetSaturationTables()
        {
            List<IReferenceDataTable> SaturationTables = new List<IReferenceDataTable>();
            DateTime now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);

            int NextSaturationOffset = -1000000;
            foreach (Bucket bucket in Buckets.Values)
            {
                foreach (IReferenceDataTable table in bucket.BucketDataTables.Values)
                {
                    table.LastSaturation = now.AddSeconds(NextSaturationOffset);
                    SaturationTables.Add(table);
                    NextSaturationOffset++;
                }
            }
            foreach (IReferenceDataTable table in CatalogDataTables.Values.OfType<IReferenceDataTable>())
            {
                table.LastSaturation = now.AddSeconds(NextSaturationOffset);
                SaturationTables.Add(table);
                NextSaturationOffset++;
            }
            return SaturationTables;
        }

        #endregion

        #region IDataProvider Implementation

        public long GetCount(string col)
        {
            if (col == null)
            {
                var result = 0L;
                foreach (var t in CatalogDataTables.Values)
                {
                    result += t.DataItems.Values.Count;
                }
                return result;
            }

            var tab = GetTableByName(col);

            return tab.DataItems.Values.Count;
        }

        private IDataTable GetTableByName(string tableName)
        {
            if (tableName == null) return CatalogDataTables["Mentions"];

            var tab = CatalogDataTables.Values.Where(x => string.Equals(x.TableMetadata.TableName, tableName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (tab == null) throw new Exception("Table " + tableName + " not found");
            return tab;
        }

        public object GetData(string col)
        {
            return GetTableByName(col).GetValuesInParallel();
            //return CatalogDataTables["Mentions"].DataItems.Values;
        }

        public Type GetDataType(string col)
        {
            return GetTableByName(col).TableMetadata.DataType;
        }

        public string Name
        {
            get
            {
                return CatalogName;
            }
            set
            {
                CatalogName = value;
            }
        }

        public string Id
        {
            get
            {
                return CatalogName;
            }
            set
            {
                
            }
        }

        #endregion
    }
}
