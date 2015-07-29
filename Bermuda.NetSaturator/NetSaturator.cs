using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;

namespace Bermuda.NetSaturator
{
    public class NetSaturator : NetProcessor
    {

        #region Constructor

        /// <summary>
        /// default constructor with compute node
        /// </summary>
        /// <param name="computeNode"></param>
        public NetSaturator(
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
        /// put this item in the correct tables
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
                
                //send the packet along
                Catalog.Buckets[bucket].BucketDataTables[Table.TableName].AddOrUpdate(item.PrimaryKey, item);
                Catalog.CatalogDataTables[Table.TableName].AddOrUpdate(item.PrimaryKey, item);

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
