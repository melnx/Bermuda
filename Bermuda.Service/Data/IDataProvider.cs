using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Entities.Thrift;
using Bermuda.Entities;

namespace Bermuda.Service.Data
{
    public interface IDataProvider
    {
        IEnumerable<Mention> GetData();
        string Name { get; set; }
        string Id { get; set; }
    }
}