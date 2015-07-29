using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Bermuda.Interface
{
    public interface INetCom
    {
        bool Consuming { get; set; }
        INetComConsumer Consumer { get; set; }
        bool StartConsuming(int port, INetComConsumer consumer);
        bool StartConsuming(INetComConsumer consumer);
        bool StopConsuming();
        bool Send(string data, string address, int port);
        bool Send(string data, string address);
    }
}
