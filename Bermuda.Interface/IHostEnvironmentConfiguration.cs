using System;
using System.Collections.Generic;
using System.Net;

namespace Bermuda.Interface
{
    public interface IHostEnvironmentConfiguration
    {
        IPeerInfo CurrentEndpoint { get; }
        IPEndPoint InternalServiceEndPoint { get; }
        IPEndPoint ExternalServiceEndPoint { get; }
        string CurrentInstanceId { get; }
        int CurrentInstanceIndex { get; }
        IEnumerable<IPeerInfo> AllNodeEndpoints { get; }
        //ComputeNodeType ComputeNodeType { get; }
        IComputeNode GetComputeNode();
        IEnumerable<IDataProcessor> GetProcessors(IComputeNode compute_node);
        IDictionary<long, string> GetInstanceAddressMap();
    }

    //these compute node types are here ONLY until we implement the one node to rule them all
    public enum ComputeNodeType
    {
        MX,
        WeatherData,
        WikiData,
        Combined,
        Net,
        NetGateway
    }
}
