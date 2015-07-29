using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "UpgradeStatus")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureUpgradeStatus")]
    public class AzureUpgradeStatus
    {
        [DataMember]
        [XmlElement("UpgradeType")]
        public String UpgradeType { get; set; } // Auto|Manual
        
        [DataMember]
        [XmlElement("CurrentUpgradeDomainState")]
        public String CurrentUpgradeDomainState { get; set; } // Before|During

        [DataMember]
        [XmlElement("CurrentUpgradeDomain")]
        public String CurrentUpgradeDomain { get; set; }
    }
}
