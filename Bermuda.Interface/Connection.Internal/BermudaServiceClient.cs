using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Diagnostics;

namespace Bermuda.Interface.Connection.Internal
{
    public interface IBermudaServiceChannel : IBermudaService, IClientChannel
    {
    }

    [DebuggerStepThrough]
    public partial class BermudaServiceClient : ClientBase<IBermudaService>, IBermudaService
    {

        public BermudaServiceClient()
        {
        }

        public BermudaServiceClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        public BermudaServiceClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public BermudaServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public BermudaServiceClient(System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress) :
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

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, int remdepth, string command, string cursor, string paging2)
        {
            return base.Channel.GetData(domain, query, mapreduce, merge, paging, remdepth, command, cursor, paging2);
        }

        public static BermudaServiceClient GetClient(string url)
        {
            var binding = new NetTcpBinding(SecurityMode.None);
            var endpoint = new EndpointAddress(new Uri(url));
            binding.AdjustForBermuda();

            var client = new BermudaServiceClient(binding, endpoint);

            return client;
        }

        
    }
}