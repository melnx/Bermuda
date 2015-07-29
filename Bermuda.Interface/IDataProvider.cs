using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bermuda.Interface
{
    public interface IDataProvider
    {
        object GetData(string collection);
        long GetCount(string collection);
        Type GetDataType(string collection);
        string Name { get; set; }
        string Id { get; set; }
    }
}