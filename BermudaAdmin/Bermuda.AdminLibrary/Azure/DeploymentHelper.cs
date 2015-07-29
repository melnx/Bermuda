using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using Bermuda.AdminLibrary.Utility;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using System.Diagnostics;
using Bermuda.AdminLibrary.Models;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System.Xml.Serialization;
using System.Xml;

namespace Bermuda.AdminLibrary.Azure
{
    public class DeploymentHelper
    {
        #region Private Members
        private XNamespace wa = "http://schemas.microsoft.com/windowsazure";

        private String createDeploymentFormat = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/{2}";

        private String deleteDeploymentFormat = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/{2}";

        private String getOperationStatusFormat = "https://management.core.windows.net/{0}/operations/{1}";
        #endregion Private Members

        #region Private Methods
        private XDocument CreatePayload(String deploymentName, String packageUrl,
                                        String configurationString, String label)
        {
            XDocument payload = null;

            try
            {
                String base64ConfigurationString = Base64Utility.ConvertToBase64String(configurationString);

                String base64Label = Base64Utility.ConvertToBase64String(label);

                XElement xName = new XElement(wa + "Name", deploymentName);

                XElement xPackageUrl = new XElement(wa + "PackageUrl", packageUrl);

                XElement xLabel = new XElement(wa + "Label", base64Label);

                XElement xConfiguration = new XElement(wa + "Configuration", base64ConfigurationString);

                XElement xStartDeployment = new XElement(wa + "StartDeployment", "true");

                XElement xTreatWarningsAsError = new XElement(wa + "TreatWarningsAsError", "false");

                XElement createDeployment = new XElement(wa + "CreateDeployment");

                createDeployment.Add(xName);
                createDeployment.Add(xPackageUrl);
                createDeployment.Add(xLabel);
                createDeployment.Add(xConfiguration);
                createDeployment.Add(xStartDeployment);
                createDeployment.Add(xTreatWarningsAsError);

                payload = new XDocument();

                payload.Add(createDeployment);

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
        public String CreateDeployment(String subscriptionId, List<Byte> certificateBytes, String serviceName,
                                        String deploymentName, String deploymentSlot, String label, int instanceCount = 1)
        {
            String requestId = string.Empty;

            try
            {
                // Go read the Blob Urls for the Azure Deployment Package and
                // Configuration files.
                List<CloudBlob> blobList = AzureStorageUtility.ListBlobs();

                string configUri = string.Empty;
                string packageUri = string.Empty;

                foreach (CloudBlob blob in blobList)
                {
                    if (blob.Attributes.Uri.ToString().ToLower().EndsWith(".cscfg"))
                    {
                        configUri = blob.Attributes.Uri.ToString();

                        Logger.Write("Config Url: " + configUri);
                    }

                    if (blob.Attributes.Uri.ToString().ToLower().EndsWith(".cspkg"))
                    {
                        packageUri = blob.Attributes.Uri.ToString();

                        Logger.Write("Package Url: " + packageUri);
                    }
                }

                // Construct paths to Configuration File in Blob Storage
                string deploymentPackageFolder = ConfigurationManager.AppSettings["DeploymentPackageFolder"].ToString();

                string fileName = configUri.ToString().Substring(configUri.IndexOf("/" + deploymentPackageFolder + "/") + deploymentPackageFolder.Length + 2);

                Logger.Write("Config File Name: " + fileName);

                string configurationString = AzureStorageUtility.ReadBlobFile(fileName);

                // Strip off first non-viewable character
                configurationString = configurationString.Substring(1);

                Logger.Write("Configuration String: " + configurationString);

                // Change instance count to the selected amount
                AzurePackageManager azPackageMgr = new AzurePackageManager();

                string modifiedConfigurationString = azPackageMgr.ChangeDeploymentInstanceCount(instanceCount, configurationString);

                String uri = String.Format(createDeploymentFormat, subscriptionId, serviceName, deploymentSlot);

                XDocument payload = CreatePayload(deploymentName, packageUri, modifiedConfigurationString, label);

                ServiceManagementOperation operation = new ServiceManagementOperation(certificateBytes);

                requestId = operation.Invoke(uri, payload);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in CreateDeployment()  Error: {0}", ex.Message));

                requestId = string.Empty;
            }

            return requestId;
        }

        public String DeleteDeployment(String subscriptionId, List<Byte> certificateBytes, String serviceName,
                                       String deploymentSlot)
        {
            String requestId = string.Empty;

            try
            {
                String uri = String.Format(createDeploymentFormat, subscriptionId, serviceName, deploymentSlot);

                ServiceManagementOperation operation = new ServiceManagementOperation(certificateBytes);

                requestId = operation.Invoke(uri, true);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in DeleteDeployment()  Error: {0}", ex.Message));

                requestId = string.Empty;
            }

            return requestId;
        }

        public String GetOperationStatus(String subscriptionId, List<Byte> certificateBytes, String requestId, out String status)
        {
            status = string.Empty;

            XDocument operationStatus = null;
        
            try
            {
                String uri = String.Format(getOperationStatusFormat, subscriptionId, requestId);

                ServiceManagementOperation operation = new ServiceManagementOperation(certificateBytes);

                operationStatus = operation.Invoke(uri);

                status = operationStatus.Element("Operation").Element("Status").Value;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in GetOperationStatus()  Error: {0}", ex.Message));

                status = string.Empty;
            }

            return operationStatus.ToString();
        }
        #endregion Public Methods
    }
}
