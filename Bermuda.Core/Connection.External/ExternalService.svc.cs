using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Diagnostics;
using Bermuda.Core.MapReduce;
using Bermuda.Interface.Connection.External;
using Bermuda.Interface;
using Bermuda.Catalog;

namespace Bermuda.Core.Connection.External
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ExternalService" in code, svc and config file together.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, AddressFilterMode = AddressFilterMode.Any, InstanceContextMode = InstanceContextMode.Single)]
    //[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ExternalService : IExternalService
    {
        public string Ping(string param)
        {
            switch(param)
            {
                case "status":
                    return BermudaMapReduce.Instance.GetStatus(false);
            }
            return "0";
        }

        public BermudaResult GetDataFromCursor(string cursor, string paging, string command)
        {
            return GetData(null, null, null, null, null, command, cursor, paging);
        }

        public BermudaCursor GetCursor(string domain, string query, string mapreduce, string merge, string paging, string command)
        {
            var data = GetData(domain, query, mapreduce, merge, paging, command, null, null);

            return new BermudaCursor { CursorId = data.CacheKey, Error = data.Error };
        }

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, string command)
        {
            return GetData(domain, query, mapreduce, merge, paging, command, null, null);
        }

        public BermudaResult GetData(string domain, string query, string mapreduce, string merge, string paging, string command, string cursor, string paging2)
        {
            //return new BermudaDatapointResult { Datapoints = new List<Entities.Datapoint>(), Metadata = new Entities.BermudaNodeStatistic() };
            Stopwatch sw = new Stopwatch();
            BermudaResult result = null;
            BermudaNodeStatistic metadata = null;
            sw.Start();
            try
            {
                result = BermudaMapReduce.Instance.GetData(domain, query, mapreduce, merge, paging, 1, command, cursor, paging2);
                metadata = result.Metadata;
            }
            catch (BermudaException ex)
            {
                result = new BermudaResult { Error = ex.Message }; 
            }
            catch (Exception ex)
            {
                result = new BermudaResult { Error = ex.ToString() }; 
            }

            sw.Stop();
            if (metadata == null) metadata = new BermudaNodeStatistic();
            metadata.LinqExecutionTime = sw.Elapsed;
            result.Metadata = metadata;

            return result;
        }

        /// <summary>
        /// return the catalogs collection names
        /// </summary>
        /// <returns></returns>
        public string[] GetMetadataCatalogs()
        {
            //compute node is not initialized
            if (ComputeNode.Node == null)
                return new List<string>().ToArray();

            return ComputeNode.Node.Catalogs.Select(c => c.Key).ToArray();
        }

        /// <summary>
        /// return the table definitions
        /// </summary>
        /// <returns></returns>
        public TableMetadataResult[] GetMetadataTables()
        {
            //compute node is not initialized
            if (ComputeNode.Node == null)
                return new List<TableMetadataResult>().ToArray();

            List<TableMetadataResult> results = new List<TableMetadataResult>();
            ComputeNode.Node.Catalogs.Values.ToList().ForEach(c =>
                {
                    Catalog.Catalog catalog = c as Catalog.Catalog;
                    catalog.CatalogMetadata.Tables.Values.ToList().ForEach(t =>
                        {
                            results.Add(new TableMetadataResult()
                            {
                                Catalog = catalog.Name,
                                Table = t.TableName
                            });
                        });
                });

            return results.ToArray();
        }

        public ColumnMetadataResult[] GetMetadataColumns()
        {
            //compute node is not initialized
            if (ComputeNode.Node == null)
                return new List<ColumnMetadataResult>().ToArray();

            List<ColumnMetadataResult> results = new List<ColumnMetadataResult>();
            ComputeNode.Node.Catalogs.Values.ToList().ForEach(c =>
            {
                Catalog.Catalog catalog = c as Catalog.Catalog;
                catalog.CatalogMetadata.Tables.Values.ToList().ForEach(t =>
                {
                    int OrdinalPosition = 1;
                    t.ColumnsMetadata.Values.ToList().ForEach(col =>
                    {
                        results.Add(new ColumnMetadataResult()
                        {
                            Catalog = c.Name,
                            Table = t.TableName,
                            Column = col.FieldMapping,
                            DataType = col.ColumnType.ToString(),
                            ColumnLength = col.ColumnLength,
                            Nullable = col.Nullable,
                            Visible = col.Visible,
                            OrdinalPosition = OrdinalPosition++
                        });
                    });
                });
            });

            return results.ToArray();
        }


        //public void InsertMentions(string domain, byte[] mentions)
        //{
        //    var set = ThriftMarshaller.Deserialize<ThriftMentionChunk>(mentions);
        //    AzureMapReducer.Instance.InsertMentions(domain, set.Mentions, 1);
        //}

    }
}
