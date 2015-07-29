using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;
using Bermuda.Util;
using System.Threading.Tasks;
using Bermuda.ExpressionGeneration;
using System.IO;
using Bermuda.Interface;
using Bermuda.Catalog;
using Newtonsoft.Json;

namespace Bermuda.Core.MapReduce
{
    public class BermudaMapReduce
    {
        public static readonly string XmlHeader = "__expression_xml__";
        public static readonly string QlHeader = "__ql__";
        public static readonly string DefaultToken = "__default__";
        public static readonly string PagingToken = "__paging__";
        public static readonly string MakeCursorToken = "__makecursor__";

        static readonly long StrongIndexDistance = TimeSpan.FromDays(1).Ticks;
        static readonly long CacheLifetime = TimeSpan.FromSeconds(60).Ticks;
        static readonly int MaxMachinesPerQuery = 9000;

        ConcurrentDictionary<string, BermudaResult> CachedData = new ConcurrentDictionary<string, BermudaResult>();

        //0:individual blobs   1:blob subsets   2:whole blob sets
        static readonly int CacheTraceMessageLevel = 2;

        static BermudaMapReduce _instance;
        public static BermudaMapReduce Instance
        {
            get
            {
                return _instance ?? (_instance = new BermudaMapReduce());
            }
        }

        private BermudaMapReduce()
        {
         
        }

        public IPeerInfo Endpoint
        {
            get
            {
                return HostEnvironment.Instance.CurrentEndpoint;
            }
        }

        
 
        private IEnumerable<string> ParseCommand(string command)
        {
            if( command == null ) return new string[0];
            return command.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
        }


