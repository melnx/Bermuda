using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace Bermuda.NetSaturatorClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _Started = false;
        public bool Started
        {
            get
            {
                return _Started;
            }
            set
            {
                _Started = value;
                NotifyPropertyChanged("Started");
                NotifyPropertyChanged("Stopped");
            }
        }

        public bool Stopped
        {
            get
            {
                return !_Started;
            }
        }

        private Int64 _PacketsSent;
        public Int64 PacketsSent
        {
            get { return _PacketsSent; }
            set 
            { 
                _PacketsSent = value;
                NotifyPropertyChanged("PacketsSent");
                NotifyPropertyChanged("PacketsSentLabel");
            }
        }

        public string PacketsSentLabel
        {
            get
            {
                return string.Format("Packets Sent: {0}", _PacketsSent);
            }
        }

        private int _PacketsPerSecond;

        public int PacketsPerSecond
        {
            get { return _PacketsPerSecond; }
            set { _PacketsPerSecond = value; }
        }

        UdpClient Client { get; set; }

        int Port = 1234;

        string Address = "127.0.0.1";

        #region INotifyPropertyChanged

        /// <summary>
        /// handling of property change for this window
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        Timer timer;
        ManualResetEvent eventStop = new ManualResetEvent(false);

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            Started = true;
            eventStop.Reset();
            Client = new UdpClient(Address, Port);
            if(PacketsPerSecond > 0)
                timer = new Timer(SendPacket, eventStop, 0, 1000 / PacketsPerSecond);
        }

        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            Started = false;
            eventStop.Set();
            //Thread.Sleep(1000);
            timer.Dispose();
            Client.Close();
            Client = null;
        }

        public void SendPacket(Object stateInfo)
        {
            ManualResetEvent stop = (ManualResetEvent)stateInfo;
            if (stop.WaitOne(0))
                return;

            DateTime now = DateTime.Now;
            string string_packet = string.Format("{0},{1} {2},{3}\r\n", PacketsSent, now.ToShortDateString(), now.ToShortTimeString(), "This is a test");
            byte[] packet = Encoding.ASCII.GetBytes(string_packet);
            try
            {
                Client.Send(packet, packet.Length);
                PacketsSent++;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

    }
}
