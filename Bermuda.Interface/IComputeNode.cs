using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Interface
{
    public interface IComputeNode
    {
        void Init(Int64 index, Int64 compute_node_count);
        Dictionary<string, IDataProvider> Catalogs { get; set; }
        long ComputeNodeCount { get; set; }
        long ComputeNodeIndex { get; set; }
        IEnumerable<IDataProvider> GetCatalogs(string name);
        long GlobalBucketCount { get; set; }
        //ICatalog InitializeMXCatalog(string Name, string ConnectionString);
        //void RefreshCatalogs();
        List<IDataTable> GetAllCatalogTables();
        List<IReferenceDataTable> SaturationTables { get; set; }
        string SerializeComputeNode();
        IComputeNode DeserializeComputeNode(string json);
        Int64 MaxAvailableMemoryPercent { get; set; }
        Int64 MinAvailableMemoryPercent { get; set; }
        bool Purging { get; set; }
    }
}
