using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.Threading;
using Bermuda.Interface;
using Bermuda.Core.Connection.Internal;
using Bermuda.Interface.Connection.Internal;
using Bermuda.Core.Connection.External;
using Bermuda.Interface.Connection.External;
using Bermuda.Catalog;
using Bermuda.DataPurge;
using System.IO;

namespace Bermuda.Core
{
    public class HostEnvironment : IHostEnvironment
    {
        public readonly TimeSpan PeerCheckInterval = TimeSpan.FromSeconds(5);
        private IEnumerable<IPeerInfo> _cachedPeerConnections;
        private DateTime _lastPeerCheck = DateTime.MinValue;

        ServiceHost internalServiceHost;
        ServiceHost externalServiceHost;

        IComputeNode computeNode;
        IDataProcessor saturator;

        const int BUCKET_COUNT = 1000;
        const int BUCKET_COUNT_DEBUG = 100;

        List<IDataProcessor> processors = new List<IDataProcessor>();

        static IHostEnvironment _instance;
        public static IHostEnvironment Instance
        {
            get
            {
                return _instance ?? (_instance = new HostEnvironment());
            }
        }

        //singleton constructor
        private HostEnvironment() { }

        public IHostEnvironmentConfiguration HostEnvironmentConfiguration { get; private set; }

        public IPeerInfo CurrentEndpoint
        {
            get
            {
                return HostEnvironmentConfiguration.CurrentEndpoint;
            }
        }

        public string CurrentInstanceId
        {
            get
            {
                return HostEnvironmentConfiguration.CurrentInstanceId;
            }
        }

        public void Initialize(IHostEnvironmentConfiguration hostEnvConfig)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;

            this.HostEnvironmentConfiguration = hostEnvConfig;
            this.StartInternalService(hostEnvConfig.InternalServiceEndPoint);
            this.StartExternalService(hostEnvConfig.ExternalServiceEndPoint);

            int bucket_count = BUCKET_COUNT;

#if DEBUG
            Thread.Sleep(2000);
            bucket_count = BUCKET_COUNT_DEBUG;
#else
            Thread.Sleep(10000);
#endif

            //ComputeNodeType NodeType = ComputeNodeType.MX;
            //switch (NodeType)
            //{
            //    case ComputeNodeType.MX:
            //        computeNode = new ComputeNodeMX.ComputeNodeMX(this.HostEnvironmentConfiguration.CurrentInstanceIndex, bucket_count, this.HostEnvironmentConfiguration.AllNodeEndpoints.Count());
            //        computeNode.MinAvailableMemoryPercent = 20;
            //        computeNode.MaxAvailableMemoryPercent = 30;
            //        processors.Add(new DatabaseSaturator.DatabaseSaturator(computeNode));
            //        processors.Add(new PurgeProcessor(computeNode));
            //        break;
            //    case ComputeNodeType.WeatherData:
            //        //Weather data compute node
            //        computeNode = new ComputeNodeWX.ComputeNodeWX(this.HostEnvironmentConfiguration.CurrentInstanceIndex, bucket_count, this.HostEnvironmentConfiguration.AllNodeEndpoints.Count());
            //        computeNode.MinAvailableMemoryPercent = 20;
            //        computeNode.MaxAvailableMemoryPercent = 30;
            //        processors.Add(new FileSaturator.FileSaturator(computeNode));
            //        break;
            //    case ComputeNodeType.WikiData:
            //        //Wikipedia data compute node
            //        computeNode = new ComputeNodeWikiData.ComputeNodeWikiData(this.HostEnvironmentConfiguration.CurrentInstanceIndex, bucket_count, this.HostEnvironmentConfiguration.AllNodeEndpoints.Count());
            //        computeNode.MinAvailableMemoryPercent = 20;
            //        computeNode.MaxAvailableMemoryPercent = 30;
            //        processors.Add(new S3Saturator.S3Saturator(computeNode));
            //        break;
            //}

            //string json = computeNode.SerializeComputeNode();
            //IComputeNode temp = computeNode.DeserializeComputeNode(json);
            //File.WriteAllText("c:\\temp\\BermudaMX.Config", json);

            computeNode = this.HostEnvironmentConfiguration.GetComputeNode();
            processors.AddRange(this.HostEnvironmentConfiguration.GetProcessors(computeNode));
            processors.ForEach(p => p.StartProcessor());
        }

        public void Shutdown()
        {
            this.internalServiceHost.Close();
            this.externalServiceHost.Close();
            processors.ForEach(p => p.StopProcessor());
        }

        private void StartInternalService(IPEndPoint endpoint)
        {
            internalServiceHost = new ServiceHost(typeof(BermudaService));

            var binding = new NetTcpBinding(SecurityMode.None);
            var internalEndpoint = String.Format("net.tcp://{0}/BermudaService.svc", endpoint);
            AdjustBinding(binding);

            Trace.WriteLine("[STARTING] internal webservice: " + endpoint);

            var sep = internalServiceHost.AddServiceEndpoint(typeof(IBermudaService), binding, internalEndpoint);

            internalServiceHost.Open();
            Trace.WriteLine("[STARTED] internal webservice: " + endpoint);
        }

        private void StartExternalService(IPEndPoint endpoint)
        {
            externalServiceHost = new ServiceHost(typeof(ExternalService));

            var binding = new NetTcpBinding(SecurityMode.None);
            var externalEndpoint = String.Format("net.tcp://{0}/ExternalService.svc", endpoint);
            AdjustBinding(binding);

            Trace.WriteLine("[STARTING] external webservice: " + endpoint);

            var sep = externalServiceHost.AddServiceEndpoint(typeof(IExternalService), binding, externalEndpoint);

            externalServiceHost.Open();
            Trace.WriteLine("[STARTED] external webservice: " + endpoint);
        }

        public static void AdjustBinding(NetTcpBinding binding)
        {
            binding.ReaderQuotas.MaxBytesPerRead *= 100;
            binding.ReaderQuotas.MaxStringContentLength *= 100;
            binding.ReaderQuotas.MaxArrayLength *= 100;
            binding.MaxReceivedMessageSize *= 100;
            binding.MaxConnections = 100;
        }

        public IEnumerable<IDataProvider> GetBucketInterfacesForDomain(string domain, IEnumerable<string> blobNames = null)
        {
            return ComputeNode.Node.GetCatalogs(domain);
        }

        public static BermudaServiceClient GetServiceClient(IPeerInfo peer)
        {
            BermudaServiceClient client = null;
        
            var url = String.Format("net.tcp://{0}/BermudaService.svc", peer.EndPoint);
            var binding = new NetTcpBinding(SecurityMode.None);
            var endpoint = new EndpointAddress(new Uri(url));
            HostEnvironment.AdjustBinding(binding);
            client = new BermudaServiceClient(binding, endpoint);

            return client;
        }       

        public IEnumerable<IPeerInfo> GetAvailablePeerConnections(int count)
        {
            if( _cachedPeerConnections == null || (DateTime.Now - _lastPeerCheck) > PeerCheckInterval )
            {
                _cachedPeerConnections = HostEnvironmentConfiguration.AllNodeEndpoints;
                _lastPeerCheck = DateTime.Now;
            }

            return _cachedPeerConnections.Take(count);
        }

        public IEnumerable<IPeerInfo> GetAvailablePeerConnections()
        {
            return GetAvailablePeerConnections(9999);
        }
    }
}
