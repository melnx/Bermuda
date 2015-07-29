using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.AdminLibrary.Interfaces;
using Bermuda.AdminLibrary.Models;

namespace Bermuda.AdminLibrary.Amazon
{
    public class S3Manager : IBermudaAdmin
    {
        public string CreateInstance()
        {
            throw new NotImplementedException();
        }

        public bool DeleteInstance(string deploymentId)
        {
            throw new NotImplementedException();
        }

        public bool StopInstance(string deploymentId)
        {
            throw new NotImplementedException();
        }

        public bool StartInstance(string deploymentId)
        {
            throw new NotImplementedException();
        }

        public bool UpdateConfig(string deploymentId, string configuration)
        {
            throw new NotImplementedException();
        }

        public List<CloudInstance> ListInstances()
        {
            throw new NotImplementedException();
        }
    }
}
