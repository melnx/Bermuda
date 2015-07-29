using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.BermudaConfig.Storage
{
    public interface IStorageAccess
    {
        StorageFactory.StorageType StorageType { get; set; }

        bool OpenFileDialog(out string PathName, out string FileName);
        bool SaveFileDialog(out string PathName, out string FileName);

        bool ReadFile(string PathName, string FileName, out string Data);
        bool SaveFile(string Data, string PathName, string FileName);
        
        

    }
}
