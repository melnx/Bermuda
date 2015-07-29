using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Bermuda.AdminLibrary.Azure;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Bermuda.AdminService.Models;
using System.Net;
using System.IO;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class DeploymentController : ApiController
    {
        // GET /api/deployment
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/deployment/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/deployment/subscriptionId/serviceName/deploymentName/deploymentSlot/label/instanceCount
        // CreateDeployment
        public string Post(AzureDeployment azureDeployment)
        {
            string requestId = string.Empty;

            try
            {
                Logger.Write("In the DeploymentController.Post method...");

                DeploymentHelper deploymentHelper = new DeploymentHelper();

                Logger.Write(string.Format("SubscriptionId: {0}\tService Name: {1}\tCertificate Byte Length: {2}\tDeployment Name: {3}",
                                            azureDeployment.SubscriptionId, azureDeployment.ServiceName, 
                                            azureDeployment.CertificateBytes.ToArray().Length,  azureDeployment.DeploymentName));

                requestId = deploymentHelper.CreateDeployment(azureDeployment.SubscriptionId, 
                                                              azureDeployment.CertificateBytes, 
                                                              azureDeployment.ServiceName,
                                                              azureDeployment.DeploymentName,
                                                              azureDeployment.DeploymentSlot,
                                                              azureDeployment.Label,
                                                              azureDeployment.InstanceCount);

                Logger.Write("Returned RequestId: " + requestId);
            }
            catch (WebException wex)
            {
                string responseString = string.Empty;

                using (StreamReader responseReader = new StreamReader(wex.Response.GetResponseStream()))
                {
                    responseString = responseReader.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(responseString) != true)
                    {
                        Logger.Write("DeploymentController.Post() WebException Response: " + responseString);
                    }

                    responseReader.Close();
                }

                Logger.Write(string.Format("Error in DeploymentController.Post()  Error: {0}\n{1}", wex.Message,
                                            wex.InnerException != null ? wex.InnerException.Message : string.Empty));

                requestId = string.Empty;

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
            	Logger.Write(string.Format("Error in DeploymentController.Post()  Error: {0}\n{1}", ex.Message, 
                                            ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                requestId = string.Empty;

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return requestId;
        }

        // PUT /api/deployment/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/deployment
        public string Delete(AzureDeployment azureDeployment)
        {
            string requestId = string.Empty;

            try
            {
                DeploymentHelper deploymentHelper = new DeploymentHelper();

                requestId = deploymentHelper.DeleteDeployment(azureDeployment.SubscriptionId, 
                                                              azureDeployment.CertificateBytes, 
                                                              azureDeployment.ServiceName, 
                                                              azureDeployment.DeploymentSlot);
            }
            catch (WebException wex)
            {
                string responseString = string.Empty;

                using (StreamReader responseReader = new StreamReader(wex.Response.GetResponseStream()))
                {
                    responseString = responseReader.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(responseString) != true)
                    {
                        Logger.Write("DeploymentController.Delete() WebException Response: " + responseString);
                    }

                    responseReader.Close();
                }

                Logger.Write(string.Format("Error in DeploymentController.Delete()  Error: {0}\n{1}", wex.Message,
                                            wex.InnerException != null ? wex.InnerException.Message : string.Empty));

                requestId = string.Empty;

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in DeploymentController.Post()  Error: {0}\n{1}", ex.Message,
                                            ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                requestId = string.Empty;

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return requestId;
        }
    }
}
