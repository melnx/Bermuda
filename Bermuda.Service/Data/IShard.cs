using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Entities;
using Bermuda.Entities.Thrift;

namespace Bermuda
{
    public interface IShard<ChunkType> where ChunkType : IChunk
    {
        void InsertData(Guid chunkId, IEnumerable<ThriftMention> filter);

        List<ThriftMention> GetMentions(Guid chunkId, Func<ThriftMention, bool> filter);

        Guid CreateChunk();

        int GetSize(Guid chunkId);
    }
}
