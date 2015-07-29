using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Xml.Linq;
using ExpressionSerialization;
using System.Reflection;
using Bermuda.Entities;
using System.Collections.Concurrent;
using Bermuda.Entities.Thrift;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Net;
using System.Diagnostics;
using Bermuda.Service.Data;
using Bermuda.Service.BermudaPeer;
using Bermuda.DomainLayer;
using Bermuda.Service.Util;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Reflection.Emit;
using Bermuda.Entities.ExpressionGeneration;

namespace Bermuda
{
    public class AzureMapReducer : IMapReducer
    {
        public static readonly string XmlHeader = "__expression_xml__";
        public static readonly string QlHeader = "__ql__";
        public static readonly string DefaultToken = "__default__";

        static readonly long StrongIndexDistance = TimeSpan.FromDays(1).Ticks;
        static readonly long CacheLifetime = TimeSpan.FromSeconds(60).Ticks;
        static readonly int MaxMachinesPerQuery = 9000;

        //0:individual blobs   1:blob subsets   2:whole blob sets
        static readonly int CacheTraceMessageLevel = 2;

        static AzureMapReducer _instance;
        public static AzureMapReducer Instance
        {
            get
            {
                return _instance ?? (_instance = new AzureMapReducer());
            }
        }

        private AzureMapReducer()
        {
         
        }

        public IPEndPoint Endpoint
        {
            get
            {
                return AzureInterface.Instance.CurrentEndpoint.IPEndpoint;
            }
        }

        static Random rng = new Random();

