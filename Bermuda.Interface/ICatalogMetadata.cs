using System;
using System.Collections.Generic;
namespace Bermuda.Interface
{
    public interface ICatalogMetadata
    {
        //void Init(ICatalog catalog);
        ICatalog Catalog { get; set; }
        Dictionary<string, IRelationshipMetadata> Relationships { get; set; }
        Dictionary<string, ITableMetadata> Tables { get; set; }
    }
}
