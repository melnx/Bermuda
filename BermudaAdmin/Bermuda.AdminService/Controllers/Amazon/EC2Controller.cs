using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Amazon
{
    public class EC2Controller : ApiController
    {
        // GET /api/ec2
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/ec2/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/ec2
        public void Post(string value)
        {
        }

        // PUT /api/ec2/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/ec2/5
        public void Delete(int id)
        {
        }
    }
}
