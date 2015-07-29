using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.AdminLibrary.Interfaces;

namespace Bermuda.AdminLibrary.Models
{
    public class CloudInstance : ICloudEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
