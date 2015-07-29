using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Bermuda.Interface
{
    public interface IHostEnvironment
    {
        void Initialize(IHostEnvironmentConfiguration endpointProvider);
        void Shutdown();
        IHostEnvironmentConfiguration HostEnvironmentConfiguration { get; }
        IPeerInfo CurrentEndpoint { get; }
        string CurrentInstanceId { get; }
        IEnumerable<IPeerInfo> GetAvailablePeerConnections();
        IEnumerable<IPeerInfo> GetAvailablePeerConnections(int count);
        IEnumerable<IDataProvider> GetBucketInterfacesForDomain(string domain, IEnumerable<string> blobNames = null);
    }
}
