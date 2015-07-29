using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.BermudaConfig.Storage
{
    public interface ICloudStorageBrowser
    {
        string BrowserTitle { get; }
        bool GetBuckets(out IEnumerable<string> Buckets);
        bool GetFiles(string bucket, out IEnumerable<string> Files);
        bool DeleteFile(string PathName, string FileName);
        bool NewDirectory(string PathName);
        bool DeleteDirectory(string PathName);
    }
}
