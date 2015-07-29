using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Entities;
using System.Diagnostics;
using Bermuda.Entities.Thrift;
using System.Collections.Concurrent;

namespace Bermuda
{
    public class AzureShardInterface : IShard<InMemoryChunk>
    {
        public static ConcurrentDictionary<Guid, InMemoryChunk> Chunks = new ConcurrentDictionary<Guid, InMemoryChunk>();

        public InMemoryChunk GetChunkInterface(Guid chunkId)
        {
            InMemoryChunk chunk = null;
            if (!Chunks.TryGetValue(chunkId, out chunk))
            {
                chunk = new InMemoryChunk{ Id = chunkId };
                Chunks[chunkId] = chunk;
            }
            return chunk;
        }

        public void InsertData(Guid chunkId, IEnumerable<ThriftMention> mentions)
        {
            GetChunkInterface(chunkId).InsertData(mentions);
        }

        public List<ThriftMention> GetMentions(Guid chunkId, Func<ThriftMention, bool> filter)
        {
            var data = GetChunkInterface(chunkId).GetData();
            return data.Where(filter).ToList();
        }

        public int GetSize(Guid chunkId)
        {
            return GetChunkInterface(chunkId).Data.Count;
        }

        public Guid CreateChunk()
        {
            var guid = Guid.NewGuid();
            var chunk =  new InMemoryChunk { Id = guid };
            chunk.PersistData();
            Chunks[guid] = chunk;
            return guid;
        }

        public Guid ShardId
        {
            get;
            set;
        }

    }
}
