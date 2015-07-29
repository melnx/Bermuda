using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Bermuda.NetSaturator
{
    public abstract class NetProcessor : IDataProcessor, INetComConsumer
    {

        #region IDataProcessor

        private IComputeNode computeNode;
        public IComputeNode ComputeNode
        {
            get
            {
                return computeNode;
            }
            set
            {
                computeNode = value;
            }
        }

        public bool StartProcessor()
        {
            SaturationThread = new Thread(new ThreadStart(Saturate));
            SaturationThread.Start();
            return true;
        }

        public bool StopProcessor()
        {
            eventStop.Set();
            SaturationThread.Join();
            return true;
        }

        #endregion

        #region INetComConsumer

        public void ConsumeData(string data)
        {
            string[] split = data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            //print out data
            split.ToList().ForEach(s => Trace.WriteLine(s));

            //add to messages to process
            split.ToList().ForEach(s => UdpMessageToDecode.Enqueue(s));
        }

        #endregion

        #region Variables and Properties

        /// <summary>
        /// starting and stopping event for saturator
        /// </summary>
        protected ManualResetEvent eventStop = new ManualResetEvent(false);

        /// <summary>
        /// the main saturation thread
        /// </summary>
        protected Thread SaturationThread { get; set; }

        /// <summary>
        /// the mapping from instance count to address
        /// </summary>
        protected IDictionary<long, string> AddressMap { get; set; }

        /// <summary>
        /// the udp socket connection for incoming data
        /// </summary>
        protected INetCom Client { get; set; }

        /// <summary>
        /// the catalog for this processor
        /// </summary>
        protected string CatalogName { get; set; }
        protected ICatalog Catalog { get { return computeNode.Catalogs[CatalogName] as ICatalog; } }

        /// <summary>
        /// the table for this processor
        /// </summary>
        protected string TableName { get; set; }
        protected ITableMetadata Table { get { return Catalog.CatalogMetadata.Tables[TableName]; } }

        /// <summary>
        /// The udp messages to process
        /// </summary>
        protected ConcurrentQueue<string> UdpMessageToDecode { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor with compute node
        /// </summary>
        /// <param name="computeNode"></param>
        public NetProcessor(
            IComputeNode computeNode, 
            IDictionary<long, string> address_mappings,
            string catalog_name,
            string table_name,
            INetCom com)
        {
            Client = com;
            ComputeNode = computeNode;
            UdpMessageToDecode = new ConcurrentQueue<string>();
            AddressMap = address_mappings;
            CatalogName = catalog_name;
            TableName = table_name;
        }

        #endregion

        #region Saturation thread routine

        /// <summary>
        /// saturation main routine with udp endpoint
        /// </summary>
        private void Saturate()
        {
            //main loop
            while (true)
            {
                try
                {
                    if(!Client.Consuming)
                        Client.StartConsuming(this);
                    
                    //check for packets to process
                    while (UdpMessageToDecode.Count != 0)
                    {
                        //process the message
                        string packet;
                        if (UdpMessageToDecode.TryDequeue(out packet))
                        {
                            ProcessMessage(packet);
                        }
                        //check stop event
                        if (eventStop.WaitOne(0))
                            return;
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
                //check stop event
                if (eventStop.WaitOne(100))
                    return;
            }
        }

        #endregion

        #region Methods

        
        /// <summary>
        /// Process a message and put it into the correct bucket
        /// </summary>
        /// <param name="packet"></param>
        public bool ProcessMessage(string packet)
        {
            try
            {
                //create our type
                IDataItem item = Activator.CreateInstance(Table.DataType) as IDataItem;

                //get the fields of packet
                string[] fields = packet.Split(Table.ColumnDelimiters, StringSplitOptions.None);

                //parse the fields
                for (int x = 0; x < fields.Length; x++)
                {
                    //get the column
                    var column = Table.ColumnsMetadata.Values.FirstOrDefault(c => c.OrdinalPosition == x);

                    //ILineProcessor is here
                    var value = Convert.ChangeType(fields[x], column.ColumnType);

                    //put in our object
                    item.GetType().GetField(column.FieldMapping).SetValue(item, value);
                }
                //action to take for message
                ProcessMessageAction(packet, item);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// abstract implementation of action to take on process message
        /// </summary>
        /// <param name="packet"></param>
        protected abstract bool ProcessMessageAction(string packet, IDataItem item);
        
        #endregion
        
    }
}
