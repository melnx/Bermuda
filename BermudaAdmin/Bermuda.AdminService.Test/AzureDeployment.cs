using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Bermuda.AdminService.Test
{
    [DataContract]
    public class AzureDeployment
    {
        [DataMember]
        public string SubscriptionId { get; set; }

        [DataMember]
        public List<Byte> CertificateBytes { get; set; }

        [DataMember]
        public string ServiceName { get; set; }

        [DataMember]
        public string DeploymentName { get; set; }

        [DataMember]
        public string DeploymentSlot { get; set; }

        [DataMember]
        public string Label { get; set; }

        [DataMember]
        public int InstanceCount { get; set; }
    }
}