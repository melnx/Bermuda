using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Bermuda.AdminLibrary.Azure;
using Bermuda.AdminLibrary.Models;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class HostedServiceController : ApiController
    {
        // GET /api/hostedservice
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/hostedservice/subscriptionId/certificateBytes/serviceName
        public HostedService Get(string subscriptionId, List<Byte> certificateBytes, string serviceName)
        {
            HostedService hostedService = null;

            try
            {
                HostedServiceHelper hostedServiceHelper = new HostedServiceHelper();

                hostedService = hostedServiceHelper.GetHostedService(subscriptionId, certificateBytes, serviceName);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error occurred in HostedServiceController.Get(string subscriptionId, Byte[] certificateBytes, string serviceName)  Error: {0}", ex.Message));

                hostedService = null;
            }

            return hostedService;
        }

        // POST /api/hostedservice
        public void Post(string value)
        {
        }

        // PUT /api/hostedservice/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/hostedservice/5
        public void Delete(int id)
        {
        }
    }
}
