//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Linq.Expressions;
//using System.Xml.Linq;
//using ExpressionSerialization;
//using System.Reflection;
//using Bermuda.Entities;
//using Bermuda.Entities.Thrift;
//using Bermuda.Service.MapReduce;

//namespace Bermuda
//{
//    public class MapReducer : IMapReducer
//    {
//        string _endpoint;
//        public string Endpoint
//        {
//            get
//            {
//                if (_endpoint == null)
//                {
//                    _endpoint = MockAppFabricInterface.Instance.GetEndpointForMapReduceServer(this);
//                }
//                return _endpoint;
//            }
//        }

//        public MapReducer()
//        {
//        }

//        static Random rng = new Random();

//        public void InsertMentions(string domain, string shard, string chunk, Mention[] mentions, int remdepth = 1)
//        {
//            if (remdepth > 0)
//            {
//                var subset = mentions.Where(x => (x.CreatedOn - x.OccurredOn).TotalDays <= 1).ToArray();
//                var cacherow = MockAppFabricInterface.Instance.GetAvailableTarget(domain, mentions.Length, true);
//                InsertMentions(domain, cacherow.Shard, cacherow.Chunk, subset, remdepth - 1);

//                subset = mentions.Where(x => (x.CreatedOn - x.OccurredOn).TotalDays > 1).ToArray();
//                cacherow = MockAppFabricInterface.Instance.GetAvailableTarget(domain, mentions.Length, false);
//                InsertMentions(domain, cacherow.Shard, cacherow.Chunk, subset, remdepth - 1);   

//                //var cacherow = AppFabricInterface.Instance.GetAvailableShardEndpoint(domain, mentions.Length, false);
//                //InsertMentions(domain, cacherow.Shard, cacherow.Chunk, mentions, remdepth - 1);
//            }
//            else
//            {
//                AzureInterface.Instance.InsertData(domain, shard, chunk, mentions);
//            }
//        }

//        Dictionary<string, MentionCache> CachedMentions = new Dictionary<string, MentionCache>();
//        Dictionary<string, DatapointCache> CachedDatapoints = new Dictionary<string, DatapointCache>();

//        public Mention[] GetMentions(string domain, string shard, int partitions, string query, DateTime minDate, DateTime maxDate, int remdepth)
//        {
//            if (shard == null || shard.Contains('|'))
//            {
//                //map and partition
//                var metadata = PartitionShardEndpoints(domain, shard, minDate, maxDate);
//                List<Mention[]> result = new List<Mention[]>(metadata.Count());

//                //reduce
//                metadata.AsParallel().ForAll(param => 
//                {
//                    if (param.Reducer.Endpoint == Endpoint)
//                    {
//                        result.Add(GetMentions(domain, string.Join("|", param.Shards), partitions, query, minDate, maxDate, remdepth - 1));
//                    }
//                    else
//                    {
//                        var connection = param.Reducer;
//                        //result.Add(connection.GetMentions(domain, string.Join("|",param.Shards), partitions, query, minDate, maxDate, remdepth - 1));
//                    }
//                });

//                return result.SelectMany(x => x).ToArray();
//            }
//            else
//            {
//                //map
//                var chunks = AzureInterface.Instance.ListChunksForDomainInShard(domain, shard, minDate.Ticks, maxDate.Ticks).ToList();

//                //check cache and return result if all results match
//                var key = domain + ":" + shard + ":" + query + ":" + minDate + ":" + maxDate;
//                MentionCache cachedMentions;
//                if (CachedMentions.TryGetValue(key, out cachedMentions) && !chunks.Any(x => x.UpdatedOn > cachedMentions.CreatedOn.Ticks))
//                {
//                    return cachedMentions.Mentions;
//                }

//                List<List<ThriftMention>> results = new List<List<ThriftMention>>(chunks.Count());

//                //reduce
//                chunks.AsParallel().ForAll(cacheRow =>
//                {
//                    var shardInterface = MockAppFabricInterface.Instance.GetShardInterface(shard);
//                    results.Add( shardInterface.GetMentions(cacheRow.Chunk, GetFilterExpression(query)) );
//                });

//                var result = results.SelectMany(x => x).ToArray();

//                CachedMentions[key] = new MentionCache { Mentions = result, CreatedOn = DateTime.Now };

//                return result;
//            }
//        }

