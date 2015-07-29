using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Bermuda.Core;
using System.Diagnostics;


namespace Bermuda.Amazon.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());

            Console.WriteLine("Loading configuration information...");
            var config = new AmazonHostEnvironmentConfiguration();
            Console.WriteLine();
            Console.WriteLine(config.ToString());
            Console.WriteLine();

            HostEnvironment.Instance.Initialize(config);

            Console.WriteLine();
            Console.WriteLine("Press enter at any time to exit...");
            Console.WriteLine();
            Console.ReadLine();
        }  
    }
}
