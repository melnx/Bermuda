using System;
using System.Collections;

namespace Bermuda.Interface
{
    public interface IDataTable
    {
        object AddOrUpdate(object key, object item);
        IDictionary DataItems { get; set; }
        ITableMetadata TableMetadata { get; set; }
        bool TryGetValue(object key, Type itemType, out object item);
        bool TryRemove(object key, Type itemType, out object item);
        object GetValuesInParallel();
        object GetPurgeItems();
        bool Purging { get; set; }
        DateTime LastPurge { get; set; }
        void DeleteItem(IDataItem item, bool HardDelete);
        bool AddItem(IDataItem item);
    }
}
