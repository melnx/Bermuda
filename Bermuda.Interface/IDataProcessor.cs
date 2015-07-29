using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Interface
{
    public interface IDataProcessor
    {
        /// <summary>
        /// the compute node interface for saturator
        /// </summary>
        IComputeNode ComputeNode { get; set; }

        /// <summary>
        /// the processor start function
        /// </summary>
        /// <returns></returns>
        bool StartProcessor();

        /// <summary>
        /// the processor stop function
        /// </summary>
        /// <returns></returns>
        bool StopProcessor();
    }
}
