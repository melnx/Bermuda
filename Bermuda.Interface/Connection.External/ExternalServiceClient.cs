using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Diagnostics;
using Bermuda.Interface.Connection.Internal;

namespace Bermuda.Interface.Connection.External 
{
    public interface IExternalServiceChannel : IBermudaService, IClientChannel
    {
    }

    [DebuggerStepThrough]
    public partial class ExternalServiceClient : ClientBase<IExternalService>, IExternalService
    {

        public ExternalServiceClient()
        {
        }

        public ExternalServiceClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        public ExternalServiceClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public ExternalServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public ExternalServiceClient(System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        public string Ping(string param)
        {
            return base.Channel.Ping(param);
        }

        public BermudaCursor GetCursor(string domain, string query, string mapreduce, string merge, string paging, string command)
        {
            return base.Channel.GetCursor(domain, query, mapreduce, merge, paging, command);
        }

        public BermudaResult GetDataFromCursor(string cursor, string paging, string command)
        {
            return base.Channel.GetDataFromCursor(cursor, paging, command);
        }

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, string command)
        {
            return base.Channel.GetData(domain, query, mapreduce, merge, paging, command);
        }

        public string[] GetMetadataCatalogs()
        {
            return base.Channel.GetMetadataCatalogs();
        }

        public TableMetadataResult[] GetMetadataTables()
        {
            return base.Channel.GetMetadataTables();
        }

        public ColumnMetadataResult[] GetMetadataColumns()
        {
            return base.Channel.GetMetadataColumns();
        }

        public static ExternalServiceClient GetClient(string url)
        {
            var binding = new NetTcpBinding(SecurityMode.None);
            var endpoint = new EndpointAddress(new Uri(url));
            binding.AdjustForBermuda();

            var client = new ExternalServiceClient(binding, endpoint);

            return client;
        }
    }
}
