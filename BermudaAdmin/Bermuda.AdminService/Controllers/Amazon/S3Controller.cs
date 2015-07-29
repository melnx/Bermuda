using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Amazon
{
    public class S3Controller : ApiController
    {
        // GET /api/s3
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/s3/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/s3
        public void Post(string value)
        {
        }

        // PUT /api/s3/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/s3/5
        public void Delete(int id)
        {
        }
    }
}
