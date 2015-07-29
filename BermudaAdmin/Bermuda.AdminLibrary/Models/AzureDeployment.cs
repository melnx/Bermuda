using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "Deployment")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureDeployment")]
    public class AzureDeployment
    {
        [DataMember]
        [XmlElement]
        public String Name { get; set; }

        [DataMember]
        [XmlElement]
        public String DeploymentSlot { get; set; }

        [DataMember]
        [XmlElement]
        public String PrivateID { get; set; }

        [DataMember]
        [XmlElement]
        public String Status { get; set; }

        [DataMember]
        [XmlElement]
        public String Label { get; set; }

        [DataMember]
        [XmlElement]
        public String Url { get; set; }

        [DataMember]
        [XmlElement]
        public String Configuration { get; set; }

        [DataMember]
        [XmlArray("RoleInstanceList", Form = XmlSchemaForm.Unqualified)]
        [XmlArrayItem(typeof(AzureRoleInstance))]
        public List<AzureRoleInstance> RoleInstanceList { get; set; }

        [DataMember]
        [XmlElement]
        public AzureUpgradeStatus UpgradeStatus { get; set; }

        [DataMember]
        [XmlElement]
        public Int32 UpgradeDomainCount { get; set; }

        [DataMember]
        [XmlArray("RoleList", Form = XmlSchemaForm.Unqualified)]
        [XmlArrayItem(typeof(AzureRole))]
        List<AzureRole> RoleList { get; set; }

        [DataMember]
        [XmlElement]
        public String SdkVersion { get; set; }

        [DataMember]
        [XmlArray("InputEndpointList", Form=XmlSchemaForm.Unqualified)]
        [XmlArrayItem(typeof(AzureInputEndpoint))]
        public List<AzureInputEndpoint> InputEndpointList { get; set; }

        [DataMember]
        [XmlElement]
        public Boolean Locked { get; set; } // Deployment Write Allowed Status

        [DataMember]
        [XmlElement]
        public Boolean RollbackAllowed { get; set; } // Rollback Operation Allowed
    }
}
