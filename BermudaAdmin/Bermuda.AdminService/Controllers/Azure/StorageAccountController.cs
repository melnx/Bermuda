using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Bermuda.AdminService.Controllers.Azure
{
    public class StorageAccountController : ApiController
    {
        // GET /api/storageaccount
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /api/storageaccount/5
        public string Get(int id)
        {
            return "value";
        }

        // POST /api/storageaccount
        public void Post(string value)
        {
        }

        // PUT /api/storageaccount/5
        public void Put(int id, string value)
        {
        }

        // DELETE /api/storageaccount/5
        public void Delete(int id)
        {
        }
    }
}
