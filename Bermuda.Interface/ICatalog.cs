using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Constants;

namespace Bermuda.Interface
{
    public interface ICatalog
    {
        void Init(IComputeNode compute_node);
        Dictionary<long, IBucket> Buckets { get; set; }
        Dictionary<string, IDataTable> CatalogDataTables { get; set; }
        ICatalogMetadata CatalogMetadata { get; set; }
        string CatalogName { get; set; }
        IComputeNode ComputeNode { get; set; }
        string ConnectionString { get; set; }
        ConnectionTypes ConnectionType { get; set; }
        List<IReferenceDataTable> GetSaturationTables();
        bool InitializeFromMetadata();
        bool Initialized { get; set; }
    }
}