//        public Datapoint[] GetDatapoints(string domain, string shard, int partitions, string query, string mapreduce, string combine, DateTime minDate, DateTime maxDate, int remdepth)
//        {
//            if (shard == null || shard.Contains('|'))
//            {
//                //map and partition
//                var metadata = PartitionShardEndpoints(domain, shard, minDate, maxDate);
//                List<Datapoint[]> results = new List<Datapoint[]>(metadata.Count());

//                //reduce
//                metadata.AsParallel().ForAll(param => 
//                {
//                    if (param.Reducer.Endpoint == Endpoint)
//                    {
//                        results.Add(GetDatapoints(domain, string.Join("|", param.Shards), partitions, query, mapreduce, combine, minDate, maxDate, remdepth - 1));
//                    }
//                    else
//                    {
//                        //results.Add(param.Reducer.GetDatapoints(domain, string.Join("|",param.Shards), partitions, query, mapreduce, combine, minDate, maxDate, remdepth - 1));
//                    }
//                });

//                var pointGroups = results.SelectMany(x => x).GroupBy(x => new { x.EntityId, x.Timestamp });
//                Datapoint[] result = new Datapoint[pointGroups.Count()];
//                int i = 0;

//                var combineExpression = GetCombineExpression(combine);

//                foreach (var g in pointGroups)
//                {
//                    result[i] = new Datapoint { EntityId = g.Key.EntityId, Timestamp = g.Key.Timestamp, Value = combineExpression(g) };
//                    ++i;
//                }

//                return result;
//            }
//            else
//            {
//                var key = domain + ":" + shard + ":" + query + ":" + minDate + ":" + maxDate;
//                DatapointCache cachedDatapoints;
//                if (CachedDatapoints.TryGetValue(key, out cachedDatapoints))
//                {
//                    return cachedDatapoints.Datapoints;
//                }

//                var list = GetMentions(domain, shard, partitions, query, minDate, maxDate, 0);

//                var result = GetMapReduceExpression(mapreduce)(list).ToArray();

//                CachedDatapoints[key] = new DatapointCache { Datapoints = result, CreatedOn = DateTime.Now };

//                return result;
//            }
//        }

//        private static IEnumerable<ZipMetadata> PartitionShardEndpoints(string domain, string shard, DateTime minDate, DateTime maxDate)
//        {
//            var shardEndpoints = shard == null ? MockAppFabricInterface.Instance.ListShardEndpointsForDomain(domain, minDate, maxDate) : shard.Split('|');
//            var reducers = MockAppFabricInterface.Instance.GetAvailablePeerConnections(shardEndpoints.Count());

//            List<IEnumerable<string>> partitionedEndpoints = new List<IEnumerable<string>>();
//            int partitionSize = (int)Math.Ceiling((double)shardEndpoints.Count() / (double)reducers.Count());
//            for (int i = 0; i < reducers.Count(); i++)
//            {
//                partitionedEndpoints.Add(shardEndpoints.Skip(i * partitionSize).Take(partitionSize));
//            }

//            return reducers.Zip(partitionedEndpoints, (reducer, shards) => new ZipMetadata { Shards = shards, Reducer = reducer });
//        }

//        private static Func<Mention, bool> GetFilterExpression(string xml)
//        {
//            var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Entities.Mention)) }));
//            return serializer.Deserialize<Func<Mention, bool>>(XElement.Parse(xml)).Compile(); 
//        }

//        private static Func<IEnumerable<Mention>, IEnumerable<Datapoint>> GetMapReduceExpression(string xml)
//        {
//            var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Entities.Mention)) }));
//            return serializer.Deserialize<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>>(XElement.Parse(xml)).Compile();
//        }

//        private static Func<IEnumerable<Datapoint>, double> GetCombineExpression(string xml)
//        {
//            var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Entities.Mention)) }));
//            return serializer.Deserialize<Func<IEnumerable<Datapoint>, double>>(XElement.Parse(xml)).Compile();
//        }

//        class ZipMetadata
//        {
//            public IEnumerable<string> Shards;
//            public MapReducerConnection Reducer;
//        }

//        class DatapointCache
//        {
//            public Datapoint[] Datapoints;
//            public DateTime CreatedOn;
//        }

//        class MentionCache
//        {
//            public Mention[] Mentions;
//            public DateTime CreatedOn;
//        }
//    }
//}