        public static string GetHttpDataFromPeer(IPEndPoint peer, Dictionary<string,string> get)
        {
            var url = peer.ToString() + "?" + string.Join("&", get.Select(x => x.Key + '=' + x.Value));

            var request = HttpWebRequest.Create(url);

            var response = request.GetResponse();

            var reader = new StreamReader(response.GetResponseStream());

            var result = reader.ReadToEnd();

            return result;
        }

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, int remdepth, string command, string cursor, string paging2)
        {
            var args = ParseCommand(command);
            bool noCache = args.Contains("-nocache");
            bool makeCursor = cursor == MakeCursorToken;
            bool useCursor = !makeCursor && !string.IsNullOrWhiteSpace(cursor);

            DateTime minDate = DateTime.MinValue;
            DateTime maxDate = DateTime.MaxValue;

            if (remdepth > 0)
            {
                //map
                var queryHash = cursor ?? GetQueryHash(domain, query, mapreduce, merge, paging, null);

                //reduce 
                BermudaResult cachedDatapoints;
                if (!noCache && CachedData.TryGetValue(queryHash, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime)
                {
#if DEBUG
                    if (CacheTraceMessageLevel < 3) Trace.WriteLine("returned CACHED BLOBS DATAPOINTS results FOR ENTIRE BLOB SET [REMDEPTH:" + remdepth + "]");
#endif

                    if (useCursor)
                    {
                        var dataType = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(cachedDatapoints.DataType);
                        return GetCursorData(paging2, cachedDatapoints, dataType);
                    }
                    else
                    {
                        return new BermudaResult { DataType = cachedDatapoints.DataType, Data = cachedDatapoints.Data, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_1" }, CacheKey = cachedDatapoints.CacheKey };
                    }
                }
                else
                {
                    if (useCursor) throw new Exception("Cursor " + cursor + " not found");
                    //var assignments = PartitionBlobs(domain, blobInterfaces, minDate, maxDate, false, true);

                    var reducers = HostEnvironment.Instance.GetAvailablePeerConnections();

                    if (!reducers.Any()) throw new Exception("Specified dataset not loaded: " + domain);

                    ConcurrentDictionary<PeerInfo, BermudaResult> results = new ConcurrentDictionary<PeerInfo, BermudaResult>();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    List<Task> tasks = new List<Task>();
                    foreach (var reducer in reducers)
                    {
                        Task t = new Task((peerObj) =>
                        {
                            var peerInfo = peerObj as PeerInfo;
                            var initiated = DateTime.Now;
                            var subqueryHash = GetQueryHash(domain, query, mapreduce, merge, paging, peerInfo.ToString());
                            Stopwatch sw3 = new Stopwatch();
                            sw3.Start();

                            //see if the cache contains a matching result and return it if it's not outdated
                            BermudaResult cachedDatapoints2;
                            if (!noCache && CachedData.TryGetValue(subqueryHash, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                            {
                                if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB DATAPOINT results FOR BLOB SUBSET [REMDEPTH:" + remdepth + "]");

                                BermudaResult res = null;

                                if (useCursor) 
                                {
                                    var dataType2 = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(cachedDatapoints2.DataType);
                                    res = GetCursorData(paging2, cachedDatapoints2, dataType2);
                                }
                                else 
                                {
                                    res = new BermudaResult { DataType = cachedDatapoints2.DataType, Data = cachedDatapoints2.Data, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_2" } };
                                }
                                
                                results[peerInfo] = res;
                            }
                            else
                            {
                                try
                                {
                                    Stopwatch sw2 = new Stopwatch();
                                    sw2.Start();
                                    BermudaResult subresult = null;

                                    if (peerInfo.Equals(Endpoint))
                                    {
                                        subresult = GetData(domain, query, mapreduce, merge, paging, remdepth - 1, command, cursor, paging2);

                                    }
                                    else
                                    {
                                        using (var client = HostEnvironment.GetServiceClient(peerInfo))
                                        {
                                            subresult = client.GetData(domain, query, mapreduce, merge, paging, remdepth - 1, command, cursor, paging2);
                                        }
                                        //subresult = GetDataFromPeer(domain, query, mapreduce, merge, minDate, maxDate, remdepth - 1, command, assignment.PeerEndpoint.Endpoint);
                                    }

                                    sw2.Stop();
                                    subresult.CreatedOn = DateTime.Now.Ticks;
                                    subresult.Metadata.Initiated = initiated;
                                    subresult.Metadata.Completed = DateTime.Now;
                                    subresult.Metadata.OperationTime = sw2.Elapsed;
                                    results[peerInfo] = CachedData[subqueryHash] = subresult;
                                }
                                catch (Exception ex)
                                {
                                    results[peerInfo] = new BermudaResult { Error = "[Failed Node] " + ex };
                                }
                            }
                        }, reducer, TaskCreationOptions.LongRunning);

                        tasks.Add(t);
                        t.Start();
                    }

                    Task.WaitAll(tasks.ToArray());

                    sw.Stop();

#if DEBUG
                    Trace.WriteLine("Join Time:" + sw.Elapsed);
#endif

                    if (results.Any(x => x.Value.Error != null)) throw new BermudaException("Some nodes failed:\r\n" + string.Join("\r\n", results.Select(x => x.Value.Error)));

                    if (results.All(x => x.Value.Data == null)) return new BermudaResult { Metadata = new BermudaNodeStatistic { Notes = "No Data" } };

                    //if all results are not the same time throw an error
                    if (results.GroupBy(x => x.Value.DataType).Count() > 1) throw new BermudaException("Subresults must all return the same type");

                    var dataTypeDescriptor = results.Select(x => x.Value.DataType).FirstOrDefault(x => x != null);

                    if (dataTypeDescriptor == null) return new BermudaResult { Error = "Could not determine the merge type, none of the nodes provided type info" };

                    //use the passed combine espression to make multiple datapoint sets into one

                    var dataType = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(dataTypeDescriptor);

                    //allItems = results.Values.SelectMany(x => x.DataObject)

                    var totalJson = "[" + string.Join(",", results.Values.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data.Trim('[', ']')).Where(x => !string.IsNullOrWhiteSpace(x))) + "]";

                    var allItems = LinqRuntimeTypeBuilder.DeserializeJson(totalJson, dataTypeDescriptor, true);


                    //var aaa = new JavaScriptSerializer().Deserialize<Datapoint[]>(totalJson);
                    //var ggc = aaa.GroupBy(x => new { x.Id, x.Id2 }).Count();

                    //InvokeSelectManyViaReflectionTheKilla(results.Values.Select(x => x.DataObject), dataType);

                    var mergeFunc = GetMergeFunc(merge, mapreduce, dataType, dataType);
                    if (mergeFunc != null)
                    {
                        //var dataType = "kdsajkdsa";
                        var mergeInvokeMethod = mergeFunc.GetType().GetMethod("Invoke");
                        allItems = mergeInvokeMethod.Invoke(mergeFunc, new object[] { allItems }); // MergeDatapoints(results.Values.Where(x => x.Data != null).SelectMany(x => x.Data), mergeFunc);
                    }

                    var pagingFunc = GetPagingFunc(paging, dataType);
                    if (pagingFunc != null)
                    {
                        var pagingInvokeMethod = pagingFunc.GetType().GetMethod("Invoke");
                        allItems = pagingInvokeMethod.Invoke(pagingFunc, new object[] { allItems });
                    }

                    //figure out the metadata
                    var finalMetadata = new BermudaNodeStatistic { Notes = "Merged Datapoints in " + sw.Elapsed, NodeId = HostEnvironment.Instance.CurrentInstanceId, ChildNodes = results.Values.Select(x => x.Metadata).ToArray() };

                    var arraylol = ToArrayCollection(allItems, dataType);

                    var json = JsonConvert.SerializeObject(arraylol);
                    //var json = JsonConvert.SerializeObject(allItems);

                    var originalData = makeCursor ? arraylol : null;

                    var finalResult = new BermudaResult { DataType = dataTypeDescriptor, OriginalData = originalData, Data = json, CreatedOn = DateTime.Now.Ticks, Metadata = finalMetadata, CacheKey = queryHash };

                    CachedData[queryHash] = finalResult;

                    return finalResult;
                }
            }
            else
            {
                ConcurrentDictionary<string, BermudaResult> results = new ConcurrentDictionary<string, BermudaResult>();
                BermudaNodeStatistic stats = new BermudaNodeStatistic();

                var bucketInterfaces = HostEnvironment.Instance.GetBucketInterfacesForDomain(domain);

                if (!bucketInterfaces.Any()) throw new BermudaException("Data not loaded for: " + domain);
                if (bucketInterfaces.Count() > 1) throw new BermudaException("Multiple buckets not supported by BermudaMapReduce");

                var queryHash = GetQueryHash(domain, query, mapreduce, merge, paging, Endpoint.ToString());

                BermudaResult cachedDatapoints;
                if (!noCache && CachedData.TryGetValue(queryHash, out cachedDatapoints) && (DateTime.Now.Ticks - cachedDatapoints.CreatedOn) < CacheLifetime)
                {
                    if (CacheTraceMessageLevel < 2) Trace.WriteLine("returned CACHED BLOB SET DATAPOINT results [REMDEPTH:" + remdepth + "]");

                    if (useCursor)
                    {
                        var dataType = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(cachedDatapoints.DataType);
                        return GetCursorData(paging2, cachedDatapoints, dataType);
                    }
                    else
                    {
                        return new BermudaResult { DataType = cachedDatapoints.DataType, Data = cachedDatapoints.Data, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_3" }, CacheKey = queryHash };
                    }
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
                    string json = null;

                    foreach (var bucketInterface in bucketInterfaces)
                    {
                        var bucketKey = GetQueryHash(domain, query, mapreduce, merge, paging, Endpoint.ToString());

                        //see if the cache contains a matching result and return it if it's not outdated
                        BermudaResult cachedDatapoints2;
                        if (!noCache && CachedData.TryGetValue(bucketKey, out cachedDatapoints2) && (DateTime.Now.Ticks - cachedDatapoints2.CreatedOn) < CacheLifetime)
                        {
                            if (CacheTraceMessageLevel < 1) Trace.WriteLine("returned CACHED BLOB DATAPOINT results  [REMDEPTH:" + remdepth + "]");

                            if (useCursor)
                            {
                                if (cachedDatapoints2.OriginalData == null) throw new Exception("Cursor " + cursor + " contains null data");
                                var dataType = LinqRuntimeTypeBuilder.GetTypeFromTypeKey(cachedDatapoints2.DataType);
                                results[bucketInterface.Name] = GetCursorData(paging2, cachedDatapoints2, dataType);
                                
                            }
                            else
                            {
                                results[bucketInterface.Name] = new BermudaResult { DataType = cachedDatapoints2.DataType, Data = cachedDatapoints2.Data, Metadata = new BermudaNodeStatistic { Notes = "Cache_Hit_4" } };
                                json = cachedDatapoints2.Data;
                            }
                        }
                        else
                        {
                            //get mentions
                            var collections = GetCollections(query, mapreduce);

                            if (collections.Count() > 1) throw new BermudaException("More than one collection specified: " + string.Join(",", collections));

                            var table = collections.FirstOrDefault();

                            var tableName = table == null ? null : table.Source;

                            var raw = bucketInterface.GetData(tableName);
                            //var rawType = raw.GetType();
                            //itemType = ReduceExpressionGeneration.GetTypeOfEnumerable(rawType);
                            itemType = bucketInterface.GetDataType(tableName);
                            var mapreduceFunc = GetMapReduceFunc(mapreduce, itemType, out resultType);
                            var queryFunc = GetFilterFunc(query, itemType);
                            var pagingFunc = GetPagingFunc(paging, resultType);
                    
                            var minDateTicks = minDate.Ticks;
                            var maxDateTicks = maxDate.Ticks;


                            object subresult = raw;
                             
                                //queryFunc == null ?
                                //    raw.AsParallel() :
                                //minDate == DateTime.MinValue && maxDate == DateTime.MaxValue ?
                                //    raw.AsParallel().Where(x => queryFunc) :
                                //    raw.AsParallel().Where(x => x.OccurredOnTicks >= minDateTicks && x.OccurredOnTicks <= maxDateTicks && queryFunc(x, parameters));

                            if (json == null)
                            {
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

                                if (pagingFunc != null)
                                {
                                    var pagingInvokeMethod = pagingFunc.GetType().GetMethod("Invoke");
                                    subresult = pagingInvokeMethod.Invoke(pagingFunc, new object[] { subresult });
                                }


                                datapoints = subresult;
                            }

                            //format a metada string
                            if (!args.Contains("-nocount"))
                            {
                                stats.TotalItems = bucketInterface.GetCount(tableName);
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

                    stats.NodeId = HostEnvironment.Instance.CurrentInstanceId;
                    stats.Notes = "Computed Datapoints";
                    
                    //Trace.WriteLine("total mentions processed: " + mentionCount);

                    //var datapoints = results.Values.SelectMany(x => x.Datapoints);
                    if (datapoints == null) return new BermudaResult() { Metadata = new BermudaNodeStatistic { Notes = "No Results" } };

                    //foreach (var p in datapoints) if (p.IsCount) p.Value = p.Count;

                    var mergeFunc = GetMergeFunc(merge, mapreduce, itemType, resultType);
                    if (mergeFunc != null)
                    {
                        var mergeFuncInvoke = mergeFunc.GetType().GetMethod("Invoke");
                        datapoints = mergeFuncInvoke.Invoke(mergeFunc, new object[] { datapoints });
                    }

                    stats.LinqExecutionTime = sw.Elapsed;

                    var arraylol = ToArrayCollection(datapoints, resultType);

                    if (json == null && datapoints != null)
                    {
                        json = JsonConvert.SerializeObject(arraylol);
                    }
                    
                    //var json = JsonConvert.SerializeObject(datapoints);
                    var originalData = makeCursor ? arraylol : null;

                    var result = CachedData[queryHash] = new BermudaResult { DataType = LinqRuntimeTypeBuilder.GetTypeKey(resultType), OriginalData = originalData, Data = json, CreatedOn = DateTime.Now.Ticks, Metadata = stats  };

                    sw.Stop();

                    return result;
                }
            }
        }

        private BermudaResult GetCursorData(string paging2, BermudaResult cachedDatapoints, Type dataType)
        {
            var pagingFunc2 = GetPagingFunc(paging2, dataType);
            if (pagingFunc2 != null)
            {
                var mergeInvokeMethod = pagingFunc2.GetType().GetMethod("Invoke");
                var allItems = mergeInvokeMethod.Invoke(pagingFunc2, new object[] { cachedDatapoints.OriginalData });

                var arraylol2 = ToArrayCollection(allItems, dataType);

                var json2 = JsonConvert.SerializeObject(arraylol2);

                return new BermudaResult { DataType = cachedDatapoints.DataType, Data = json2, Metadata = new BermudaNodeStatistic { Notes = "Cursor_Hit_1" }, CacheKey = cachedDatapoints.CacheKey };
            }
            else
            {
                throw new BermudaException("Cursor Paging Function required");
            }

        }

        private IEnumerable<CollectionExpression> GetCollections(string query, string mapreduce)
        {
            if (query != null && !query.StartsWith(QlHeader)) throw new BermudaException("Invalid filter expression");
            if (mapreduce != null && !mapreduce.StartsWith(QlHeader)) throw new BermudaException("Invalid reduce expression");

            return EvoQLBuilder.GetCollections
            (
                query == null ? null : query.Substring(QlHeader.Length),
                mapreduce == null ? null : mapreduce.Substring(QlHeader.Length),
                null,
                null
            );
        }

        static ConcurrentDictionary<string, object> pagingFuncCache = new ConcurrentDictionary<string, object>();
        private object GetPagingFunc(string str, Type itemType)
        {
            
            if (str == null) return null;

            object result = null;

            if(str.StartsWith(QlHeader))
            {
                var expr = EvoQLBuilder.GetPagingExpression(str.Substring(QlHeader.Length), itemType);
                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                throw new BermudaException("Unsupported paging expression");
            }

            mergeFuncCache[str] = result;

            return result;
        }

        private static object ToArrayCollection(object collection, Type elementType)
        {
            if (collection == null) return null;

            var genericToArrayInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "ToArray" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericToArrayInfo = genericToArrayInfos.FirstOrDefault();
            var toArrayInfo = genericToArrayInfo.MakeGenericMethod(elementType);

            var res = toArrayInfo.Invoke(null, new object[] { collection });

            return res;
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

            var reducers = HostEnvironment.Instance.GetAvailablePeerConnections( machinesNeeded );

            if (distrubuteEverythingToEveryone)
            {
                return reducers.Select(x => new ZipMetadata { Blobs = blobInterfaces, PeerEndpoint = x });
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

                var result = reducers.Zip(partitionedEndpoints, (r, b) => new ZipMetadata { Blobs = b, PeerEndpoint = r });

                return result;
            }
        }

        private static string GetQueryHash(string domain, string query, string mapreduce, string merge, string paging, string endpoint)
        {
            StringBuilder str = new StringBuilder();
            str.Append(domain);
            str.Append("|||");
            if (query != null) str.Append(query.GetHashCode());
            str.Append("|||");
            if (mapreduce != null) str.Append(mapreduce.GetHashCode());
            str.Append("|||");
            if (merge != null) str.Append(merge.GetHashCode());
            str.Append("|||");
            if (paging != null) str.Append(paging.GetHashCode());
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
                var res = EvoQLBuilder.GetWhereExpression(str.Substring(QlHeader.Length), itemType);
                var expr = res;
                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = expr == null ? null : compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                throw new Exception("Invalid filter expression");
                //var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(MentionTest)) }));
                //var expr = serializer.Deserialize<Func<MentionTest, object[], bool>>(XElement.Parse(str));
                //result = expr.Compile();
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
                resultType = itemType;
                return null;
            }

            object result = null;

            //if (reduceFuncCache.TryGetValue(str, out result)) return result;

            if (str.StartsWith(QlHeader))
            {
                var res = EvoQLBuilder.GetReduceExpression(str.Substring(QlHeader.Length), itemType);
                var expr = res;
                var type = expr == null ? null : expr.GetType();
                resultType = expr == null ? itemType : ReduceExpressionGeneration.GetTypeOfEnumerable( type.GetProperty("ReturnType").GetValue(expr, null) as Type );
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = expr==null ? null : compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                throw new Exception("Invalid reduce expression");
                //var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(MentionTest)) }));
                //var expr = serializer.Deserialize<Func<IEnumerable<MentionTest>, IEnumerable<Datapoint>>>(XElement.Parse(str));
                //resultType = typeof(Datapoint);
                //result = expr.Compile();
            }

            reduceFuncCache[str] = result;

            return result;
        }

        static ConcurrentDictionary<string, object> mergeFuncCache = new ConcurrentDictionary<string, object>();
        private static object GetMergeFunc(string str, string mapreduce, Type itemType, Type resultType)
        {
            if (str == null) return null;

            object result = null;

            if (str == DefaultToken)
            {
                if (resultType == null) return null;
                if (!mapreduce.StartsWith(QlHeader)) throw new Exception("QL based mapreduce expression must be used with __default__ merge specification");
                var res = EvoQLBuilder.GetMergeExpression(mapreduce.Substring(QlHeader.Length), itemType, resultType);
                var expr = res;
                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                result = expr == null ? null : compileMethod.Invoke(expr, new object[0]);
            }
            else
            {
                throw new Exception("Invalid merge expression");
                //var serializer = new ExpressionSerializer(new TypeResolver(new Assembly[] { Assembly.GetAssembly(typeof(ThriftMention)) }));
                //var expr = serializer.Deserialize<Func<IEnumerable<Datapoint>, double>>(XElement.Parse(str));
                //result = expr.Compile();
            }

            mergeFuncCache[str] = result;

            return result;
        }

        public string GetStatus(bool isInternal)
        {
            var reducers = HostEnvironment.Instance.GetAvailablePeerConnections(10000);
            ConcurrentDictionary<string, string> results = new ConcurrentDictionary<string, string>();

            List<Task> statusTasks = new List<Task>();
            foreach (var reducer in reducers)
            {
                Task t = new Task((r) =>
                {
                    var rie = r as PeerInfo;
                    string id = rie.Id.Split('_').LastOrDefault();

                    try
                    {
                        if (rie.Equals(Endpoint))
                        {
                            var memStatus = SystemInfo.GetMemoryStatusEx();
                            var usedMem = Math.Round((double)(memStatus.ullTotalPhys - memStatus.ullAvailPhys) / 1073741824d, 2); //Math.Round( (double)GC.GetTotalMemory(false) / 1073741824d, 4);
                            var availMem = Math.Round((double)memStatus.ullAvailPhys / 1073741824d, 2);
                            string sub = string.Format(" Memory Usage: Used={0}GB, Available={1}GB", usedMem, availMem);

                            long totalMentions = 0;

                            //foreach (var sql in SqlInterface.StoredSqlInterfaces)
                            foreach (var sql in ComputeNode.Node.Catalogs.Values)
                            {
                                var count = sql.GetCount(null);
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
                                using (var client = HostEnvironment.GetServiceClient(rie))
                                {
                                    results[id] = client.Ping("status");
                                }
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
        public IPeerInfo PeerEndpoint;
    }

 
}
