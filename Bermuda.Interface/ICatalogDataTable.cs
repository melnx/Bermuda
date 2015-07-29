using System;
namespace Bermuda.Interface
{
    public interface ICatalogDataTable : IDataTable
    {
        ICatalog Catalog { get; set; }
        
    }
}
