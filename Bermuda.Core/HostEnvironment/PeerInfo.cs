using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Bermuda.Interface;

namespace Bermuda.Core
{
    public class PeerInfo : IPeerInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public string Id { get; set; }

        public bool Equals(IPeerInfo other)
        {
            if (other == null) return false;

            return EndPoint.Equals(other.EndPoint) && Id == other.Id;
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, EndPoint: {1}", Id, EndPoint.ToString());
        }
    }
}
