using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Interface.Connection.External
{
    [ServiceContract]
    public interface IExternalService
    {
        [OperationContract]
        string Ping(string param);

        [OperationContract]
        BermudaCursor GetCursor(string domain, string query, string mapreduce, string merge, string paging, string command);

        [OperationContract]
        BermudaResult GetDataFromCursor(string cursor, string paging, string command);

        [OperationContract]
        BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, string command);

        [OperationContract]
        string[] GetMetadataCatalogs();

        [OperationContract]
        TableMetadataResult[] GetMetadataTables();

        [OperationContract]
        ColumnMetadataResult[] GetMetadataColumns();

        //[OperationContract]
        //void InsertMentions(string domain, byte[] mentions);
    }
}
