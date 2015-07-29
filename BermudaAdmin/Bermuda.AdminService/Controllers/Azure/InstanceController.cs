using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class InstanceController : ApiController
    {
        // GET /api/instance
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/instance/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/instance
        public void Post(string value)
        {
        }

        // PUT /api/instance/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/instance/5
        public void Delete(int id)
        {
        }
    }
}
