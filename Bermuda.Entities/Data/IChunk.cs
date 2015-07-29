using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Entities;
using Bermuda.Entities.Thrift;

namespace Bermuda
{
    public interface IChunk
    {
        void InsertData(IEnumerable<ThriftMention> mentions);
        List<ThriftMention> GetData();
    }
}
