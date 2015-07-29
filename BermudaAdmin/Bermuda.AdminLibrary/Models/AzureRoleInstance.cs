using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "RoleInstance")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureRoleInstance")]
    public class AzureRoleInstance
    {
        [DataMember]
        [XmlElement("RoleName")]
        public String RoleName { get; set; }

        [DataMember]
        [XmlElement("InstanceName")]
        public String InstanceName { get; set; }

        [DataMember]
        [XmlElement("InstanceStatus")]
        public String InstanceStatus { get; set; }

        [DataMember]
        [XmlElement("InstanceUpgradeDomain")]
        public Int32 InstanceUpgradeDomain { get; set; }

        [DataMember]
        [XmlElement("InstanceFaultDomain")]
        public Int32 InstanceFaultDomain { get; set; }

        [DataMember]
        [XmlElement("InstanceSize")]
        public Int32 InstanceSize { get; set; }

        [DataMember]
        [XmlElement("InstanceStateDetails")]
        public String InstanceStateDetails { get; set; }

        [DataMember]
        [XmlElement("InstanceErrorCode")]
        public String InstanceErrorCode { get; set; }
    }
}
