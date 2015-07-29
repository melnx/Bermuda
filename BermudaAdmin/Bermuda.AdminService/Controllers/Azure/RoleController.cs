using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class RoleController : ApiController
    {
        // GET /api/role
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/role/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/role
        public void Post(string value)
        {
        }

        // PUT /api/role/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/role/5
        public void Delete(int id)
        {
        }
    }
}
