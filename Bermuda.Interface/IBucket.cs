using System;
using Bermuda.Interface;
using System.Collections.Generic;

namespace Bermuda.Interface
{
    public interface IBucket
    {
        Dictionary<string, IBucketDataTable> BucketDataTables { get; set; }
        long BucketMod { get; set; }
        ICatalog Catalog { get; set; }
    }
}
