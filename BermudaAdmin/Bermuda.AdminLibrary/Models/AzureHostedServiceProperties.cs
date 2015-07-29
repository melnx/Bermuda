using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "HostedServiceProperties")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureHostedServiceProperties")]
    public class AzureHostedServiceProperties
    {
        [DataMember]
        [XmlElement("Description")]
        public String Description { get; set; }

        [DataMember]
        [XmlElement("Location")]
        public String Location { get; set; }  // i.e. North Central US

        [DataMember]
        [XmlElement("AffinityGroup")]
        public String AffinityGroup { get; set; }

        [DataMember]
        [XmlElement("Label")]
        public String Label { get; set; }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
