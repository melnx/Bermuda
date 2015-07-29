using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "InputEndpoint")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureInputEndpoint")]
    public class AzureInputEndpoint
    {
        [DataMember]
        [XmlElement("RoleName")]
        public String RoleName { get; set; }

        [DataMember]
        [XmlElement("Vip")]
        public String Vip { get; set; } // Virtual IP Address

        [DataMember]
        [XmlElement("Port")]
        public Int32 Port { get; set; } // Port Number
    }
}