        public void InsertMentions(string domain, List<ThriftMention> mentions, int remdepth = 1)
        {
            if (remdepth > 0)
            {
                var stronglyIndexedData = mentions.Where(x => Math.Abs(x.CreatedOnTicks - x.OccurredOnTicks) <= StrongIndexDistance).ToList();
                //var cacherow = AzureInterface.Instance.GetAvailableTarget( domain, mentions.Count, true);
                if (stronglyIndexedData.Count > 0)
                {
                    AzureInterface.Instance.InsertData(domain, stronglyIndexedData, true);
                }

                var weaklyIndexedData = mentions.Where(x => Math.Abs(x.CreatedOnTicks - x.OccurredOnTicks) > StrongIndexDistance).ToList();
                //cacherow = AzureInterface.Instance.GetAvailableTarget( domain, mentions.Count, false);
                if (weaklyIndexedData.Count > 0)
                {
                    AzureInterface.Instance.InsertData(domain, weaklyIndexedData, false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("remdepth");
            }
        }

        //ConcurrentDictionary<string, MentionResult> CachedMentions = new ConcurrentDictionary<string, MentionResult>();
        //ConcurrentDictionary<string, DatapointResult> CachedDatapoints = new ConcurrentDictionary<string, DatapointResult>();
        ConcurrentDictionary<string, BermudaResult> CachedData = new ConcurrentDictionary<string, BermudaResult>();

        /*
        public MentionResult GetMentions(string domain, IEnumerable<string> blobs, string query, string paging, DateTime minDate, DateTime maxDate, int remdepth, object[] parameters, string command)
        {
            var args = ParseCommand(command);

            if ( remdepth > 0 )
            {
                //map 
                var blobInterfaces = blobs == null ? AzureInterface.Instance.ListBlobs(domain, minDate.Ticks, maxDate.Ticks) : AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);
                
                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, paging, minDate, maxDate, parameters, null);

                //check cache
                MentionResult cachedMentions;
                if (CachedMentions.TryGetValue(blobSetKey, out cachedMentions) && (DateTime.Now.Ticks - cachedMentions.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 3) Trace.WriteLine("returned CACHED BLOB SET MENTION LIST results");
                    return new MentionResult { Mentions = cachedMentions.Mentions, Metadata = new BermudaNodeStatistic { Notes = "Cache Hit" } };
                }
                else
                {
                    var assignments = PartitionBlobs(domain, blobInterfaces, minDate, maxDate, false, true);

                    //reduce
                    ConcurrentDictionary<IPEndPoint, MentionResult> results = new ConcurrentDictionary<IPEndPoint, MentionResult>();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    List<Task> tasks = new List<Task>();
                    foreach (var ass in assignments)
                    {
                        Task t = new Task((assObj) =>
                            {
                                ZipMetadata assignment = assObj as ZipMetadata;
                                var initiated = DateTime.Now;

                                var blobSubsetKey = GetQueryChecksum(domain, string.Join(",", assignment.Blobs.Select(x => x.Name)), query, paging, minDate, maxDate, parameters, assignment.PeerEndpoint.ToString());

                                //see if the cache contains a matching result and return it if it's not outdated
                                MentionResult cachedMentions2;
                                if (CachedMentions.TryGetValue(blobSubsetKey, out cachedMentions2) && (DateTime.Now.Ticks - cachedMentions2.CreatedOn) < CacheLifetime)
                                {
                                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB SUBSET MENTION LIST results FOR BLOB SUBSET [REMDEPTH:" + remdepth + "]");
                                    results[assignment.PeerEndpoint] = new MentionResult { Mentions = cachedMentions2.Mentions, Metadata = new BermudaNodeStatistic { Notes = "Cache Hit" } };
                                }
                                else
                                {
                                    try
                                    {
                                        Stopwatch sw2 = new Stopwatch();
                                        sw2.Start();
                                        MentionResult subresult = null;

                                        if (assignment.PeerEndpoint == Endpoint)
                                        {
                                            subresult = GetMentions(domain, assignment.Blobs.Select(x => x.Name), query, paging, minDate, maxDate, remdepth - 1, parameters, command);
                                        }
                                        else
                                        {
                                            using (var client = AzureInterface.Instance.GetServiceClient(assignment.PeerEndpoint))
                                            {
                                                client.Open();
                                                subresult = client.GetMentionList(domain, assignment.Blobs.Select(x => x.Name), query, paging, minDate, maxDate, remdepth - 1, parameters, command);
                                                client.Close();
                                            }
                                        }

                                        sw2.Stop();
                                        subresult.CreatedOn = DateTime.Now.Ticks;
                                        subresult.Metadata.Initiated = initiated;
                                        subresult.Metadata.Completed = DateTime.Now;
                                        subresult.Metadata.OperationTime = sw2.Elapsed;
                                        results[assignment.PeerEndpoint] = CachedMentions[blobSubsetKey] = subresult;
                                    }
                                    catch (Exception ex)
                                    {
                                        results[assignment.PeerEndpoint] = new MentionResult { Metadata = new BermudaNodeStatistic { Error = "[Failed Node] " + ex.Message } };
                                    }
                                }
                            },
                            ass,
                            TaskCreationOptions.LongRunning
                            );


                        tasks.Add(t);
                        t.Start();
                    }

                    Task.WaitAll(tasks.ToArray());

                    sw.Stop();

                    var pagingFunc = GetPagingFunc(paging);
                    var mentions = results.Values.Where(x => x.Mentions != null).SelectMany(x => x.Mentions);
                    var pagedMentions = pagingFunc(mentions);
                    var finalMentions = pagedMentions.ToList();
                    var finalMetadata = new BermudaNodeStatistic { Notes = "Merged Mentions", NodeId = AzureInterface.Instance.CurrentInstanceId, ChildNodes = results.Values.Select(x => x.Metadata).ToArray() };
                    var finalResult = CachedMentions[blobSetKey] = new MentionResult { Mentions = finalMentions, Metadata = finalMetadata, CreatedOn = DateTime.Now.Ticks };
                    return finalResult;
                }
            }
            else
            {
                ConcurrentDictionary<string, MentionResult> results = new ConcurrentDictionary<string, MentionResult>();

                var blobInterfaces = AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);

                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, paging, minDate, maxDate, parameters, Endpoint.ToString());

                BermudaNodeStatistic stats = new BermudaNodeStatistic();

                 //check cache
                MentionResult cachedMentions;
                if (CachedMentions.TryGetValue(blobSetKey, out cachedMentions) && (DateTime.Now.Ticks - cachedMentions.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB SET MENTION LIST results");
                    return new MentionResult { Mentions = cachedMentions.Mentions, Metadata = cachedMentions.Metadata.Clone() };
                }
                else
                {
                    var queryFunc = GetFilterFunc(query);
                    var pagingFunc = GetPagingFunc(paging);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    IEnumerable<Mention> mentions = null;

                    foreach( var blob in blobInterfaces )
                    {
                        //check cache and return result if all results match
                        var blobKey = GetQueryChecksum(domain, blob.Name, query, paging, minDate, maxDate, parameters, Endpoint.ToString());

                        //see if the cache contains a matching result and return it if it's not outdated
                        //MentionResult cachedMentions2;
                        //if (CachedMentions.TryGetValue(blobKey, out cachedMentions2) && (DateTime.Now.Ticks - cachedMentions2.CreatedOn) < CacheLifetime)
                        //{
                        //    results[blob.Name] = new MentionResult { Mentions = cachedMentions2.Mentions, Metadata = cachedMentions2.Metadata.Clone() };
                        //    mentions = cachedMentions2.Mentions;
                        //    if (CacheTraceMessageLevel < 1) Trace.WriteLine("returned CACHED BLOB MENTION LIST results");
                        //}
                        //else
                        //{
                            var raw = blob.GetData();

                            var minDateTicks = minDate.Ticks;
                            var maxDateTicks = maxDate.Ticks;

                            //var subresult = raw.Where(x => x.OccurredOnTicks >= minDateTicks && x.OccurredOnTicks <= maxDateTicks && queryFunc(x, parameters));
                            var subresult =
                                queryFunc == null ?
                                raw :
                                minDate == DateTime.MinValue && maxDate == DateTime.MaxValue ?
                                raw.AsParallel().Where(x => queryFunc(x, parameters)) :
                                raw.AsParallel().Where(x => x.OccurredOnTicks >= minDateTicks && x.OccurredOnTicks <= maxDateTicks && queryFunc(x, parameters));


                            var pagedsubresult = pagingFunc(subresult);


                            if (!args.Contains("-nocount"))
                            {
                                stats.TotalItems = raw.Count();
                                //stats.FilteredItems = subresult.Count();
                            }

                            mentions = pagedsubresult;
                            results[blob.Name] = new MentionResult { Mentions = pagedsubresult, Metadata = stats, CreatedOn = DateTime.Now.Ticks };
                            //CachedMentions[blobKey] = new MentionResult { Mentions = pagedsubresult.ToList(), Metadata = stats, CreatedOn = DateTime.Now.Ticks };
                        //}
                    }

                    //var mentions = results.Values.SelectMany(x => x.Mentions);
                    if (mentions == null) return new MentionResult();

                    stats.NodeId = AzureInterface.Instance.CurrentInstanceId;
                    stats.Notes = "Computed Mentions";

                    //var pagedMentions = pagingFunc(mentions);
                    var finalMentions = mentions; //pagedMentions;
                    var finalMentionsList = finalMentions.ToList();
                    sw.Stop();

                    stats.LinqExecutionTime = sw.Elapsed;

                    var finalResult = new MentionResult { Mentions = finalMentions, Metadata = stats, CreatedOn = DateTime.Now.Ticks };
                    CachedMentions[blobSetKey] = new MentionResult { Mentions = finalMentionsList, Metadata = stats, CreatedOn = DateTime.Now.Ticks };

                    return finalResult;
                }
            }
        }*/

        private IEnumerable<string> ParseCommand(string command)
        {
            if( command == null ) return new string[0];
            return command.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
        }

        //public DatapointResult GetDatapoints(string domain, Expression<Func<ThriftMention, object[], bool>> query, Expression<Func<IEnumerable<ThriftMention>, IEnumerable<ThriftDatapoint>>> mapreduce, Expression<Func<IEnumerable<ThriftDatapoint>, double>> merge, DateTime minDate, DateTime maxDate, object[] parameters, string command)
        //{
        //    ExpressionSerializer serializer = new ExpressionSerializer();

        //    string queryStr = serializer.Serialize(query).ToString();

        //    string mapreduceStr = serializer.Serialize(mapreduce).ToString();

        //    string mergeStr = serializer.Serialize(merge).ToString();

        //    var datapoints = AzureMapReducer.Instance.GetDatapoints(domain, null, queryStr, mapreduceStr, mergeStr, minDate, maxDate, 1, parameters, command);

        //    return datapoints;
        //}

        /*
        public DatapointResult GetDatapoints(string domain, IEnumerable<string> blobs, string query, string mapreduce, string merge, DateTime minDate, DateTime maxDate, int remdepth, object[] parameters, string command)
        {
            var args = ParseCommand(command);

            if ( remdepth > 0)
            {
                //map
                var blobInterfaces = blobs == null ? AzureInterface.Instance.ListBlobs(domain, minDate.Ticks, maxDate.Ticks) : AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);
 
                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, null);

                //reduce 
                DatapointResult cachedDatapoints;
                if (CachedDatapoints.TryGetValue(blobSetKey, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime )
                {
                    if (CacheTraceMessageLevel < 3) Trace.WriteLine("returned CACHED BLOBS DATAPOINTS results FOR ENTIRE BLOB SET [REMDEPTH:" + remdepth + "]");
                    return new DatapointResult { Datapoints = cachedDatapoints.Datapoints, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_1" } };
                }
                else
                {

                    var assignments = PartitionBlobs(domain, blobInterfaces, minDate, maxDate, false, true);

                    ConcurrentDictionary<IPEndPoint, DatapointResult> results = new ConcurrentDictionary<IPEndPoint, DatapointResult>();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    List<Task> tasks = new List<Task>();
                    foreach (var ass in assignments)
                    {
                        Task t = new Task((assObj) =>
                            {
                                ZipMetadata assignment = assObj as ZipMetadata;
                                var initiated = DateTime.Now;
                                var blobSubsetKey = GetQueryChecksum(domain, string.Join(",", assignment.Blobs.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, assignment.PeerEndpoint.ToString());
                                Stopwatch sw3 = new Stopwatch();
                                sw3.Start();

                                //see if the cache contains a matching result and return it if it's not outdated
                                DatapointResult cachedDatapoints2;
                                if (CachedDatapoints.TryGetValue(blobSubsetKey, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                                {
                                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB DATAPOINT results FOR BLOB SUBSET [REMDEPTH:" + remdepth + "]");
                                    results[assignment.PeerEndpoint] = new DatapointResult { Datapoints = cachedDatapoints2.Datapoints, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_2" } };
                                }
                                else
                                {
                                    try
                                    {
                                        Stopwatch sw2 = new Stopwatch();
                                        sw2.Start();
                                        DatapointResult subresult = null;

                                        if (assignment.PeerEndpoint.Equals(Endpoint))
                                        {
                                            subresult = GetDatapoints(domain, assignment.Blobs.Select(x => x.Name), query, mapreduce, merge, minDate, maxDate, remdepth - 1, parameters, command);

                                        }
                                        else
                                        {
                                            using (var client = AzureInterface.Instance.GetServiceClient(assignment.PeerEndpoint))
                                            {
                                                client.Open();
                                                subresult = client.GetDatapointList(domain, assignment.Blobs.Select(x => x.Name), query, mapreduce, merge, minDate, maxDate, remdepth - 1, parameters, command);
                                                client.Close();
                                            }
                                        }

                                        sw2.Stop();
                                        subresult.CreatedOn = DateTime.Now.Ticks;
                                        subresult.Metadata.Initiated = initiated;
                                        subresult.Metadata.Completed = DateTime.Now;
                                        subresult.Metadata.OperationTime = sw2.Elapsed;
                                        results[assignment.PeerEndpoint] = CachedDatapoints[blobSubsetKey] = subresult;
                                    }
                                    catch (Exception ex)
                                    {
                                        results[assignment.PeerEndpoint] = new DatapointResult { Metadata = new BermudaNodeStatistic { Error = "[Failed Node] " + ex.Message } };
                                    }
                                }
                            },
                            ass,
                            TaskCreationOptions.LongRunning
                            );

                        tasks.Add(t);
                        t.Start();
                    }

                    Task.WaitAll(tasks.ToArray());

                    sw.Stop();
                    Trace.WriteLine("Join Time:" + sw.Elapsed);

                 
                    //use the passed combine espression to make multiple datapoint sets into one
                    var mergeFunc = GetMergeFunc(merge);

                    var finalDatapoints = MergeDatapoints(results.Values.Where(x => x.Datapoints != null).SelectMany(x => x.Datapoints), mergeFunc);

                    //figure out the metadata
                    var finalMetadata = new BermudaNodeStatistic { Notes = "Merged Datapoints in " + sw.Elapsed, NodeId = AzureInterface.Instance.CurrentInstanceId, ChildNodes = results.Values.Select(x => x.Metadata).ToArray() };

                    CachedDatapoints[blobSetKey] = new DatapointResult { Datapoints = finalDatapoints, CreatedOn = DateTime.Now.Ticks };

                    return new DatapointResult { CreatedOn = DateTime.Now.Ticks, Datapoints = finalDatapoints, Metadata = finalMetadata };
                }
            }
            else
            {
                ConcurrentDictionary<string, DatapointResult> results = new ConcurrentDictionary<string, DatapointResult>();
                BermudaNodeStatistic stats = new BermudaNodeStatistic();

                var blobInterfaces = AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);

                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, Endpoint.ToString());

                DatapointResult cachedDatapoints;
                if (CachedDatapoints.TryGetValue(blobSetKey, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB SET DATAPOINT results [REMDEPTH:" + remdepth + "]");
                    return new DatapointResult { Datapoints = cachedDatapoints.Datapoints, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_3" } };
                }
                else
                {
                    //Chad: short circuiting to test WCF response time in Azure
                    //return new DatapointResult() { Datapoints = new List<Datapoint>(), CreatedOn = DateTime.Now.Ticks, Metadata = new BermudaNodeStatistic() };

                    var queryFunc = GetFilterFunc(query);
                    var mapreduceFunc = GetMapReduceFunc(mapreduce);
                    var mergeFunc = GetMergeFunc(merge);

                    IEnumerable<Datapoint> datapoints = null;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    foreach(var blobInterface in blobInterfaces)
                    {
                        var blobKey = GetQueryChecksum(domain, blobInterface.Name, query, mapreduce, minDate, maxDate, parameters, Endpoint.ToString());

                        //see if the cache contains a matching result and return it if it's not outdated
                        DatapointResult cachedDatapoints2;
                        if (CachedDatapoints.TryGetValue(blobKey, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                        {
                            if(CacheTraceMessageLevel < 1) Trace.WriteLine("returned CACHED BLOB DATAPOINT results  [REMDEPTH:" + remdepth + "]");
                            results[blobInterface.Name] = new DatapointResult { Datapoints = cachedDatapoints2.Datapoints, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_4" } };
                            datapoints = cachedDatapoints2.Datapoints;
                        }
                        else
                        {
                            //get mentions
                            var raw = blobInterface.GetData();

                            var minDateTicks = minDate.Ticks;
                            var maxDateTicks = maxDate.Ticks;
                            var filtered =
                                queryFunc == null ?
                                raw :
                                minDate == DateTime.MinValue && maxDate == DateTime.MaxValue ?
                                raw.AsParallel().Where(x => queryFunc(x, parameters)) :
                                raw.AsParallel().Where(x => x.OccurredOnTicks >= minDateTicks && x.OccurredOnTicks <= maxDateTicks && queryFunc(x, parameters));
                            
                            //reduce them using the passed expression
                            var subresult = mapreduceFunc(filtered);

                            datapoints = subresult;

                            //format a metada string
                            if (!args.Contains("-nocount"))
                            {
                                stats.TotalItems = raw.Count();
                                stats.FilteredItems = filtered.Count();
                                stats.ReducedItems = subresult.Count();
                            }
                            
                            //cache the result
                            //results[blobInterface.Name] = new DatapointResult { Datapoints = subresult, CreatedOn = DateTime.UtcNow.Ticks, Metadata = stats.Serialize() };
                            //CachedDatapoints[blobKey] = new DatapointResult { Datapoints = subresult.ToList(), CreatedOn = DateTime.UtcNow.Ticks, Metadata = stats.Serialize() };
                        }
                    }

                    //figure out the metadata
                    //var finalMetadata = "    [@" + AzureInterface.Instance.CurrentInstanceId + "] Calculated Datapoints:\r\n" + string.Join("\r\n", results.Values.Select(x => x.Metadata));

                    stats.NodeId = AzureInterface.Instance.CurrentInstanceId;
                    stats.Notes = "Computed Datapoints";

                    //Trace.WriteLine("total mentions processed: " + mentionCount);

                    //var datapoints = results.Values.SelectMany(x => x.Datapoints);
                    if (datapoints == null) return new DatapointResult();

                    //foreach (var p in datapoints) if (p.IsCount) p.Value = p.Count;

                    var finalDatapoints = MergeDatapoints(datapoints, mergeFunc); 
                    
                    
                    //var pointsGroups = datapoints.GroupBy(x => new { EntityId = x.Id, EntityId2 = x.Id2, x.Timestamp, x.Text, x.Text2 });

                    //var finalDatapoints = new List<Datapoint>();
                    //foreach (var g in pointGroups)
                    //{
                    //    finalDatapoints.Add(new Datapoint { Id = g.Key.EntityId, Id2 = g.Key.EntityId2, Timestamp = g.Key.Timestamp, Value = mergeFunc(g), Count = g.Sum(y => y.Count), Text = g.Key.Text, Text2 = g.Key.Text2 });
                    //}

                    sw.Stop();

                    stats.LinqExecutionTime = sw.Elapsed;
                    //finalMetadata += "\r\n        LINQ Executed in " + sw.Elapsed;

                    var result = CachedDatapoints[blobSetKey] = new DatapointResult { Datapoints = finalDatapoints, CreatedOn = DateTime.Now.Ticks, Metadata = stats };

                    return result;
                }
            }
        }*/

        private static object MergeResults(object results, Expression mergeExpr)
        {


            //var compileMethod = exor.GetType().GetMethod("Compile");

            //var func = compileMethod.Invoke(expr, new object[0]);

            //var invokeMethod = func.GetType().GetMethod("Invoke");

            //var res = invokeMethod.GetType().GetMethod("Invoke").Invoke(invokeMethod, new object[] { func, new object[0] });

            return null;
        }


        public BermudaResult GetData(string domain, IEnumerable<string> blobs, string query, string mapreduce, string merge, DateTime minDate, DateTime maxDate, int remdepth, object[] parameters, string command)
        {
            var args = ParseCommand(command);

            if (remdepth > 0)
            {
                //map
                var blobInterfaces = blobs == null ? AzureInterface.Instance.ListBlobs(domain, minDate.Ticks, maxDate.Ticks) : AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);

                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, null);

                //reduce 
                BermudaResult cachedDatapoints;
                if (CachedData.TryGetValue(blobSetKey, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 3) Trace.WriteLine("returned CACHED BLOBS DATAPOINTS results FOR ENTIRE BLOB SET [REMDEPTH:" + remdepth + "]");
                    return new BermudaResult { DataType = cachedDatapoints.DataType, Data = cachedDatapoints.Data, MetadataObject = new BermudaNodeStatistic { Notes = "Cache_Hit_1" } };
                }
                else
                {

                    var assignments = PartitionBlobs(domain, blobInterfaces, minDate, maxDate, false, true);

                    if (!assignments.Any()) throw new Exception("Specified dataset not loaded: " + domain);

                    ConcurrentDictionary<IPEndPoint, BermudaResult> results = new ConcurrentDictionary<IPEndPoint, BermudaResult>();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    List<Task> tasks = new List<Task>();
                    foreach (var ass in assignments)
                    {
                        Task t = new Task((assObj) =>
                        {
                            ZipMetadata assignment = assObj as ZipMetadata;
                            var initiated = DateTime.Now;
                            var blobSubsetKey = GetQueryChecksum(domain, string.Join(",", assignment.Blobs.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, assignment.PeerEndpoint.ToString());
                            Stopwatch sw3 = new Stopwatch();
                            sw3.Start();

                            //see if the cache contains a matching result and return it if it's not outdated
                            BermudaResult cachedDatapoints2;
                            if (CachedData.TryGetValue(blobSubsetKey, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                            {
                                if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB DATAPOINT results FOR BLOB SUBSET [REMDEPTH:" + remdepth + "]");
                                results[assignment.PeerEndpoint] = new BermudaResult { DataType = cachedDatapoints2.DataType, Data = cachedDatapoints2.Data, MetadataObject = new BermudaNodeStatistic { Notes = "Cache_Hit_2" } };
                            }
                            else
                            {
                                try
                                {
                                    Stopwatch sw2 = new Stopwatch();
                                    sw2.Start();
                                    BermudaResult subresult = null;

                                    if (assignment.PeerEndpoint.Equals(Endpoint))
                                    {
                                        subresult = GetData(domain, assignment.Blobs.Select(x => x.Name), query, mapreduce, merge, minDate, maxDate, remdepth - 1, parameters, command);

                                    }
                                    else
                                    {
                                        using (var client = AzureInterface.Instance.GetServiceClient(assignment.PeerEndpoint))
                                        {
                                            subresult = client.GetData(domain, query, mapreduce, merge, minDate, maxDate, remdepth - 1, parameters, command);
                                        }
                                    }

                                    sw2.Stop();
                                    subresult.CreatedOn = DateTime.Now.Ticks;
                                    subresult.MetadataObject.Initiated = initiated;
                                    subresult.MetadataObject.Completed = DateTime.Now;
                                    subresult.MetadataObject.OperationTime = sw2.Elapsed;
                                    results[assignment.PeerEndpoint] = CachedData[blobSubsetKey] = subresult;
                                }
                                catch (Exception ex)
                                {
                                    results[assignment.PeerEndpoint] = new BermudaResult { Error = "[Failed Node] " + ex };
                                }
                            }
                        }, ass, TaskCreationOptions.LongRunning);

                        tasks.Add(t);
                        t.Start();
                    }

                    Task.WaitAll(tasks.ToArray());

                    sw.Stop();
                    Trace.WriteLine("Join Time:" + sw.Elapsed);

                    if (results.All(x => x.Value.Error != null)) throw new Exception("All nodes failed:\r\n" + string.Join("\r\n", results.Select(x => x.Value.Error)));

                    //if all results are not the same time throw an error
                    if (results.GroupBy(x => x.Value.DataType).Count() > 1) throw new Exception("Subresults must all return the same type");

                    var dataTypeDescriptor = results.Select(x => x.Value.DataType).FirstOrDefault(x => x != null);

                    if (dataTypeDescriptor == null) return new BermudaResult { Error = "Could not determine the merge type, none of the nodes provided type info" };

                    //use the passed combine espression to make multiple datapoint sets into one

                    var dataType = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(dataTypeDescriptor);

                    //allItems = results.Values.SelectMany(x => x.DataObject)

                    var totalJson = "[" + string.Join(",", results.Values.Select(x => x.Data.Trim('[', ']'))) + "]";

                    var allItems = LinqRuntimeTypeBuilder.DeserializeJson(totalJson, dataTypeDescriptor, true);
                        

                    //var aaa = new JavaScriptSerializer().Deserialize<Datapoint[]>(totalJson);
                    //var ggc = aaa.GroupBy(x => new { x.Id, x.Id2 }).Count();

                    //InvokeSelectManyViaReflectionTheKilla(results.Values.Select(x => x.DataObject), dataType);

                    var mergeFunc = GetMergeFunc(merge, mapreduce, dataType);
                    if (mergeFunc != null)
                    {
                        //var dataType = "kdsajkdsa";
                        var mergeInvokeMethod = mergeFunc.GetType().GetMethod("Invoke");
                        allItems = mergeInvokeMethod.Invoke(mergeFunc, new object[] { allItems }); // MergeDatapoints(results.Values.Where(x => x.Data != null).SelectMany(x => x.Data), mergeFunc);
                    }

                    //figure out the metadata
                    var finalMetadata = new BermudaNodeStatistic { Notes = "Merged Datapoints in " + sw.Elapsed, NodeId = AzureInterface.Instance.CurrentInstanceId, ChildNodes = results.Values.Select(x => x.MetadataObject ).ToArray() };

                    var finalResult = new BermudaResult { DataType = dataTypeDescriptor, DataObject = allItems, CreatedOn = DateTime.Now.Ticks, MetadataObject = finalMetadata };

                    CachedData[blobSetKey] = finalResult;

                    return finalResult;
                }
            }
            else
            {
                ConcurrentDictionary<string, BermudaResult> results = new ConcurrentDictionary<string, BermudaResult>();
                BermudaNodeStatistic stats = new BermudaNodeStatistic();

                var blobInterfaces = AzureInterface.Instance.GetBlobInterfacesByNames(domain, blobs);

                var blobSetKey = GetQueryChecksum(domain, string.Join(",", blobInterfaces.Select(x => x.Name)), query, mapreduce, minDate, maxDate, parameters, Endpoint.ToString());

                BermudaResult cachedDatapoints;
                if (CachedData.TryGetValue(blobSetKey, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB SET DATAPOINT results [REMDEPTH:" + remdepth + "]");
                    return new BermudaResult { DataType = cachedDatapoints.DataType, Data = cachedDatapoints.Data, MetadataObject = new BermudaNodeStatistic { Notes = "Cache_Hit_3" } };
                }
                else
                {
                    //Chad: short circuiting to test WCF response time in Azure
                    //return new DatapointResult() { Datapoints = new List<Datapoint>(), CreatedOn = DateTime.Now.Ticks, Metadata = new BermudaNodeStatistic() };

                   
                    //IEnumerable<Datapoint> datapoints = null;
                    object datapoints = null;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    Type itemType = null;
                    Type resultType = null;

                    foreach (var blobInterface in blobInterfaces)
                    {
                        var blobKey = GetQueryChecksum(domain, blobInterface.Name, query, mapreduce, minDate, maxDate, parameters, Endpoint.ToString());

                        //see if the cache contains a matching result and return it if it's not outdated
                        BermudaResult cachedDatapoints2;
                        if (CachedData.TryGetValue(blobKey, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                        {
                            if (CacheTraceMessageLevel < 1) Trace.WriteLine("returned CACHED BLOB DATAPOINT results  [REMDEPTH:" + remdepth + "]");
                            results[blobInterface.Name] = new BermudaResult { DataType = cachedDatapoints2.DataType, Data = cachedDatapoints2.Data, MetadataObject = new BermudaNodeStatistic { Notes = "Cache_Hit_4" } };
                            datapoints = cachedDatapoints2.DataObject;
                        }
                        else
                        {
                            //get mentions
                            var raw = blobInterface.GetData();
                            var rawType = raw.GetType();
                            itemType = ReduceExpressionGeneration.GetTypeOfEnumerable(rawType);
                            var mapreduceFunc = GetMapReduceFunc(mapreduce, itemType, out resultType);
                            var queryFunc = GetFilterFunc(query, itemType);
                    
                            var minDateTicks = minDate.Ticks;
                            var maxDateTicks = maxDate.Ticks;


                            object subresult = raw.AsParallel();
                             
                                //queryFunc == null ?
                                //    raw.AsParallel() :
                                //minDate == DateTime.MinValue && maxDate == DateTime.MaxValue ?
                                //    raw.AsParallel().Where(x => queryFunc) :
                                //    raw.AsParallel().Where(x => x.OccurredOnTicks >= minDateTicks && x.OccurredOnTicks <= maxDateTicks && queryFunc(x, parameters));

                            if (queryFunc != null)
                            {
                                var queryFuncInvoke = queryFunc.GetType().GetMethod("Invoke");
                                subresult = queryFuncInvoke.Invoke(queryFunc, new object[] { subresult });
                            }

                            //reduce them using the passed expression
                            if (mapreduceFunc != null)
                            {
                                var mapReduceFuncInvoke = mapreduceFunc.GetType().GetMethod("Invoke");
                                subresult = mapReduceFuncInvoke.Invoke(mapreduceFunc, new object[] { subresult });
                            }
                            

                            datapoints = subresult;

                            //format a metada string
                            if (!args.Contains("-nocount"))
                            {
                                //stats.TotalItems = raw.Count();
                                //stats.FilteredItems = filtered.Count();
                                //stats.ReducedItems = subresult.Count();
                            }

                            //cache the result
                            //results[blobInterface.Name] = new DatapointResult { Datapoints = subresult, CreatedOn = DateTime.UtcNow.Ticks, Metadata = stats.Serialize() };
                            //CachedDatapoints[blobKey] = new DatapointResult { Datapoints = subresult.ToList(), CreatedOn = DateTime.UtcNow.Ticks, Metadata = stats.Serialize() };
                        }
                    }

                    //figure out the metadata
                    //var finalMetadata = "    [@" + AzureInterface.Instance.CurrentInstanceId + "] Calculated Datapoints:\r\n" + string.Join("\r\n", results.Values.Select(x => x.Metadata));

                    stats.NodeId = AzureInterface.Instance.CurrentInstanceId;
                    stats.Notes = "Computed Datapoints";
                    
                    //Trace.WriteLine("total mentions processed: " + mentionCount);

                    //var datapoints = results.Values.SelectMany(x => x.Datapoints);
                    if (datapoints == null) return new BermudaResult() { MetadataObject = new BermudaNodeStatistic { Notes = "No Results" } };

                    //foreach (var p in datapoints) if (p.IsCount) p.Value = p.Count;

                    var mergeFunc = resultType == null ? null : GetMergeFunc(merge, mapreduce, resultType);
                    if (mergeFunc != null)
                    {
                        var mergeFuncInvoke = mergeFunc.GetType().GetMethod("Invoke");
                        datapoints = mergeFuncInvoke.Invoke(mergeFunc, new object[] { datapoints });
                    }

                    sw.Stop();

                    stats.LinqExecutionTime = sw.Elapsed;

                    var result = CachedData[blobSetKey] = new BermudaResult { DataType = LinqRuntimeTypeBuilder.GetTypeKey(resultType), DataObject = datapoints, CreatedOn = DateTime.Now.Ticks, MetadataObject = stats  };

                    return result;
                }
            }
        }

        private object InvokeSelectManyViaReflectionTheKilla(object results, Type itemType)
        {
            var subsetType = itemType.MakeArrayType();
            //var subsetType = ReduceExpressionGeneration.GetTypeOfEnumerable(results.GetType());

            var genericCastInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "Cast" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericCastInfo = genericCastInfos.Skip(0).FirstOrDefault();
            var castInfo = genericCastInfo.MakeGenericMethod(itemType);

            results = castInfo.Invoke(null, new object[] { results });

            var genericToArrayInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "ToArray" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericToArrayInfo = genericToArrayInfos.FirstOrDefault();
            var toArrayInfo = genericToArrayInfo.MakeGenericMethod(itemType);
            results = toArrayInfo.Invoke(null, new object[] { results });

            var genericSelectManyInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "SelectMany" && x.IsGenericMethod && x.GetParameters().Length == 2);
            var genericSelectManyInfo = genericSelectManyInfos.Skip(0).FirstOrDefault();
            var selectManyInfo = genericSelectManyInfo.MakeGenericMethod(subsetType, itemType);

            
            var subsetParam = Expression.Parameter( type: subsetType, name: "subset" );
            var selectSelfExpr = Expression.Lambda( parameters: subsetParam, body: subsetParam);
            var selectSelfFunc = selectSelfExpr.Compile();

            return selectManyInfo.Invoke(null, new object[] { results, selectSelfFunc });
        }

        private static IEnumerable<ZipMetadata> PartitionBlobs(string domain, IEnumerable<IDataProvider> blobInterfaces, DateTime minDate, DateTime maxDate, bool useAggressiveCaching, bool distrubuteEverythingToEveryone)
        {
            
            if (blobInterfaces == null || blobInterfaces.Count() == 0) return new ZipMetadata[0];

            int machinesNeeded = distrubuteEverythingToEveryone ? MaxMachinesPerQuery : Math.Min(blobInterfaces.Count(), MaxMachinesPerQuery);

            var reducers = AzureInterface.Instance.GetAvailablePeerConnections( machinesNeeded );

            if (distrubuteEverythingToEveryone)
            {
                return reducers.Select(x => new ZipMetadata { Blobs = blobInterfaces, PeerEndpoint = x.IPEndpoint });
            }
            else
            {
                List<IEnumerable<IDataProvider>> partitionedEndpoints = new List<IEnumerable<IDataProvider>>();
                int partitionSize = (int)Math.Ceiling((double)blobInterfaces.Count() / (double)reducers.Count());

                int i = 0;
                foreach (var reducer in reducers)
                {
                    if (useAggressiveCaching)
                    {
                        ///TODO make this look at the reducer's id rather than the counter
                        partitionedEndpoints.Add(blobInterfaces.Where(x => Math.Abs(x.Id.GetHashCode() % reducers.Count()) == i));
                    }
                    else
                    {
                        partitionedEndpoints.Add(blobInterfaces.Skip(i * partitionSize).Take(partitionSize));
                    }
                    i++;
                }

                var result = reducers.Zip(partitionedEndpoints, (r, b) => new ZipMetadata { Blobs = b, PeerEndpoint = r.IPEndpoint });

                return result;
            }
        }

        private static string GetQueryChecksum(string domain, string blob, string query, string mapreduce, DateTime minDate, DateTime maxDate, object[] parameters, string endpoint)
        {
            StringBuilder str = new StringBuilder();
            str.Append(domain);
            str.Append("|||");
            str.Append(blob);
            str.Append("|||");
            if (query != null) str.Append(query.GetHashCode());
            str.Append("|||");
            if (mapreduce != null) str.Append(mapreduce.GetHashCode());
            str.Append("|||");
            str.Append(minDate);
            str.Append("|||");
            str.Append(maxDate);
            str.Append("|||");
            if (parameters != null) str.Append(string.Join("|", parameters.Select(x => x.GetHashCode())));
            str.Append("|||");
            str.Append(endpoint);
            return str.ToString();
        }

        static ConcurrentDictionary<string, object> filterFuncCache = new ConcurrentDictionary<string, object>();
        private static object GetFilterFunc(string str, Type itemType)
        {
            if (str == null) return null;

            object result = null;

            if (filterFuncCache.TryGetValue(str, out result)) return result;

            if (str.StartsWith(QlHeader))
            {
                var expr = EvoQLBuilder.GetWhereExpression(str.Substring(QlHeader.Length), itemType);
                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Mention)) }));
                var expr = serializer.Deserialize<Func<Mention, object[], bool>>(XElement.Parse(str));
                result = expr.Compile();
            }

            filterFuncCache[str] = result;

            return result;
        }

