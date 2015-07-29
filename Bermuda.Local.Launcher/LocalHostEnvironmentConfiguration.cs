using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Net;
using System.Net.Sockets;
using Bermuda.Core;
using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Bermuda.Catalog;
using Bermuda.NetSaturator;

namespace Bermuda.Local.Launcher
{
    public class LocalHostEnvironmentConfiguration : IHostEnvironmentConfiguration
    {
        const int INTERNAL_PORT = 20000;
        const int EXTERNAL_PORT = 13866;
        const string AmazonBucket = "BermudaConfig";

        //change this for different types
        public ComputeNodeType NodeType = ComputeNodeType.Net;
        private string BermudaConfig
        {
            get
            {
                switch (NodeType)
                {
                    case ComputeNodeType.MX:
#if DEBUG
                        return "BermudaMXTest.Config";
#else
                        return "BermudaMX.Config";
#endif
                    case ComputeNodeType.WeatherData:
                        return "BermudaWeather.Config";
                    case ComputeNodeType.WikiData:
                        return "BermudaWikipedia.Config";
                    case ComputeNodeType.Combined:
                        return "BermudaCombined.Config";
                    case ComputeNodeType.Net:
                    case ComputeNodeType.NetGateway:
                        return "BermudaUDPTest.Config";
                    default:
                        return "";
                }
            }
        }

        public LocalHostEnvironmentConfiguration()
        {
            
        }

        
        public IComputeNode GetComputeNode()
        {
            IComputeNode compute_node = null;
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //download request
                    using (var response = client.GetObject(new GetObjectRequest()
                        .WithBucketName(AmazonBucket)
                        .WithKey(BermudaConfig)))
                    {
                        using (StreamReader reader = new StreamReader(response.ResponseStream))
                        {
                            //read the file
                            string data = reader.ReadToEnd();

                            //deserialize
                            compute_node = new ComputeNode().DeserializeComputeNode(data);
                            if(compute_node.Catalogs.Values.Cast<ICatalog>().FirstOrDefault().CatalogMetadata.Tables.FirstOrDefault().Value.DataType == null)
                                compute_node.Catalogs.Values.Cast<ICatalog>().FirstOrDefault().CatalogMetadata.Tables.FirstOrDefault().Value.DataType = typeof(UDPTestDataItems);
                            compute_node.Init(CurrentInstanceIndex, AllNodeEndpoints.Count());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return compute_node;
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
                    //IFileProcessor wiki_proc = new FileSaturator.S3FileProcessor()
                    //    {
                    //        //LineProcessor = null,
                    //        AmazonBucket = "PublicDataSets",
                    //        AmazonPrefix = "wikistats/pagecounts/",
                    //    };
                    //list.Add(new FileSaturator.FileSaturator(compute_node, wiki_proc, "WikipediaData", "PageStats"));
                    break;
                case ComputeNodeType.Combined:
                    list.Add(new DatabaseSaturator.DatabaseSaturator(compute_node));
                    list.Add(new DataPurge.PurgeProcessor(compute_node));
                    //list.Add(new FileSaturator.FileSaturator(compute_node));
                    list.Add(new S3Saturator.S3Saturator(compute_node));
                    break;
                case ComputeNodeType.Net:
                    list.Add(new NetSaturator.NetSaturator(
                        compute_node,
                        GetInstanceAddressMap(),
                        "UDPTest",
                        "UDPTest",
                        new NetComUDP(12345)));
                    break;
                case ComputeNodeType.NetGateway:
                    list.Add(new NetSaturator.NetGateway(
                        compute_node,
                        GetInstanceAddressMap(),
                        "UDPTest",
                        "UDPTest",
                        new NetComUDP(1234, 12345)));
                    break;
            }
            return list;
        }

        private IPAddress CurrentIPAddress
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress addr = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                if (addr == null)
                    throw new Exception("No valid host address found");
                return addr;
            }
        }

        public IPeerInfo CurrentEndpoint
        {
            get 
            {
                return new PeerInfo()
                {
                    EndPoint = new IPEndPoint(CurrentIPAddress, INTERNAL_PORT),
                    Id = "0"
                };
            }
        }

        public System.Net.IPEndPoint InternalServiceEndPoint
        {
            get
            {
                var addr = CurrentIPAddress;
                var ipEndpoint = new IPEndPoint(addr, INTERNAL_PORT);
                return ipEndpoint;
            }
        }

        public System.Net.IPEndPoint ExternalServiceEndPoint
        {
            get
            {
                var addr = CurrentIPAddress;
                var ipEndpoint = new IPEndPoint(addr, EXTERNAL_PORT);
                return ipEndpoint;
            }
        }

        public string CurrentInstanceId
        {
            get { return "0"; }
        }

        public int CurrentInstanceIndex
        {
            get { return 0; }
        }

        public IEnumerable<IPeerInfo> AllNodeEndpoints
        {
            get 
            {
                List<IPeerInfo> list = new List<IPeerInfo>();
                list.Add(new PeerInfo()
                {
                    EndPoint = new IPEndPoint(CurrentIPAddress, INTERNAL_PORT),
                    Id = "0"
                });

                //list.Add(new PeerInfo()
                //{
                //    EndPoint = new IPEndPoint(CurrentIPAddress, INTERNAL_PORT),
                //    Id = "1"
                //});

                //list.Add(new PeerInfo()
                //{
                //    EndPoint = new IPEndPoint(CurrentIPAddress, INTERNAL_PORT),
                //    Id = "2"
                //});

                //list.Add(new PeerInfo()
                //{
                //    EndPoint = new IPEndPoint(CurrentIPAddress, INTERNAL_PORT),
                //    Id = "3"
                //});

                return list;
            }
        }

        public IDictionary<long, string> GetInstanceAddressMap()
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            map.Add(0, "127.0.0.1");
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

            sb.AppendLine("ClusterName: " + "Local");
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
