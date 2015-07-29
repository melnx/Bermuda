using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Linq.Expressions;
using Bermuda.Interface;

namespace Bermuda.Interface.Connection.Internal
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IBermudaService
    {
        [OperationContract]
        string Ping(string param);

        [OperationContract]
        BermudaCursor GetCursor(string domain, string query, string mapreduce, string merge, string paging, string command);

        [OperationContract]
        BermudaResult GetDataFromCursor(string cursor, string paging, string command);

        [OperationContract]
        BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, int remdepth, string command, string cursor, string paging2);

        //[OperationContract]
        //void InsertMentions(string domain, string blob, byte[] mentions, int remdepth);
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    //[DataContract]
    //public class CompositeType
    //{
    //    bool boolValue = true;
    //    string stringValue = "Hello ";

    //    [DataMember]
    //    public bool BoolValue
    //    {
    //        get { return boolValue; }
    //        set { boolValue = value; }
    //    }

    //    [DataMember]
    //    public string StringValue
    //    {
    //        get { return stringValue; }
    //        set { stringValue = value; }
    //    }
    //}
}
