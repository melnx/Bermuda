using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace Bermuda.NetSaturator
{
    public class NetGateway : NetProcessor
    {
        #region Constructor

        /// <summary>
        /// default constructor with compute node
        /// </summary>
        /// <param name="computeNode"></param>
        public NetGateway(
            IComputeNode computeNode, 
            IDictionary<long, string> address_mappings,
            string catalog_name,
            string table_name,
            INetCom com)
            : base(computeNode, address_mappings, catalog_name, table_name, com)
        {
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// send this packet along to the correct bermuda node to be saved
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override bool ProcessMessageAction(string packet, IDataItem item)
        {
            try
            {
                //get the mod field
                long mod = (long)Convert.ChangeType(item.GetType().GetField(Table.ModField).GetValue(item), typeof(long));
                long bucket = mod % ComputeNode.GlobalBucketCount;
                long instance = bucket % AddressMap.Count;
                string address = AddressMap[instance];

                //send the packet along
                Client.Send(packet, address);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        #endregion
    }
}
