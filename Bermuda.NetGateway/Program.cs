using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using Bermuda.Local.Launcher;
using Bermuda.Interface;
using Bermuda.Azure.WebRole;

namespace Bermuda.NetGateway
{
    class Program
    {
        const int PortIn = 1234;

        const int PortOut = 12345;

        const string Address = "127.0.0.1";

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());
            Console.WriteLine("Started...");


            var config = new LocalHostEnvironmentConfiguration();
            //var config = new AzureHostEnvironmentConfiguration();
            config.NodeType = ComputeNodeType.NetGateway;
            var compute_node = config.GetComputeNode();
            var processors = config.GetProcessors(compute_node);
            processors.ToList().ForEach(p => p.StartProcessor());

            Console.WriteLine("Open...");

            Console.WriteLine("Press Any Key To Exit...");
            Console.ReadLine();
        }
    }
}
