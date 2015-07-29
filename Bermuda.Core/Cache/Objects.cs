using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Core.Cache
{
    public class CacheRow
    {
        public Guid Domain;
        public Guid Shard;
        public Guid Chunk;
        public int ItemCount;
        public int Size;
        public bool HasStrongIndex;
        public long MinDate;
        public long MaxDate;
        public DateTime CreatedOn;
    }
}
