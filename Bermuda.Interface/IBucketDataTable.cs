using System;
using System.Collections.Generic;
namespace Bermuda.Interface
{
    public interface IBucketDataTable : IReferenceDataTable
    {
        IBucket Bucket { get; set; }
        string GetRelationshipQuery(IRelationshipMetadata rel, List<IDataItem> parents);
    }
}
