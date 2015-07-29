using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using Bermuda.Interface;
using Bermuda.Core;
using System.Net;
using System.Text;
using Microsoft.WindowsAzure;
using System.Configuration;
using Microsoft.WindowsAzure.StorageClient;
using Bermuda.Catalog;
using System.Diagnostics;
using System.IO;
using Bermuda.NetSaturator;

namespace Bermuda.Azure.WebRole
{
    public class AzureHostEnvironmentConfiguration : IHostEnvironmentConfiguration
    {
        const string INTERNAL_ENDPOINT_NAME = "InternalTCPEndpoint";
        const string EXTERNAL_ENDPOINT_NAME = "ExternalTCPEndpoint";
        const string AzureContainer = "bermudaconfig";

        //change this for different types
        public ComputeNodeType NodeType = ComputeNodeType.MX;
        private string BermudaConfig
        {
            get
            {
                switch (NodeType)
                {
                    case ComputeNodeType.MX:
#if DEBUG
                        return "BermudaSmall.Config";
#else
                        return "BermudaMX.Config";
#endif
                    case ComputeNodeType.WeatherData:
                        return "BermudaWeather.Config";
                    case ComputeNodeType.WikiData:
                        return "BermudaWikipedia.Config";
                    case ComputeNodeType.Net:
                        return "BermudaUDPTest.Config";
                    default:
                        return "";
                }
            }
        }

        public AzureHostEnvironmentConfiguration()
        {
            //CombineComputeNodes();
        }

        public IComputeNode GetComputeNode()
        {
            IComputeNode compute_node = null;
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageAccount.ConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(AzureContainer);
                container.CreateIfNotExist();
                CloudBlob blob = container.GetBlobReference(BermudaConfig);
                string data = blob.DownloadText();

                //deserialize
                compute_node = new ComputeNode().DeserializeComputeNode(data);
                if (compute_node.Catalogs.Values.Cast<ICatalog>().FirstOrDefault().CatalogMetadata.Tables.FirstOrDefault().Value.DataType == null)
                    compute_node.Catalogs.Values.Cast<ICatalog>().FirstOrDefault().CatalogMetadata.Tables.FirstOrDefault().Value.DataType = typeof(UDPTestDataItems);
                compute_node.Init(CurrentInstanceIndex, AllNodeEndpoints.Count());
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return compute_node;
        }

        private void CombineComputeNodes()
        { 
            NodeType = ComputeNodeType.MX;
            IComputeNode mx = GetComputeNode();
            NodeType = ComputeNodeType.WeatherData;
            IComputeNode weather = GetComputeNode();
            NodeType = ComputeNodeType.WikiData;
            IComputeNode wiki = GetComputeNode();
            weather.Catalogs.Values.ToList().ForEach(c => mx.Catalogs.Add(c.Name, c));
            wiki.Catalogs.Values.ToList().ForEach(c => mx.Catalogs.Add(c.Name, c));
            string json = mx.SerializeComputeNode();
            File.WriteAllText("c:\\Temp\\BermudaCombined.Config", json);
        }

        public IEnumerable<IDataProcessor> GetProcessors(IComputeNode compute_node)
        {
            List<IDataProcessor> list = new List<IDataProcessor>();

            switch (NodeType)
            {
                case ComputeNodeType.MX:
                    list.Add(new DatabaseSaturator.DatabaseSaturator(compute_node));
                    list.Add(new DataPurge.PurgeProcessor(compute_node));
                    break;
                case ComputeNodeType.WeatherData:
                    var weather_filesroot = @"c:\code\EvoApp\Bermuda\Bermuda.FileSaturator\weatherdata";
                    IFileProcessor weather_proc = new FileSaturator.FileSystemFileProcessor()
                        {
                            LineProcessor = new FileSaturator.WeatherLineProcessor(),
                            FilesRootPath = weather_filesroot,
                        };
                    list.Add(new FileSaturator.FileSaturator(compute_node, weather_proc, "Weather", "Weather"));
                    break;
                case ComputeNodeType.WikiData:
                    list.Add(new S3Saturator.S3Saturator(compute_node));
                    break;
                case ComputeNodeType.Net:
                    list.Add(new NetSaturator.NetSaturator(compute_node,
                        GetInstanceAddressMap(),
                        "UDPTest",
                        "UDPTest",
                        new NetComUDP(1234, 12345)));
                    break;
            }
            return list;
        }

        public IPEndPoint InternalServiceEndPoint
        {
            get
            {
                return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[INTERNAL_ENDPOINT_NAME].IPEndpoint;
            }
        }

        public IPEndPoint ExternalServiceEndPoint
        {
            get
            {
                return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[EXTERNAL_ENDPOINT_NAME].IPEndpoint;
            }
        }

        public IEnumerable<IPeerInfo> AllNodeEndpoints
        {
            get
            {
                var endpoints = RoleEnvironment.CurrentRoleInstance.Role.Instances.Select(x => x.InstanceEndpoints[INTERNAL_ENDPOINT_NAME]);
                return endpoints.Select(x => new PeerInfo { EndPoint = x.IPEndpoint, Id = x.RoleInstance.Id });
            }
        }

        public IPeerInfo CurrentEndpoint
        {
            get
            {
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[INTERNAL_ENDPOINT_NAME];
                return new PeerInfo { EndPoint = endpoint.IPEndpoint, Id = endpoint.RoleInstance.Id };
            }
        }

        int? _currentInstanceIndex;
        public int CurrentInstanceIndex
        {
            get
            {
                return (_currentInstanceIndex ?? (_currentInstanceIndex = int.Parse(CurrentInstanceId))).Value;
            }
        }

        string _currentInstanceId;
        public string CurrentInstanceId
        {
            get
            {
                return _currentInstanceId ?? (_currentInstanceId = RoleEnvironment.CurrentRoleInstance.Id.Split('_').LastOrDefault());
            }
        }

        public IDictionary<long, string> GetInstanceAddressMap()
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            RoleEnvironment.CurrentRoleInstance.Role.Instances.Select(x => x.InstanceEndpoints[INTERNAL_ENDPOINT_NAME])
                .ToList().ForEach(a => map.Add(long.Parse(a.RoleInstance.Id.Split('_').LastOrDefault()), a.IPEndpoint.Address.ToString()));
            return map;
        }

        //public ComputeNodeType ComputeNodeType
        //{
        //    get
        //    {
        //        return ComputeNodeType.MX;
        //    }
        //}

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("CurrentInstanceId: " + CurrentInstanceId);
            sb.AppendLine("CurrentInstanceIndex: " + CurrentInstanceIndex);
            sb.AppendLine("CurrentEndpoint: " + CurrentEndpoint.ToString());
            sb.AppendLine("InternalServiceEndPoint: " + InternalServiceEndPoint.ToString());
            sb.AppendLine("ExternalServiceEndPoint: " + ExternalServiceEndPoint.ToString());
            sb.AppendLine("AllNodeEndpoints:");
            foreach (var ep in AllNodeEndpoints)
            {
                sb.AppendLine("\t" + ep.ToString());
            }

            return sb.ToString();
        }
    }
}
