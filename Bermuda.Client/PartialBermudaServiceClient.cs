using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Entities;
using System.Linq.Expressions;
using ExpressionSerialization;
using Bermuda.Entities.Thrift;

namespace Bermuda.Client.ExternalAccess
{
    public partial class ExternalServiceClient
    {
        
        static ExpressionSerializer serializer = new ExpressionSerializer();
        
        //#####################################################

        public MentionResult GetMentionList(string domain, Expression<Func<Mention, object[], bool>> query, Expression<Func<IEnumerable<Mention>, IEnumerable<Mention>>> paging, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetMentionList(domain, serializer.Serialize(query).ToString(), serializer.Serialize(paging).ToString(), minDate, maxDate, parameters);
            return result;
        }

        public MentionResult GetMentionList(string domain, string query, string paging, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetMentions(domain, query, paging, minDate, maxDate, parameters);
            return new MentionResult { Mentions = result.Mentions == null ? new List<Mention>() : result.Mentions.ToList(), Metadata = result.Metadata };
        }

        //#####################################################

        public DatapointResult GetDatapointList(string domain, Expression<Func<Mention, object[], bool>> query, Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce, Expression<Func<IEnumerable<Datapoint>, double>> merge, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetDatapointList(domain, serializer.Serialize(query).ToString(), serializer.Serialize(mapreduce).ToString(), serializer.Serialize(merge).ToString(), minDate, maxDate, parameters);
            return result;
        }

        public DatapointResult GetDatapointList(string domain, string query, string mapreduce, string merge, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetDatapoints(domain, query, mapreduce, merge, minDate, maxDate, parameters);
            return new DatapointResult { Datapoints = result.Datapoints == null ? new List<Datapoint>() : result.Datapoints.ToList(), Metadata = result.Metadata };
        }

        //#####################################################

        List<ThriftDatapoint> DeserializeDatapoints(byte[] data)
        {
            return ThriftMarshaller.Deserialize<ThriftDatapointChunk>(data).Datapoints;
        }

        byte[] SerializeMentions(List<ThriftMention> mentions)
        {
            return ThriftMarshaller.Serialize<ThriftMentionChunk>(new ThriftMentionChunk { Mentions = mentions });
        }

        List<ThriftMention> DeserializeMentions(byte[] data)
        {
            return ThriftMarshaller.Deserialize<ThriftMentionChunk>(data).Mentions;
        }
    }

    public class MentionResult
    {
        public List<Mention> Mentions;
        public BermudaNodeStatistic Metadata;
        public long CreatedOn;
    }

    public class DatapointResult
    {
        public List<Datapoint> Datapoints;
        public BermudaNodeStatistic Metadata;
        public long CreatedOn;
    }
}