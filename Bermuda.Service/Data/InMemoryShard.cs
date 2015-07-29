//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Bermuda.Entities;
//using Bermuda.Entities.Thrift;

//namespace Bermuda
//{
//    public class InMemoryShard : IShard<InMemoryChunk>
//    {
//        public static Dictionary<string, InMemoryChunk> Chunks = new Dictionary<string, InMemoryChunk>();

//        public void InsertData(string chunkId, IEnumerable<ThriftMention> mentions)
//        {
//            //if (!Chunks.ContainsKey(chunkId)) Chunks[chunkId] = new InMemoryChunk();
//            Chunks[chunkId].InsertData(mentions);
//        }

//        public List<ThriftMention> GetMentions(string chunkId, Func<ThriftMention, bool> filter)
//        {
//            return Chunks[chunkId].Data.Where(filter).ToList();
//        }

//        public int GetSize(string chunkId)
//        {
//            return Chunks[chunkId].Data.Count;
//        }

//        string _endpoint;
//        public string Endpoint
//        {
//            get
//            {
//                if (_endpoint == null)
//                {
//                    //_endpoint = AppFabricInterface.Instance.GetEndpointForShard(this);
//                }
//                return _endpoint;
//            }
//        }

//        public IEnumerable<string> MyChunks
//        {
//            get
//            {
//                return MockAppFabricInterface.Instance.GetChunksForShard(Endpoint);
//            }
//        }

//        public int GetChunkCount()
//        {
//            return Chunks.Where(x => MyChunks.Contains(x.Key)).Count();
//        }

//        public List<InMemoryChunk> GetChunks()
//        {
//            return Chunks.Where(x => MyChunks.Contains(x.Key) ).Select(x => x.Value).ToList();
//        }

//        public string CreateChunk()
//        {
//            var guid = Guid.NewGuid().ToString();
//            Chunks.Add(guid, new InMemoryChunk { Id = guid, Rank = ++chunkCount });
//            return guid;
//        }

//        static int chunkCount = 0;
//    }
//}
