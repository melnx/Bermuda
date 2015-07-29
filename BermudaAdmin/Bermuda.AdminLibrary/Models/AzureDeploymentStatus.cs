using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract]
    [Serializable]
    public class AzureDeploymentStatus
    {
        [DataMember]
        public String DeploymentStatus { get; set; }

        [DataMember]
        public Int32 RoleCount { get; set; }

        [DataMember]
        public Int32 InstanceCount { get; set; }
    }
}
