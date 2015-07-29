using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Bermuda.NetSaturator
{
    public class NetComUDP : INetCom
    {
        #region INetCom

        /// <summary>
        /// we are currently consuming
        /// </summary>
        public bool Consuming { get; set; }

        /// <summary>
        /// the consumer to notify data wsa received
        /// </summary>
        private INetComConsumer _Consumer;
        public INetComConsumer Consumer
        {
            get
            {
                return _Consumer;
            }
            set
            {
                _Consumer = value;
            }
        }

        /// <summary>
        /// start conuming/recieving data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public bool StartConsuming(int port, INetComConsumer consumer)
        {
            try
            {
                Consumer = consumer;
                Client = new UdpClient(port);
                Client.DontFragment = true;
                Client.BeginReceive(new AsyncCallback(ReceivePacket), null);
                Consuming = true;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// start consuming/receiving data with internal port
        /// </summary>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public bool StartConsuming(INetComConsumer consumer)
        {
            return StartConsuming(PortIn, consumer);
        }

        /// <summary>
        /// stop consuming/receiving data
        /// </summary>
        /// <returns></returns>
        public bool StopConsuming()
        {
            try
            {
                Client.Close();
                Client = null;
                Consumer = null;
                Consuming = false;
                return true;
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// sned data to specified address and port
        /// </summary>
        /// <param name="data"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Send(string data, string address, int port)
        {
            try
            {
                byte[] bytes_packet = Encoding.ASCII.GetBytes(data);
                Client.Send(bytes_packet, bytes_packet.Length, address, port);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// send data to internal port and specified address
        /// </summary>
        /// <param name="data"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool Send(string data, string address)
        {
            return Send(data, address, PortOut);
        }

        #endregion

        #region Variables and Properties

        /// <summary>
        /// the udp socket connection for incoming data
        /// </summary>
        private UdpClient Client { get; set; }

        /// <summary>
        /// the in coming port
        /// </summary>
        private int PortIn { get; set; }

        /// <summary>
        /// the out going port
        /// </summary>
        private int PortOut { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with the in and out ports for udp communications
        /// </summary>
        /// <param name="port_in"></param>
        /// <param name="port_out"></param>
        public NetComUDP(int port_in, int port_out)
        {
            PortIn = port_in;
            PortOut = port_out;
        }

        /// <summary>
        /// constructor with the in port for udp communications
        /// </summary>
        /// <param name="port_in"></param>
        /// <param name="port_out"></param>
        public NetComUDP(int port_in)
        {
            PortIn = port_in;
            PortOut = 0;
        }

        #endregion

        #region Methods

        /// <summary>
        /// receive a datagram packet
        /// </summary>
        /// <param name="result"></param>
        private void ReceivePacket(IAsyncResult result)
        {
            try
            {
                //get the data
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = Client.EndReceive(result, ref remote);

                //split on new line
                string string_data = Encoding.ASCII.GetString(data);
               
                //callback
                if(Consumer != null)
                    Consumer.ConsumeData(string_data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            finally
            {
                //new receive
                Client.BeginReceive(new AsyncCallback(ReceivePacket), null);
            }
        }

        #endregion
    }
}
