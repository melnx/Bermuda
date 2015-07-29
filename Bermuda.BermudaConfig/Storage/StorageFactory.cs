using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.BermudaConfig.Storage.Local;
using Bermuda.BermudaConfig.Storage.Amazon;
using Bermuda.BermudaConfig.Storage.Azure;

namespace Bermuda.BermudaConfig.Storage
{
    public static class StorageFactory
    {
        public static IStorageAccess CreateStorageAccess(StorageType type)
        {
            switch(type)
            {
                case StorageType.Local:
                    return new StorageLocal(type);
                case StorageType.Amazon:
                    return new StorageAmazon(type);
                case StorageType.Azure:
                    return new StorageAzure(type);
                default:
                    return null;
            }
        }

        public enum StorageType
        {
            Unknown = 0,
            Local = 1,
            Amazon = 2,
            Azure = 3
        }
    }
}
