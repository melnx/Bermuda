using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.AdminLibrary.Models
{
    public class AzurePackage
    {
        public string PackageName { get; set; }
        public string PackageFile { get; set; }
        public string ConfigFile { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
