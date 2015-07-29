using System;

namespace Bermuda.Interface
{
    public interface IReferenceDataTable: IDataTable
    {
        ICatalog Catalog { get; set; }
        string ConstructQuery();
        bool IsDeleted(object item);
        DateTime LastSaturation { get; set; }
        object LastUpdateValue { get; set; }
        DateTime NextSaturation { get; }
        bool Saturating { get; set; }
        bool UpdateLastValue(object obj);
        bool Saturated { get; set; }
        bool CanSaturate();
    }
}
