using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.AdminLibrary.Models;

namespace Bermuda.AdminLibrary.Interfaces
{
    public interface IBermudaAdmin
    {
        /// <summary>
        /// CreateInstance()
        /// </summary>
        /// <returns>Deployment Id of created instance</returns>
        string CreateInstance();

        /// <summary>
        /// DeleteInstance(string deploymentId) - Deletes a deployed Instance
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        bool DeleteInstance(string deploymentId);

        /// <summary>
        /// StopInstance(string deploymentId)
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        bool StopInstance(string deploymentId);

        /// <summary>
        /// StartInstance(string deploymentId)
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        bool StartInstance(string deploymentId);

        /// <summary>
        /// UpdateConfig(string deploymentId, string configuration)
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        bool UpdateConfig(string deploymentId, string configuration);

        List<CloudInstance> ListInstances();
    }
}