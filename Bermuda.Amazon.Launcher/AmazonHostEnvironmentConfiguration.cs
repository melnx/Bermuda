using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Interface;
using Bermuda.Core;
using System.Net;
using System.Net.Sockets;
using Amazon.EC2.Model;
using Amazon.EC2;
using Amazon;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Bermuda.Catalog;
using System.Diagnostics;

namespace Bermuda.Amazon.Launcher
{
    public class AmazonHostEnvironmentConfiguration : IHostEnvironmentConfiguration
    {
        class AmazonBermudaNode
        {
            public RunningInstance AmazonInstance;
            public bool IsCurrentInstance;
            public int NodeId;
            public string ClusterName;
            public IPeerInfo PeerInfo;
        }

        List<AmazonBermudaNode> AmazonBermudaNodes;

        const int INTERNAL_PORT = 20000;
        const int EXTERNAL_PORT = 13866;
        const string BERMUDA_NODE_ID_KEY = "bermuda:node-id";
        const string BERMUDA_CLUSTER_NAME_KEY = "bermuda:cluster-name";
        const string TERMINATED_NAME = "terminated";
        const string AmazonBucket = "BermudaConfig";

        //change this for different types
        ComputeNodeType NodeType = ComputeNodeType.MX;
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
                    default:
                        return "";
                }
            }
        }
        
        public AmazonHostEnvironmentConfiguration()
        {
            AmazonBermudaNodes = GetActiveClusterBermudaNodes();           
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

            switch(NodeType)
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
            }
            return list;
        }

        private List<AmazonBermudaNode> GetActiveClusterBermudaNodes()
        {
            AmazonEC2 ec2;
            ec2 = AWSClientFactory.CreateAmazonEC2Client();
            var resp = ec2.DescribeInstances(new DescribeInstancesRequest());
            var addresses = resp.DescribeInstancesResult.Reservation;

            var currentIP = CurrentIPAddress.ToString();
            var allAmazonBermudaNodes = new List<AmazonBermudaNode>();
            foreach (var a in addresses)
            {
                foreach (var ri in a.RunningInstance.Where(i => !string.IsNullOrWhiteSpace(i.PrivateIpAddress)))
                {
                    var newNode = new AmazonBermudaNode();
                    bool hasNodeId = false;
                    bool hasClusterName = false;
                    foreach (var tag in ri.Tag)
                    {
                        if (tag.Key == BERMUDA_NODE_ID_KEY)
                        {                      
                            newNode.AmazonInstance = ri;
                            newNode.IsCurrentInstance = ri.PrivateIpAddress == currentIP;
                            newNode.NodeId = Int32.Parse(tag.Value);
                            
                            var pi = new PeerInfo();
                            pi.Id = ri.InstanceId;
                            pi.EndPoint = new IPEndPoint(IPAddress.Parse(ri.PrivateIpAddress), INTERNAL_PORT);
                            newNode.PeerInfo = pi;

                            hasNodeId = true;
                        }
                        else if (tag.Key == BERMUDA_CLUSTER_NAME_KEY)
                        {
                            newNode.ClusterName = tag.Value;
                            hasClusterName = true;
                        }
                    }
                    if (hasNodeId && hasClusterName)
                    {
                        allAmazonBermudaNodes.Add(newNode);
                    }
                }
            }

            var activeClusterName = allAmazonBermudaNodes.Single(abn => abn.IsCurrentInstance).ClusterName;
            var activeClusterBermudaNodes = allAmazonBermudaNodes.Where(abn => abn.ClusterName == activeClusterName);

            return activeClusterBermudaNodes.ToList();    
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

        public IPEndPoint InternalServiceEndPoint
        {
            get
            {
                var addr = CurrentIPAddress;
                var ipEndpoint = new IPEndPoint(addr, INTERNAL_PORT);
                return ipEndpoint;
            }
        }

        public IPEndPoint ExternalServiceEndPoint
        {
            get
            {
                var addr = CurrentIPAddress;
                var ipEndpoint = new IPEndPoint(addr, EXTERNAL_PORT);
                return ipEndpoint;
            }
        }

        public IEnumerable<IPeerInfo> AllNodeEndpoints
        {
            get
            {
                return AmazonBermudaNodes.Select(abn => abn.PeerInfo);
            }
        }

        public IPeerInfo CurrentEndpoint
        {
            get
            {
                return AmazonBermudaNodes.Single(abn => abn.IsCurrentInstance).PeerInfo;
            }
        }

        public int CurrentInstanceIndex
        {
            get
            {
                return AmazonBermudaNodes.Single(abn => abn.IsCurrentInstance).NodeId;
            }
        }

        public string CurrentInstanceId
        {
            get
            {
                return AmazonBermudaNodes.Single(abn => abn.IsCurrentInstance).AmazonInstance.InstanceId;
            }
        }

        public IDictionary<long, string> GetInstanceAddressMap()
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            AmazonBermudaNodes.ForEach(a => map.Add(a.NodeId, a.PeerInfo.EndPoint.Address.ToString()));
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

            sb.AppendLine("ClusterName: " + AmazonBermudaNodes.Single(abn => abn.IsCurrentInstance).ClusterName);
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
