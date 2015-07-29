using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Bermuda.AdminService.Models;
using Bermuda.AdminLibrary.Azure;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System.Net;
using System.IO;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class OperationStatusController : ApiController
    {
        // GET /api/operationstatus
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST /api/operationstatus
        public OperationStatus Post(OperationStatus opsStatus)
        {
            string status = string.Empty;
            string responseString = string.Empty;

            try
            {
                DeploymentHelper deploymentHelper = new DeploymentHelper();

                responseString = deploymentHelper.GetOperationStatus(opsStatus.SubscriptionId, 
                                                                     opsStatus.CertificateBytes, 
                                                                     opsStatus.RequestId, out status);

                opsStatus.Status = status;
                opsStatus.ResponseString = responseString;
            }
            catch (WebException wex)
            {
                string responseStr = string.Empty;

                using (StreamReader responseReader = new StreamReader(wex.Response.GetResponseStream()))
                {
                    responseStr = responseReader.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(responseString) != true)
                    {
                        Logger.Write("OperationStatusController.Post() WebException Response: " + responseStr);
                    }

                    responseReader.Close();
                }

                Logger.Write(string.Format("Error in OperationStatusController.Post()  Error: {0}\n{1}", wex.Message,
                                            wex.InnerException != null ? wex.InnerException.Message : string.Empty));

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in OperationStatusController.Post()  Error: {0}\n{1}", ex.Message,
                                            ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                opsStatus = null;

                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return opsStatus;
        }

        // PUT /api/operationstatus/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/operationstatus/5
        public void Delete(int id)
        {
        }
    }
}
