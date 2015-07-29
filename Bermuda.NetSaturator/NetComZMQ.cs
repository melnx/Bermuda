using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Diagnostics;
using ZMQ;

namespace Bermuda.NetSaturator
{
    public class NetComZMQ : INetCom
    {
        #region INetCom

        public bool Consuming { get; set; }

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

        public bool StartConsuming(int port, INetComConsumer consumer)
        {
            try
            {
                if (Consuming)
                    return false;
                eventStop.Reset();
                PortSubscribe = port;
                threadMessaging = new Thread(new ThreadStart(ZMQReceiver));
                threadMessaging.Start();
                Consuming = true;
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        public bool StartConsuming(INetComConsumer consumer)
        {
            return StartConsuming(PortSubscribe, consumer);
        }

        public bool StopConsuming()
        {
            try
            {
                if (!Consuming)
                    return false;
                eventStop.Set();
                threadMessaging.Join();
                Consuming = false;
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        public bool Send(string data, string address, int port)
        {
            throw new NotImplementedException();
        }

        public bool Send(string data, string address)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Variables and Properties

        /// <summary>
        /// event to stop messaging
        /// </summary>
        private ManualResetEvent eventStop = new ManualResetEvent(false);

        /// <summary>
        /// the thread for receiving messages
        /// </summary>
        private Thread threadMessaging { get; set; }

        /// <summary>
        /// the port for subscribing
        /// </summary>
        private int PortSubscribe { get; set; }

        /// <summary>
        /// the address to listen on
        /// </summary>
        private string AddressSubscribe { get; set; }

        #endregion

        #region Methods

        private void ZMQReceiver()
        {
            try
            {
                //open the tcp connection
                using (var context = new Context(1))
                {
                    using (Socket subscriber = context.Socket(SocketType.SUB))
                    {
                        subscriber.Subscribe("", Encoding.ASCII);

                        while (true)
                        {
                            try
                            {
                                subscriber.Connect(string.Format("tcp://{0}:{1}", AddressSubscribe, PortSubscribe));
                                break;
                            }
                            catch (System.Exception ex)
                            {
                                Trace.WriteLine(ex.ToString());
                            }
                            if (eventStop.WaitOne(5000))
                                return;
                        }

                        while (true)
                        {
                            bool more = true;
                            string packet = "";
                            while (more)
                            {
                                packet += subscriber.Recv(Encoding.ASCII, 1000);
                                more = (bool)subscriber.GetSockOpt(SocketOpt.RCVMORE);

                                if (eventStop.WaitOne(0))
                                    return;
                            }
                            Consumer.ConsumeData(packet);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        #endregion

    }
}
