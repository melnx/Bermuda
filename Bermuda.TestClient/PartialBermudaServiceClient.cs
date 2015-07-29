using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Entities;
using System.Linq.Expressions;
using ExpressionSerialization;
using Bermuda.Entities.Thrift;

namespace Bermuda.TestClient.ExternalAccess
{
    public partial class ExternalServiceClient
    {
        
        static ExpressionSerializer serializer = new ExpressionSerializer();
        
        //#####################################################

        public List<ThriftMention> GetMentionList(string domain, Expression<Func<ThriftMention, object[], bool>> query, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetMentionList(domain, serializer.Serialize(query).ToString(), minDate, maxDate, parameters);
            return result;
        }

        public List<ThriftMention> GetMentionList(string domain, string query, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetMentions(domain, query, minDate, maxDate, parameters);
            return DeserializeMentions(result);
        }

        //#####################################################

        public List<ThriftDatapoint> GetDatapointList(string domain, Expression<Func<ThriftMention, object[], bool>> query, Expression<Func<IEnumerable<ThriftMention>, IEnumerable<ThriftDatapoint>>> mapreduce, Expression<Func<IEnumerable<ThriftDatapoint>, double>> merge, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetDatapointList(domain, serializer.Serialize(query).ToString(), serializer.Serialize(mapreduce).ToString(), serializer.Serialize(merge).ToString(), minDate, maxDate, parameters);
            return result;
        }

        public List<ThriftDatapoint> GetDatapointList(string domain, string query, string mapreduce, string merge, DateTime minDate, DateTime maxDate, object[] parameters)
        {
            var result = GetDatapoints(domain, query, mapreduce, merge, minDate, maxDate, parameters);
            return DeserializeDatapoints(result);
        }

        //#####################################################

        public void InsertMentions(string domain, List<ThriftMention> mentions)
        {
            var result = SerializeMentions(mentions);
            InsertMentions(domain, result);
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
}