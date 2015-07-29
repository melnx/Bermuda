using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace Bermuda.AdminLibrary.Models
{
    [DataContract(Name = "HostedService")]
    [Serializable]
    [XmlType("Bermuda.AdminLibrary.Models.AzureHostedService")]
    public class AzureHostedService
    {
        [DataMember]
        [XmlElement]
        public String Url { get; set; }

        [DataMember]
        [XmlElement]
        public String ServiceName { get; set; }

        [DataMember]
        [XmlElement]
        public AzureHostedServiceProperties HostedServiceProperties { get; set; }

        [DataMember]
        [XmlArray("Deployments", Form = XmlSchemaForm.Unqualified)]
        [XmlArrayItem(typeof(AzureDeployment))]
        List<AzureDeployment> Deployments { get; set; }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            reader.MoveToContent();
            Url = reader.GetAttribute("Url");
            ServiceName = reader.GetAttribute("ServiceName");

            Boolean isEmptyElement = reader.IsEmptyElement; // (1)
            
            reader.ReadStartElement();
            
            if (!isEmptyElement) // (1)
            {
                HostedServiceProperties = reader.ReadContentAs(typeof(AzureHostedServiceProperties), null) as AzureHostedServiceProperties;

                reader.ReadEndElement();
            }

            isEmptyElement = reader.IsEmptyElement; // (1)

            reader.ReadStartElement();

            if (!isEmptyElement) // (1)
            {
                Deployments = reader.ReadContentAs(typeof(List<AzureDeployment>), null) as List<AzureDeployment>;

                reader.ReadEndElement();
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString("Url", Url);
            writer.WriteAttributeString("ServiceName", ServiceName);
            
            writer.WriteStartAttribute("HostedServiceProperties");
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("Deployments");
            writer.WriteEndAttribute();
        }

        // This is the method named by the XmlSchemaProviderAttribute applied to the type.
        public static XmlQualifiedName GetSchema(XmlSchemaSet xs)
        {
            return new XmlQualifiedName("xmlns=\"http://schemas.microsoft.com/windowsazure\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"");
        }
    }
}
