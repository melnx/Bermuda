using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "Role")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureRole")]
    public class AzureRole
    {
        [DataMember]
        [XmlElement("RoleName")]
        public String RoleName { get; set; }

        [DataMember]
        [XmlElement("OsVersion")]
        public String OsVersion { get; set; }
    }
}
