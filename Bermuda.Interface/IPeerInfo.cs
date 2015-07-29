using System;
using System.Net;

namespace Bermuda.Interface
{
    public interface IPeerInfo : IEquatable<IPeerInfo>
    {
        IPEndPoint EndPoint { get; set; }
        bool Equals(object obj);
        string Id { get; set; }
    }
}
