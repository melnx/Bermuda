using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Bermuda.AdminService.Test
{
    [DataContract]
    public class OperationStatus
    {
        [DataMember]
        public String SubscriptionId { get; set; }

        [DataMember]
        public List<Byte> CertificateBytes { get; set; }

        [DataMember]
        public String RequestId { get; set; }

        [DataMember]
        public String Status { get; set; }

        [DataMember]
        public String ResponseString { get; set; }
    }
}