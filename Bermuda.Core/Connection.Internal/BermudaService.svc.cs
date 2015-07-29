using System;
using System.ServiceModel;
using System.Diagnostics;
using Bermuda.Core.MapReduce;
using Bermuda.Interface.Connection.Internal;
using Bermuda.Interface;

namespace Bermuda.Core.Connection.Internal
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, AddressFilterMode = AddressFilterMode.Any, InstanceContextMode = InstanceContextMode.Single)]
    //[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class BermudaService : IBermudaService
    {
        public string Ping(string param)
        {
            switch (param)
            {
                case "status":
                    return BermudaMapReduce.Instance.GetStatus(true);
            }
            return "0";
        }

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, int remdepth, string command, string cursor, string paging2)
        {
            BermudaResult result = null;
            BermudaNodeStatistic metadata = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                result = BermudaMapReduce.Instance.GetData(domain, query, mapreduce, merge, paging, remdepth, command, cursor, paging2);
                metadata = result.Metadata;
            }
            catch (BermudaException ex)
            {
                result = new BermudaResult { Error = ex.Message };
            }
            catch (Exception ex)
            {
                result = new BermudaResult { Error = ex.ToString() };
            }

            sw.Stop();
            if (metadata == null) metadata = new BermudaNodeStatistic();
            metadata.OperationTime = sw.Elapsed;
            result.Metadata = metadata;

            return result;
        }

        public BermudaResult GetDataFromCursor(string cursor, string paging, string command)
        {
            return GetData(null, null, null, null, null, 1, command, cursor, paging);
        }

        public BermudaCursor GetCursor(string domain, string query, string mapreduce, string merge, string paging, string command)
        {
            var data = GetData(domain, query, mapreduce, merge, paging, 1, command, null, null);

            return new BermudaCursor { CursorId = data.CacheKey, Error = data.Error };
        }

        //public void InsertMentions(string domain, string blob, byte[] mentions, int remdepth)
        //{
        //    var blobs = blob == null ? null : blob.Split('|').Select(x => Guid.Parse(x));
        //    var set = ThriftMarshaller.Deserialize<ThriftMentionChunk>(mentions);
        //    AzureMapReducer.Instance.InsertMentions(domain, set.Mentions, remdepth);
        //}

    }
}
