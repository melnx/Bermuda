using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Data.SqlClient;
using System.Data;
using Bermuda.Constants;
using System.Runtime.Serialization;
using EvoApp.Util;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace Bermuda.Catalog
{
    
    
    [KnownType(typeof(ColumnMetadata))]
    [KnownType(typeof(TableMetadata))]
    [KnownType(typeof(RelationshipMetadata))]
    [KnownType(typeof(CatalogMetadata))]
    [KnownType(typeof(Catalog))]
    [DataContract]
    public class ComputeNode : IComputeNode
    {
        public Dictionary<string, IDataProvider> Catalogs
        {
            get
            {
                if(_Catalogs == null)
                    _Catalogs = new Dictionary<string, IDataProvider>();
                return _Catalogs;
            }
            set
            {
                _Catalogs = value;
            }
        }

        #region Variables and Properties

        /// <summary>
        /// the static instance of the compute node
        /// </summary>
        public static IComputeNode Node { get; set; }

        /// <summary>
        /// the index of this compute node instance
        /// </summary>
        public Int64 ComputeNodeIndex { get; set; }

        /// <summary>
        /// the total bucket count across all nodes
        /// </summary>
        [DataMember]
        public Int64 GlobalBucketCount { get; set; }

        /// <summary>
        /// the number of instances deployed
        /// </summary>
        public Int64 ComputeNodeCount { get; set; }

        /// <summary>
        /// the minimum amount of memory for purging and saturation
        /// </summary>
        [DataMember]
        public Int64 MaxAvailableMemoryPercent { get; set; }

        /// <summary>
        /// the maximum amount of memory for purging and saturation
        /// </summary>
        [DataMember]
        public Int64 MinAvailableMemoryPercent { get; set; }

        /// <summary>
        /// the compute node is purging
        /// </summary>
        public bool Purging { get; set; }

        /// <summary>
        /// the collection of catalogs
        /// </summary>
        [DataMember]
        private Dictionary<string, IDataProvider> _Catalogs { get; set; }

        /// <summary>
        /// the list of saturation tables
        /// </summary>
        public List<IReferenceDataTable> SaturationTables { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor
        /// </summary>
        public ComputeNode()
        {

        }

        /// <summary>
        /// constructor with the compute node index
        /// </summary>
        /// <param name="index"></param>
        public ComputeNode(Int64 index, Int64 bucket_count, Int64 compute_node_count)
        {
            //init
            GlobalBucketCount = bucket_count;
            Init(index, compute_node_count);

            //remove this alltogether
            //this.RefreshMXCatalogs();
        }

        /// <summary>
        /// init the compute node
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bucket_count"></param>
        /// <param name="compute_node_count"></param>
        public void Init(Int64 index, Int64 compute_node_count)
        {
            ComputeNodeIndex = index;
            ComputeNodeCount = compute_node_count;
            Node = this;
            SaturationTables = new List<IReferenceDataTable>();
            Catalogs.Values.Cast<ICatalog>().ToList().ForEach(c => c.InitializeFromMetadata());
            Catalogs.Values.Cast<ICatalog>().Where(c => c.ConnectionType == ConnectionTypes.SQLServer).ToList().ForEach(c => SaturationTables.AddRange(c.GetSaturationTables()));
            //RefreshCatalogs();
        }

        #endregion

        #region Methods

        /// <summary>
        /// serialize the compute node
        /// </summary>
        /// <param name="compute_node"></param>
        /// <returns></returns>
        public string SerializeComputeNode()
        {
            //return JsonUtil.SerializeToJson<ComputeNode>(this);
            string serialized;
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(this.GetType());
                serializer.WriteObject(stream, this);
                serialized = Encoding.Default.GetString(stream.ToArray());
            }

            return serialized;
        }

        /// <summary>
        /// deserialize the compute node
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public IComputeNode DeserializeComputeNode(string json)
        {
            //deserialize to object
            //IComputeNode compute_node = JsonUtil.DeserializeFromJson<ComputeNode>(json);
            IComputeNode compute_node;
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(this.GetType());
                compute_node = (IComputeNode)serializer.ReadObject(jsonReader);
                jsonReader.Close();
            }
            
            //hook up all the parent relations
            foreach (ICatalog catalog in compute_node.Catalogs.Values.ToList())
            {
                catalog.Init(compute_node);
                catalog.CatalogMetadata.Catalog = catalog;
                foreach (ITableMetadata table in catalog.CatalogMetadata.Tables.Values.ToList())
                {
                    table.Init(catalog.CatalogMetadata);
                    foreach (IColumnMetadata column in table.ColumnsMetadata.Values.ToList())
                    {
                        column.Init(table);
                    }
                }
                foreach (IRelationshipMetadata relation in catalog.CatalogMetadata.Relationships.Values.ToList())
                {
                    relation.Init(catalog.CatalogMetadata);
                }
            }
            
            return compute_node;
        }

        /// <summary>
        /// get the catalog by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<IDataProvider> GetCatalogs(string name)
        {
            return Catalogs.Values.Where(c => c.Name.ToLower() == name.ToLower());
        }

        /// <summary>
        /// get all catalog tables
        /// </summary>
        /// <returns></returns>
        public List<IDataTable> GetAllCatalogTables()
        {
            List<IDataTable> tables = new List<IDataTable>();
            foreach (ICatalog catalog in Catalogs.Values.ToList())
                tables.AddRange(catalog.CatalogDataTables.Values);
            return tables;
        }

        /// <summary>
        /// refresh the catalogs
        /// </summary>
        //public void RefreshCatalogs()
        //{
        //    //get the catalogs
        //    var refresh_catalogs = GetCatalogs();

        //    //parse the catalogs
        //    foreach (var catalog in refresh_catalogs)
        //    {
        //        //check if this is new catalog
        //        ICatalog check_catalog;
        //        IDataProvider check_provider;
        //        if (!Catalogs.TryGetValue(catalog.CatalogName, out check_provider))
        //        {
        //            //add the catalog
        //            Catalogs.Add(catalog.CatalogName, (IDataProvider)catalog);
        //            check_catalog = catalog;
        //        }
        //        else
        //            check_catalog = (ICatalog)check_provider;

        //        //initialize the catalog
        //        if (!check_catalog.Initialized)
        //        {
        //            //init the catalog from the meta data definition
        //            check_catalog.InitializeFromMetadata();

        //            //init this catalog for saturation
        //            SaturationTables.AddRange(check_catalog.GetSaturationTables());
        //        }
        //    }
        //}

        /// <summary>
        /// get the list of catalogs to attempt a refresh
        /// </summary>
        /// <returns></returns>
        //protected virtual IEnumerable<ICatalog> GetCatalogs()
        //{
        //    throw new NotImplementedException("IEnumerable<ICatalog> GetCatalogs() is not implemented");
        //    //return RefreshMXCatalogs();
        //}

        #endregion

    }
}
