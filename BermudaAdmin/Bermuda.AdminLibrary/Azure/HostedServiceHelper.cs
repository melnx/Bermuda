using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Bermuda.AdminLibrary.Utility;
using System.Diagnostics;
using System.Xml.Serialization;
using Bermuda.AdminLibrary.Models;
using System.IO;
using System.Xml;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Bermuda.AdminLibrary.Azure
{
    public class HostedServiceHelper
    {
        #region Private Members
        private XNamespace wa = "http://schemas.microsoft.com/windowsazure";

        private String createHostedServiceFormat = "https://management.core.windows.net/{0}/services/hostedservices";

        private String getServiceOperationFormat = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";
        #endregion Private Members

        #region Private Methods
        private XDocument CreatePayload(String serviceName, String label, String description,
                                        String location, String affinityGroup)
        {
            XDocument payload = null;

            try
            {
                String base64LabelName = Base64Utility.ConvertToBase64String(label);

                XElement xServiceName = new XElement(wa + "ServiceName", serviceName);

                XElement xLabel = new XElement(wa + "Label", base64LabelName);

                XElement xDescription = new XElement(wa + "Description", description);

                XElement xLocation = new XElement(wa + "Location", location);

                XElement xAffinityGroup = new XElement(wa + "AffinityGroup", affinityGroup);

                XElement createHostedService = new XElement(wa + "CreateHostedService");

                createHostedService.Add(xServiceName);
                createHostedService.Add(xLabel);
                createHostedService.Add(xDescription);

                // We should have one or the other of location and affinityGroup,
                // not both.
                if (string.IsNullOrWhiteSpace(location) == true && 
                    string.IsNullOrWhiteSpace(affinityGroup) == false)
                {
                    createHostedService.Add(xAffinityGroup);
                }
                else if (string.IsNullOrWhiteSpace(location) == false && 
                         string.IsNullOrWhiteSpace(affinityGroup) == true)
                {
                    createHostedService.Add(xLocation);
                }

                payload = new XDocument();

                payload.Add(createHostedService);

                payload.Declaration = new XDeclaration("1.0", "UTF-8", "no");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in CreatePayload()  Error: {0}", ex.Message));

                payload = null;
            }

            return payload;
        }
        #endregion Private Methods

        #region Public Methods
        public String CreateHostedService(String subscriptionId, String thumbprint,
                                           String serviceName, String label,
                                           String description, String location, String affinityGroup)
        {
            String requestId = string.Empty;

            try
            {
                String uri = String.Format(createHostedServiceFormat, subscriptionId);

                XDocument payload = CreatePayload(serviceName, label, description, location, affinityGroup);

                ServiceManagementOperation operation = new ServiceManagementOperation(thumbprint);

                requestId = operation.Invoke(uri, payload);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in CreateHostedService()  Error: {0}", ex.Message));

                requestId = string.Empty;
            }

            return requestId;
        }


        public HostedService GetHostedService(String subscriptionId, List<Byte> certificateBytes, String serviceName)
        {
            HostedService azureService = null;

            try
            {
                String uri = String.Format(getServiceOperationFormat, subscriptionId, serviceName);

                ServiceManagementOperation operation = new ServiceManagementOperation(certificateBytes);

                XDocument hostedServiceProperties = operation.Invoke(uri);

                XmlSerializer serializer = new XmlSerializer(typeof(Bermuda.AdminLibrary.Models.HostedService));

                MemoryStream memStream = new MemoryStream();

                XmlWriter writer = XmlWriter.Create(memStream);

                hostedServiceProperties.Save(writer);

                writer.Close();

                memStream.Seek(0, SeekOrigin.Begin);

                azureService = (HostedService)serializer.Deserialize(memStream);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in GetHostedService()  Error: {0}", ex.Message));

                azureService = null;
            }

            return azureService;
        }
        #endregion Public Methods
    }
}
