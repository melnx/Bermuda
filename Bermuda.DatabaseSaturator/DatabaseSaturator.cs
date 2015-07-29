using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using System.Data.SqlClient;
using System.Reflection;
using System.Data;
using System.Collections.Concurrent;
using Bermuda.Util;
using Bermuda.Catalog;
using System.Diagnostics;
using System.IO;

namespace Bermuda.DatabaseSaturator
{
    public class DatabaseSaturator : IDataProcessor
    {
        #region IDataProcessor Implementation

        private IComputeNode computeNode;
        public IComputeNode ComputeNode
        {
            get
            {
                return computeNode;
            }
            set
            {
                computeNode = value;
            }
        }

        public bool StartProcessor()
        {
            return Start();
        }

        public bool StopProcessor()
        {
            return Stop();
        }

        #endregion

        #region Variables and Properties

        /// <summary>
        /// starting and stopping event for saturator
        /// </summary>
        private ManualResetEvent eventStop = new ManualResetEvent(false);

        /// <summary>
        /// the number of thread to use for saturation
        /// </summary>
        public int SaturationThreadCount { get; set; }

        /// <summary>
        /// the main saturation thread
        /// </summary>
        private Thread SaturationThread { get; set; }

        /// <summary>
        /// the last time we refreshed the catalog
        /// </summary>
        private DateTime LastCatalogRefresh { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with catalog
        /// </summary>
        public DatabaseSaturator(IComputeNode computeNode)
        {
            ComputeNode = computeNode;
#if DEBUG
            //SaturationThreadCount = Environment.ProcessorCount;
            SaturationThreadCount = Math.Min(Environment.ProcessorCount * 4, 64);
            //SaturationThreadCount = 1;
            //SaturationThreadCount = 64;
#else
            SaturationThreadCount = Math.Min(Environment.ProcessorCount * 4, 64);
            //SaturationThreadCount = Environment.ProcessorCount;
#endif
            LastCatalogRefresh = DateTime.UtcNow;
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Datasaturator start
        /// </summary>
        /// <returns></returns>
        private bool Start()
        {
            try
            {
                eventStop.Reset();
                SaturationThread = new Thread(new ThreadStart(Saturate));
                SaturationThread.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Datasaturator stop
        /// </summary>
        /// <returns></returns>
        private bool Stop()
        {
            //check if we are running
            if (SaturationThread == null || SaturationThread.Join(0))
                return true;

            //set the event
            eventStop.Set();

            //wait on main thread to join
            SaturationThread.Join();
            
            return true;
        }

        #endregion

        #region Main Saturation Thread Routine

        /// <summary>
        /// feed the thread pool for saturation
        /// </summary>
        private void Saturate()
        {
            //the currently executing threads and thier events
            Thread[] threads = new Thread[SaturationThreadCount];
            ManualResetEvent[] events = new ManualResetEvent[SaturationThreadCount];
            for(int x=0;x<SaturationThreadCount;x++)
            {
                events[x] = new ManualResetEvent(true);
            }
            //saturator is paused
            bool Paused = false;

            //main loop
            while (true)
            {
                try
                {
                    //check the memory usage
                    var memStatus = SystemInfo.GetMemoryStatusEx();
                    if (ComputeNode.Purging)
                    {
                        //mark as paused
                        Paused = true;

                        //report memory usage
                        System.Diagnostics.Trace.WriteLine(string.Format("Saturator has reach maximum memory threshold - TotalMemory:{0} MB, AvailableMemory:{1} MB, AvailablePercent:{2} %",
                            memStatus.ullTotalPhys / 1024 / 1024, memStatus.ullAvailPhys / 1024 / 1024, (double)memStatus.ullAvailPhys / (double)memStatus.ullTotalPhys * 100.0));

                        //sleep for 5 minutes
                        if (eventStop.WaitOne(1 * 60 * 1000))
                            return;
                    }
                    else
                    {
                        //report is we are unpausing
                        if (Paused)
                        {
                            //report memory usage
                            System.Diagnostics.Trace.WriteLine(string.Format("Saturator is starting after pause - TotalMemory:{0} MB, AvailableMemory:{1} MB, AvailablePercent:{2} %",
                            memStatus.ullTotalPhys / 1024 / 1024, memStatus.ullAvailPhys / 1024 / 1024, (double)memStatus.ullAvailPhys / (double)memStatus.ullTotalPhys * 100.0));
                        }
                        //mark as not paused
                        Paused = false;

                        //check to refresh catalogs
                        //if ((DateTime.UtcNow - LastCatalogRefresh).TotalMinutes > 5)
                        //{
                        //    computeNode.RefreshCatalogs();

                        //    LastCatalogRefresh = DateTime.UtcNow;

                        //    PrintReport();
                        //}
                        //System.Diagnostics.Trace.WriteLine("Take Snapshot");
                        //Thread.Sleep(1 * 60 * 1000);

                        //wait at least on signaled event
                        int index = WaitHandle.WaitAny(events, 5000);

                        //check the return
                        if (index != WaitHandle.WaitTimeout)
                        {
                            //get the next bucket data table to service
                            IReferenceDataTable table = computeNode.SaturationTables.Where(t => t.Saturating == false).OrderBy(t => t.NextSaturation).FirstOrDefault();

                            //make sure we got one
                            if (table != null && DateTime.UtcNow > table.NextSaturation)
                            {
                                //create the thread
                                table.Saturating = true;
                                events[index].Reset();
                                threads[index] = new Thread(new ParameterizedThreadStart(SaturateDataTable));
                                threads[index].Start(new SaturateDataTableData()
                                {
                                    eventDone = events[index],
                                    table = table
                                });
                                //events[index].WaitOne();

                                ////for testing remove all nodes
                                ///////////////////////////////////
                                //foreach (var item in table.DataItems)
                                //{
                                //    var args = new object[] { };
                                //    object o = item.GetType().GetProperty("Value").GetValue(item, args);
                                //    table.DeleteItem((IDataItem)o);
                                //}
                                //threads[index] = null;
                                
                                ///////////////////////////////////
                                ////check profiler here
                                //GC.Collect(GC.MaxGeneration);
                                //GC.WaitForPendingFinalizers();
                                ////foreach (IDataTable table in table.Catalog.ComputeNode.SaturationTables)
                                ////    System.Diagnostics.Trace.WriteLine(string.Format("The status - Name:{0}, Count:{1}", table.TableMetadata.TableName, table.DataItems.Count));
                                //System.Diagnostics.Trace.WriteLine("Done");


                                


                                //Thread.Sleep(1000000000);
                                //string test = "";
                            }
                            else
                            {
                                //sleep for little while
                                if (eventStop.WaitOne(5000))
                                    return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                }
                if (eventStop.WaitOne(0))
                    return;
            }
        }

        /// <summary>
        /// report collection sizes to trace and to file
        /// </summary>
        public void PrintReport()
        {
            DateTime now = DateTime.Now;
            DateTime midnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "ItemStatus_*");
            foreach (string s in files)
            {
                string[] path_parts = s.Split(new string[] {"\\"}, StringSplitOptions.None);
                if (path_parts.Count() > 0)
                {
                    string name = path_parts.Last();
                    if (name.IndexOf('.') != -1)
                        name = name.Substring(0, name.Length - name.IndexOf('.') - 1);
                    string[] name_parts = name.Split('_');
                    if (name_parts.Count() == 4)
                    {
                        int year = 0;
                        int month = 0;
                        int day = 0;
                        if (int.TryParse(name_parts[1], out year))
                        {
                            if (int.TryParse(name_parts[2], out month))
                            {
                                if (int.TryParse(name_parts[3], out day))
                                {
                                    DateTime fileTime = new DateTime(year, month, day, 0, 0, 0);
                                    if ((midnight - fileTime).TotalDays > 5)
                                    {
                                        try
                                        {
                                            File.Delete(s);
                                        }
                                        catch (Exception) { }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            string FileName = string.Format("ItemStatus_{0}_{1}_{2}", now.Year, now.Month, now.Day);

            using (StreamWriter file = new StreamWriter(FileName, true))
            {
                foreach (ICatalog catalog in ComputeNode.Catalogs.Values.ToList())
                {
                    foreach (var catalog_table in catalog.CatalogDataTables.Values.ToList())
                    {
                        string log = string.Format(
                            "Time: {3} {4}, Catalog:{0}, CatalogTableName:{1}, Count{2}",
                            catalog.CatalogName,
                            catalog_table.TableMetadata.TableName,
                            catalog_table.DataItems.Count,
                            now.ToShortDateString(),
                            now.ToShortTimeString());
                        System.Diagnostics.Trace.WriteLine(log);
                        file.WriteLine(log);
                    }
                    foreach (var bucket in catalog.Buckets.Values.ToList())
                    {
                        foreach (var bucket_table in bucket.BucketDataTables.Values.ToList())
                        {
                            if (bucket_table is RelationshipDataTable)
                            {
                                string log = string.Format(
                                    "Time: {5} {6}, Catalog:{0}, ReferenceTableName:{1}, Mode:{2}, ItemCount:{3}, LookupCount:{4}",
                                    catalog.CatalogName,
                                    bucket_table.TableMetadata.TableName,
                                    bucket.BucketMod,
                                    bucket_table.DataItems.Count,
                                    ((RelationshipDataTable)bucket_table).RelationshipParentLookup.Count,
                                    now.ToShortDateString(),
                                    now.ToShortTimeString());
                                System.Diagnostics.Trace.WriteLine(log);
                                file.WriteLine(log);
                            }
                            else
                            {
                                string log = string.Format(
                                    "Time: {4} {5}, Catalog:{0}, BucketTableName:{1}, Mode:{2}, ItemCount:{3}",
                                    catalog.CatalogName,
                                    bucket_table.TableMetadata.TableName,
                                    bucket.BucketMod,
                                    bucket_table.DataItems.Count,
                                    now.ToShortDateString(),
                                    now.ToShortTimeString());
                                System.Diagnostics.Trace.WriteLine(log);
                                file.WriteLine(log);
                            }
                        }
                    }
                }
                file.Close();
            }
        }

        #endregion

        #region BucketDataTable Saturation Routine

        /// <summary>
        /// the saturate data table routine
        /// </summary>
        /// <param name="obj"></param>
        private void SaturateDataTable(object obj)
        {
            //get the parameter
            SaturateDataTableData data = (SaturateDataTableData)obj;
            
            try
            {
                //the parent query for relations
                string parent_query = data.table.ConstructQuery();

                //check if we can saturate this table
                if (!data.table.CanSaturate())
                    return;

                //get the mod value
                Int64 mod = -1;
                if (data.table is BucketDataTable)
                    mod = (data.table as BucketDataTable).Bucket.BucketMod;

                //list of items
                List<IDataItem> list = new List<IDataItem>();

                //connect to db
                using (IDbConnection connection = DBFactory.CreateConnection(
                    data.table.Catalog.ConnectionString, data.table.Catalog.ConnectionType))
                {
                    //open the connection
                    connection.Open();

                    //create the command
                    using (IDbCommand command = DBFactory.CreateCommand(
                        parent_query, connection, data.table.Catalog.ConnectionType))
                    {
                        //set timeout
                        command.CommandTimeout = 5 * 60;

                        //execute the reader
                        using (IDataReader dr = command.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                        {
                            //parse the results
                            while (dr.Read())
                            {
                                //create the data item
                                try
                                {
                                    IDataItem item = (IDataItem)Activator.CreateInstance(data.table.TableMetadata.DataType);
                                    if (item != null)
                                    {
                                        //set the IDataItem with row info
                                        ProcessRow(dr, item, data.table, false);

                                        //add items to the list
                                        list.Add(item);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                                }
                            }
                        }
                    }
                    //close the connection for chad
                    connection.Close();
                }
                //process the parent items
                bool table_saturated = true;
                foreach (var item in list)
                {
                    //process the item
                    if (ProcessItem(item, data.table))
                        table_saturated = false;

                    //get the update field
                    object update = item.GetType().GetField(data.table.TableMetadata.SaturationUpdateField).GetValue(item);
                    data.table.UpdateLastValue(update);
                }
                //set table saturation
                data.table.Saturated = table_saturated;

                //report status
                ReportStatus(
                    data.table.Catalog.CatalogName,
                    mod,
                    data.table.TableMetadata.TableName,
                    list.Count,
                    data.table.DataItems.Count,
                    data.table.Catalog.CatalogDataTables[data.table.TableMetadata.TableName].DataItems.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            finally
            {
                data.table.LastSaturation = DateTime.UtcNow;
                data.table.Saturating = false;
                data.eventDone.Set();
            }
        }

        /// <summary>
        /// data class for starting saturation thread
        /// </summary>
        private class SaturateDataTableData
        {
            public ManualResetEvent eventDone { get; set; }
            public IReferenceDataTable table { get; set; }
        }

        /// <summary>
        /// process a row
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="item"></param>
        /// <param name="table_metadata"></param>
        private void ProcessRow(IDataReader dr, IDataItem item, IReferenceDataTable table, bool update_last_update)
        {
            //get the data
            List<Tuple<string,object>> data = new List<Tuple<string,object>>();
            for (int x = 0; x < dr.FieldCount; x++)
            {
                //get the column metadata
                string column_name = dr.GetName(x);
                IColumnMetadata col = null;
                if(table.TableMetadata.ColumnsMetadata.ContainsKey(column_name))
                    col = table.TableMetadata.ColumnsMetadata[column_name];

                if (col != null)
                {
                    //get the value
                    object value = dr[x];
                    data.Add(new Tuple<string, object>(column_name, value));

                    try
                    {
                        //check if the column is a collection
                        if (col.ColumnType == typeof(List<Tuple<List<long>, long>>))
                        {
                            string string_value = value.ToString();
                            List<Tuple<List<long>, long>> list = new List<Tuple<List<long>, long>>();
                            
                            //get the relationship for this field
                            var rel = table.Catalog.CatalogMetadata.Relationships.Values.FirstOrDefault(r => r.ParentChildCollection == column_name);

                            //check if null
                            if (!dr.IsDBNull(x))
                            {
                                foreach (var s in string_value.Split(','))
                                {
                                    string[] tuple = s.Split('|');
                                    if (tuple.Length == 2)
                                    {
                                        long item1;
                                        long item2;
                                        if (Int64.TryParse(tuple[0], out item1))
                                        {
                                            if (Int64.TryParse(tuple[1], out item2))
                                            {
                                                if (rel != null && rel.DistinctRelationship)
                                                {
                                                    var existing = list.FirstOrDefault(t => t.Item2 == item2);
                                                    if (existing == null)
                                                    {
                                                        List<long> sub_list = new List<long>();
                                                        sub_list.Add(item1);
                                                        list.Add(new Tuple<List<long>, long>(sub_list, item2));
                                                    }
                                                    else
                                                    {
                                                        existing.Item1.Add(item1);
                                                    }
                                                }
                                                else
                                                {
                                                    List<long> sub_list = new List<long>();
                                                    sub_list.Add(item1);
                                                    list.Add(new Tuple<List<long>, long>(sub_list, item2));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //List<long> list = string_value.Split(',').Select(t => TryParseInt64(t))
                            //                                  .Where(i => i.HasValue)
                            //                                  .Select(i => i.Value)
                            //                                  .ToList();
                            item.GetType().GetField(col.FieldMapping).SetValue(item, list);
                        }
                        else
                        {
                            //check if null
                            if (!dr.IsDBNull(x))
                            {
                                //set the value with reflection
                                item.GetType().GetField(col.FieldMapping).SetValue(item, value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.ToString());
                    }
                        
                    //update the last update value
                    if (column_name == table.TableMetadata.SaturationUpdateField && update_last_update)
                        table.UpdateLastValue(value);
                }
            }
            if (item is Mention)
            {
                if ((item as Mention).Tags == null)
                {
                    System.Diagnostics.Trace.WriteLine("NO");
                }
            }
        }

        /// <summary>
        /// parse string to int 64 helper func
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static long? TryParseInt64(string text)
        {
            long value;
            return long.TryParse(text, out value) ? value : (long?)null;
        }
        /// <summary>
        /// process an item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="table"></param>
        private bool ProcessItem(IDataItem item, IReferenceDataTable table)
        {
            //check if item is deleted
            if (table.IsDeleted(item))
            {
                table.DeleteItem(item, true);
                return false;
            }
            //add/update the list
            else
            {
                return table.AddItem(item);
            }
        }

        
        /// <summary>
        /// report on status of saturation
        /// </summary>
        /// <param name="catalog_name"></param>
        /// <param name="mod"></param>
        /// <param name="table_name"></param>
        /// <param name="local_count"></param>
        /// <param name="mod_count"></param>
        /// <param name="global_count"></param>
        private void ReportStatus(
            string catalog_name,
            Int64 mod,
            string table_name,
            int local_count,
            int mod_count,
            int global_count)
        {

//#if DEBUG
            string log = string.Format("[{0}][Mod:{1}][Table:{2}][LocalCount:{3}][ModCount:{4}][GlobalCount:{5}]",
                catalog_name,
                mod,
                table_name,
                local_count,
                mod_count,
                global_count);

            System.Diagnostics.Trace.WriteLine(log);
//#else

//#endif
        }

        #endregion

    }
}
