using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Amazon
{
    public class RDSController : ApiController
    {
        // GET /api/rds
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/rds/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/rds
        public void Post(string value)
        {
        }

        // PUT /api/rds/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/rds/5
        public void Delete(int id)
        {
        }
    }
}