        //static ConcurrentDictionary<string, Func<IEnumerable<Mention>, IEnumerable<Mention>>> pagingFuncCache = new ConcurrentDictionary<string, Func<IEnumerable<Mention>, IEnumerable<Mention>>>();
        //private static Func<IEnumerable<Mention>, IEnumerable<Mention>> GetPagingFunc(string str)
        //{
        //    Func<IEnumerable<Mention>, IEnumerable<Mention>> result = null;

        //    if (pagingFuncCache.TryGetValue(str, out result)) return result;

        //    var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Mention)) }));
        //    var expr = serializer.Deserialize <Func<IEnumerable<Mention>, IEnumerable<Mention>>>(XElement.Parse(str));
        //    result = expr.Compile();

        //    pagingFuncCache[str] = result;

        //    return result;
        //}

        static ConcurrentDictionary<string, object> reduceFuncCache = new ConcurrentDictionary<string, object>();
        private static object GetMapReduceFunc(string str, Type itemType, out Type resultType)
        {
            if (str == null)
            {
                resultType = null;
                return null;
            }

            object result = null;

            //if (reduceFuncCache.TryGetValue(str, out result)) return result;

            if (str.StartsWith(QlHeader))
            {
                var expr = EvoQLBuilder.GetReduceExpression(str.Substring(QlHeader.Length), itemType);
                var type = expr == null ? null : expr.GetType();
                resultType = expr == null ? itemType : ReduceExpressionGeneration.GetTypeOfEnumerable( type.GetProperty("ReturnType").GetValue(expr, null) as Type );
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Mention)) }));
                var expr = serializer.Deserialize<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>>(XElement.Parse(str));
                resultType = typeof(Datapoint);
                result = expr.Compile();
            }

            reduceFuncCache[str] = result;

            return result;
        }

        static ConcurrentDictionary<string, object> mergeFuncCache = new ConcurrentDictionary<string, object>();
        private static object GetMergeFunc(string str, string mapreduce, Type itemType)
        {
            if (str == null) return null;

            object result = null;

            //if (mergeFuncCache.TryGetValue(str, out result)) return result;

            //if (str == DefaultToken)
            //{
            //    Expression<Func<IEnumerable<Datapoint>, double>> merge = x => x.Sum(y => y.Value);
            //    result = merge.Compile();
            //}
            //else if (str == "__average__")
            //{
            //    Expression<Func<IEnumerable<Datapoint>, double>> merge = x => x.Sum(y => y.Value * (double)y.Count) / x.Sum(y => y.Count);
            //    result = merge.Compile();
            //}
            if (str == DefaultToken)
            {
                if (!mapreduce.StartsWith(QlHeader)) throw new Exception("QL based mapreduce expression must be used with __default__ merge specification");
                var expr = EvoQLBuilder.GetMergeExpression(mapreduce.Substring(QlHeader.Length), itemType);
                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(Entities.Thrift.ThriftMention)) }));
                var expr = serializer.Deserialize<Func<IEnumerable<Datapoint>, double>>(XElement.Parse(str));
                result = expr.Compile();
            }

            mergeFuncCache[str] = result;

            return result;
        }

        public string GetStatus(bool isInternal)
        {
            var reducers = AzureInterface.Instance.GetAvailablePeerConnections(10000);
            ConcurrentDictionary<string, string> results = new ConcurrentDictionary<string, string>();

            List<Task> statusTasks = new List<Task>();
            foreach (var reducer in reducers)
            {
                Task t = new Task((r) =>
                {
                    RoleInstanceEndpoint rie = r as RoleInstanceEndpoint;
                    string id = rie.RoleInstance.Id.Split('_').LastOrDefault();

                    try
                    {
                        if (rie.IPEndpoint.Equals(Endpoint))
                        {
                            var memStatus = SystemInfo.GetMemoryStatusEx();
                            var usedMem = Math.Round((double)(memStatus.ullTotalPhys - memStatus.ullAvailPhys) / 1073741824d, 2); //Math.Round( (double)GC.GetTotalMemory(false) / 1073741824d, 4);
                            var availMem = Math.Round((double)memStatus.ullAvailPhys / 1073741824d, 2);
                            string sub = string.Format(" Memory Usage: Used={0}GB, Available={1}GB", usedMem, availMem);

                            int totalMentions = 0;

                            foreach (var sql in SqlInterface.StoredSqlInterfaces)
                            {
                                var count = sql.GetData().Count();
                                totalMentions += count;
                                sub += "\r\n    Subdomain:" + sql.Name + "  Mentions:" + count;
                            }
                            sub += "\r\n    Total Mentions:" + totalMentions;

                            results[id] = sub;
                        }
                        else
                        {
                            if (!isInternal)
                            {
                                var client = AzureInterface.Instance.GetServiceClient(rie);
                                results[id] = client.Ping("status");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        results[id] = "[Failed Status On Node] " + ex.Message;
                    }
                },
                reducer,
                TaskCreationOptions.LongRunning);

                statusTasks.Add(t);
                t.Start();
            }

            Task.WaitAll(statusTasks.ToArray());

            if (isInternal) return string.Join("\r\n", results.Values);
            else return string.Join("\r\n", results.Select(x => "[@" + x.Key + "] ==> " + x.Value));
        }
    }

    public class ZipMetadata
    {
        public IEnumerable<IDataProvider> Blobs;
        public IPEndPoint PeerEndpoint;
    }

 
}
