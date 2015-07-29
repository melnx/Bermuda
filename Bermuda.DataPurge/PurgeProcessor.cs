using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Threading;
using Bermuda.Util;
using System.Diagnostics;
using Bermuda.Catalog;
using System.Runtime.InteropServices;

namespace Bermuda.DataPurge
{
    public class PurgeProcessor : IDataProcessor
    {
        #region ISaturator Implementation

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
        /// the number of thread to use for purgin
        /// </summary>
        public int PurgeThreadCount { get; set; }

        /// <summary>
        /// the main purge thread
        /// </summary>
        private Thread PurgeThread { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with catalog
        /// </summary>
        public PurgeProcessor(IComputeNode computeNode)
        {
            ComputeNode = computeNode;
#if DEBUG
            PurgeThreadCount = Environment.ProcessorCount;
#else
            PurgeThreadCount = Math.Min(Environment.ProcessorCount * 4, 64);
#endif
            
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// purger start
        /// </summary>
        /// <returns></returns>
        private bool Start()
        {
            try
            {
                eventStop.Reset();
                PurgeThread = new Thread(new ThreadStart(Purge));
                PurgeThread.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// purger stop
        /// </summary>
        /// <returns></returns>
        private bool Stop()
        {
            //check if we are running
            if (PurgeThread == null || PurgeThread.Join(0))
                return true;

            //set the event
            eventStop.Set();

            //wait on main thread to join
            PurgeThread.Join();

            return true;
        }

        #endregion

        #region Main Purge Thread Routine

        /// <summary>
        /// purge routine
        /// </summary>
        private void Purge()
        {
            //we are purging to min memory
            bool Purging = false;

            //the last memory report
            DateTime LastMemoryReport = DateTime.UtcNow.AddDays(-1);

            //the currently executing threads and thier events
            Thread[] threads = new Thread[PurgeThreadCount];
            ManualResetEvent[] events = new ManualResetEvent[PurgeThreadCount];
            for (int x = 0; x < PurgeThreadCount; x++)
            {
                events[x] = new ManualResetEvent(true);
            }

            //main loop
            while (true)
            {
                try
                {
                    //wait at least on signaled event
                    int index = WaitHandle.WaitAny(events, 5000);

                    //check the return
                    if (index != WaitHandle.WaitTimeout)
                    {
                        //check the memory
                        var memStatus = SystemInfo.GetMemoryStatusEx();
                        if ((double)memStatus.ullAvailPhys / (double)memStatus.ullTotalPhys * 100.0 < (ComputeNode.Purging ? ComputeNode.MaxAvailableMemoryPercent : ComputeNode.MinAvailableMemoryPercent) && ComputeNode.SaturationTables.Count > 0)
                        {
                            //mark as puring
                            ComputeNode.Purging = true;

                            //check if we need to report memory
                            if ((DateTime.UtcNow - LastMemoryReport).TotalSeconds > 10)
                            {
                                //fire off the gc and claim working set
                                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                                GC.WaitForPendingFinalizers();
                                MinimizeFootprint();

                                //report memory usage
                                LastMemoryReport = DateTime.UtcNow;
                                System.Diagnostics.Trace.WriteLine(string.Format("Purge Memory Status - TotalMemory:{0} MB, AvailableMemory:{1} MB, AvailablePercent:{2} %",
                                    memStatus.ullTotalPhys / 1024 / 1024, memStatus.ullAvailPhys / 1024 / 1024, (double)memStatus.ullAvailPhys / (double)memStatus.ullTotalPhys * 100.0));
                            }
                            //get the next bucket data table to service
                            IDataTable table = computeNode.GetAllCatalogTables().Where(t => t.Purging == false /*&& t.DataItems.Count > 1000*/).OrderBy(t => t.LastPurge).FirstOrDefault();

                            //make sure we got one
                            if (table != null)
                            {
                                //create the thread
                                table.Purging = true;
                                table.LastPurge = DateTime.UtcNow;
                                events[index].Reset();
                                threads[index] = new Thread(new ParameterizedThreadStart(PurgeDataTable));
                                threads[index].Start(new PurgeDataTableData()
                                {
                                    eventDone = events[index],
                                    table = table
                                });
                            }
                            else
                            {
                                //sleep for little while
                                if (eventStop.WaitOne(5000))
                                    return;
                            }
                        }
                        else
                        {
                            //report if we just completed
                            if (ComputeNode.Purging)
                            {
                                System.Diagnostics.Trace.WriteLine(string.Format("Purge Memory Complete - TotalMemory:{0} MB, AvailableMemory:{1} MB, AvailablePercent:{2} %",
                                    memStatus.ullTotalPhys / 1024 / 1024, memStatus.ullAvailPhys / 1024 / 1024, (double)memStatus.ullAvailPhys / (double)memStatus.ullTotalPhys * 100.0));
                            }
                            //mark as not purging
                            ComputeNode.Purging = false;

                            //wait for 1 minute
                            if (eventStop.WaitOne(10 * 1000))
                                return;
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

        #endregion

        #region Purge Routine

        /// <summary>
        /// the purge data table routine
        /// </summary>
        /// <param name="obj"></param>
        private void PurgeDataTable(object obj)
        {
            //get the parameter
            PurgeDataTableData data = (PurgeDataTableData)obj;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int count = 0;
                long msQuery = 0;
                long msEnumerate = 0;

                //check operation
                if (data.table.TableMetadata.SaturationPurgeOperation != Constants.PurgeOperations.PURGE_OP_NONE)
                {
                    //get the purge items
                    var results = data.table.GetPurgeItems();

                    //get the enumerator info for reflection
                    //var ge = results.GetType().GetMethods().Where(e => e.Name == "GetEnumerator" && e.GetParameters().Count() == 1).FirstOrDefault();
                    //var enumerator = ge.Invoke(results, new object[] { ParallelMergeOptions.NotBuffered });
                    var ge = results.GetType().GetMethods().Where(e => e.Name == "GetEnumerator" && e.GetParameters().Count() == 0).FirstOrDefault();
                    var enumerator = ge.Invoke(results, new object[] { });
                    var mn = enumerator.GetType().GetMethod("MoveNext");

                    //record the query time
                    msQuery = sw.ElapsedMilliseconds;

                    //parse the enumerator
                    while ((bool)mn.Invoke(enumerator, new object[] { }))
                    {
                        //get the current object
                        var oa = new object[] { };
                        var objCurrent = enumerator.GetType().GetProperty("Current").GetValue(enumerator, oa);
                        IDataItem item = (IDataItem)objCurrent;

                        //delete the item
                        //lock (data.table.TableMetadata.CatalogMetadata.Catalog)
                        {
                            data.table.DeleteItem(item, false);
                        }
                        //inc the count
                        count++;
                    }
                    msEnumerate = sw.ElapsedMilliseconds - msQuery;
                }
                //check on purge
                if (count > 0 && data.table.DataItems.Count > 0)
                {
                    //report the deletion
                    System.Diagnostics.Trace.WriteLine(
                        string.Format(
                            "Saturator Purge Complete - Catalog:{0}, Table:{1}, TableCount:{2}, PurgeCount:{3}, Time:{4} ms, QueryTime:{5}, EnumerateTime:{6}",
                            data.table.TableMetadata.CatalogMetadata.Catalog.CatalogName,
                            data.table.TableMetadata.TableName,
                            data.table.DataItems.Count,
                            count,
                            sw.ElapsedMilliseconds,
                            msQuery,
                            msEnumerate));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            finally
            {
                data.table.Purging = false;
                data.eventDone.Set();
            }
        }

        /// <summary>
        /// data class for starting purge thread
        /// </summary>
        private class PurgeDataTableData
        {
            public ManualResetEvent eventDone { get; set; }
            public IDataTable table { get; set; }
        }

        #endregion

        #region Methods

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        static void MinimizeFootprint()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        #endregion

    }
}
