using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Bermuda.Interface;
using System.Threading;
using Bermuda.Util;
using Amazon;
using Bermuda.Catalog;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bermuda.FileSaturator
{
    public class FileSaturator : IDataProcessor
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
        /// the list of files to saturate
        /// </summary>
        private List<FileData> SaturationFiles { get; set; }

        //List<FileData> MyFiles { get; set; }

        /// <summary>
        /// the last time we refreshed the file data
        /// </summary>
        private DateTime LastFileDataRefresh { get; set; }

        private string CatalogName { get; set; }
        private string TableName { get; set; }
        private IFileProcessor FileProcessor { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with compute node
        /// </summary>
        /// <param name="compute_node"></param>
        //public FileSaturator(IComputeNode compute_node)
        //{
        //    ComputeNode = compute_node;

        //    SaturationThreadCount = Math.Min(64,Environment.ProcessorCount * 4);
        //    //SaturationThreadCount = 1;

        //    SaturationFiles = new List<FileData>();

        //    LastFileDataRefresh = DateTime.UtcNow.AddDays(-1);
        //}

        public FileSaturator(IComputeNode compute_node, IFileProcessor file_processor, string catalog_name, string table_name)
        {
            ComputeNode = compute_node;
            CatalogName = catalog_name;
            TableName = table_name;
            FileProcessor = file_processor;

            //SaturationThreadCount = Math.Min(64,Environment.ProcessorCount * 4);
            SaturationThreadCount = 1;

            SaturationFiles = new List<FileData>();

            LastFileDataRefresh = DateTime.UtcNow.AddDays(-1);
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

                //Task.Factory.StartNew(ChadStart);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        void ChadStart()
        {
            RefreshFileData();

            var buckets = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName.Equals(CatalogName)).First().Buckets;
            var bucketMods = buckets.Select(b => b.Value.BucketMod).ToList();
            var MyFiles = SaturationFiles.Where(f => bucketMods.Contains(Math.Abs(f.GetHashCode()) % ComputeNode.GlobalBucketCount)).OrderBy(f => f.Name).ToList();

            MyFiles.AsParallel().ForAll(f =>
                SaturateFile(new SaturateFileData()
                {
                    eventDone = null,
                    Data = f
                }));
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

        /// <summary>
        /// refresh the list of files
        /// </summary>
        private void RefreshFileData()
        {
            //List<string> files = FileProcessor.GetFileNames();
            List<object> files = FileProcessor.GetFileObjects();

            //get the new files
            //var new_files = files.Where(f => !SaturationFiles.Any(sf => sf.Name.Equals(f, StringComparison.InvariantCultureIgnoreCase))).ToList();

            var buckets = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName.Equals(CatalogName)).First().Buckets;
            var bucketMods = buckets.Select(b => b.Value.BucketMod).ToList();
            //var MyFiles = files.Where(f => bucketMods.Contains(Math.Abs(f.GetHashCode()) % ComputeNode.GlobalBucketCount)).ToList();
            
            //add to the 
            //new_files.ForEach(f =>
            files.ForEach(f =>
                {
                    var fd = new FileData()
                    {
                        Name = f,
                        ModifiedDate = DateTime.UtcNow,
                        Saturated = false,
                        Saturating = false,
                    };

                    if (bucketMods.Contains(Math.Abs(fd.GetHashCode()) % ComputeNode.GlobalBucketCount))
                        SaturationFiles.Add(fd);
                });
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
            for (int x = 0; x < SaturationThreadCount; x++)
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
                        if ((DateTime.UtcNow - LastFileDataRefresh).TotalMinutes > 5)
                        {
                            RefreshFileData();

                            LastFileDataRefresh = DateTime.UtcNow;

                            //PrintReport();
                        }
                        //wait at least on signaled event
                        int index = WaitHandle.WaitAny(events, 5000);

                        //check the return
                        if (index != WaitHandle.WaitTimeout)
                        {
                            //get the next file to service
                            FileData file = SaturationFiles.Where(f => f.Saturating == false && f.Saturated == false).OrderBy(f => f.ModifiedDate).FirstOrDefault();

                            //System.Diagnostics.Trace.WriteLine(SaturationFiles.Where(f => f.Saturated == false ).Count());

                            //make sure we got one
                            if (file != null)
                            {
                                //create the thread
                                file.Saturating = true;
                                events[index].Reset();
                                threads[index] = new Thread(new ParameterizedThreadStart(SaturateFile));
                                threads[index].Start(new SaturateFileData()
                                {
                                    eventDone = events[index],
                                    Data = file
                                });
                                
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
        /// thread to saturate from a file
        /// </summary>
        /// <param name="obj"></param>
        /// private void SaturateFile(object obj)
        private void SaturateFile(object obj)
        {
            SaturateFileData data = (SaturateFileData)obj;

            System.Diagnostics.Trace.WriteLine(SaturationFiles.Where(f => f.Saturated == false).Count());

            try
            {
                ICatalog catalog = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName.Equals(CatalogName)).First();
                
                long mod = Math.Abs(data.Data.GetHashCode()) % ComputeNode.GlobalBucketCount;
                var bucket = catalog.Buckets.Values.FirstOrDefault(b => b.BucketMod == mod);
                ITableMetadata tableMeta = catalog.CatalogMetadata.Tables[TableName];

                // create a copy of the FileProcessor by reflection
                using (var proc = (IFileProcessor)FileProcessor.GetType().GetConstructor(new Type[] { }).Invoke(new object[] { }))
                {
                    // create a copy of the LineProcessor by reflection
                    proc.LineProcessor = (ILineProcessor)FileProcessor.LineProcessor.GetType().GetConstructor(new Type[] { }).Invoke(new object[] { });

                    if (!proc.OpenFile(data.Data.Name, tableMeta))
                        throw new Exception("Error Opening file: " + data.Data.Name);

                    // columns metadata ordered by ordinal position
                    var columns = tableMeta.ColumnsMetadata.Values.ToList().OrderBy(f => f.OrdinalPosition).ToList();

                    if (tableMeta.IsFixedWidth)
                    {
                        while (proc.NextLine())
                        {
                            // must fix this....
                            var item = new WeatherDataItem();
                            var line = proc.GetLine();

                            // iterate through the columns in order by ordinal position
                            columns.ForEach(column =>
                                {
                                    var field = line.Substring(column.FixedWidthStartIndex, column.FixedWidthLength);

                                    // set the field by reflection
                                    object result;
                                    if (proc.LineProcessor.ProcessColumn(column, field, out result))
                                    {
                                        item.GetType().GetField(column.ColumnName).SetValue(item, result);
                                    }
                                });

                            // and add to bucket
                            bucket.BucketDataTables[TableName].AddItem(item);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //columns are delimited...untested code
                        //while (proc.NextLine())
                        //{
                        //    var item = new WeatherDataItem();
                        //    var line = proc.GetLine();
                        //    var fields = line.Split(tableMeta.ColumnDelimiters, StringSplitOptions.None);

                        //    tableMeta.ColumnsMetadata.Values.ToList().ForEach(column =>
                        //    {
                        //        var field = fields[column.OrdinalPosition];

                        //        object result;
                        //        if (proc.ProcessColumn(column, field, out result))
                        //        {
                        //            item.GetType().GetField(column.ColumnName).SetValue(item, result);
                        //        }
                        //    });
                        //    bucket.BucketDataTables["Weather"].AddItem(item);
                        //}
                    }
                    data.Data.Saturated = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            finally
            {
                //data.table.LastSaturation = DateTime.UtcNow;
                data.Data.Saturating = false;
                if(data.eventDone != null)
                    data.eventDone.Set();
            }
        }
        
        

        public IEnumerable<IDataItem> ParseFile(string[] allLines)
        {
            var res = new ConcurrentBag<IDataItem>();
            
            // header row
            //List<string> allLines = new List<string>();
            //string currLine = reader.ReadLine();

            //while ((currLine = reader.ReadLine()) != null)
            //{
            //    allLines.Add(currLine);
            //}

            allLines.Skip(1).ToList().ForEach( line =>
                {
                    var item = new WeatherDataItem();

                    try
                    {
                        //item.Id = line.GetHashCode();

                        item.Station = line.Substring(0, 6);

                        if (!line.Substring(7, 5).Equals("99999"))
                            item.Wban = line.Substring(7, 5);

                        item.Date = new DateTime(int.Parse(line.Substring(14, 4)),
                                            int.Parse(line.Substring(18, 2)),
                                            int.Parse(line.Substring(20, 2)));

                        if (!line.Substring(24, 6).Equals("9999.9"))
                            item.MeanTemperature = float.Parse(line.Substring(24, 6));

                        item.MeanTemperatureCount = int.Parse(line.Substring(31, 2));

                        if (!line.Substring(35, 6).Equals("9999.9"))
                            item.MeanDewpoint = float.Parse(line.Substring(35, 6));

                        item.MeanDewpointCount = int.Parse(line.Substring(42, 2));

                        if (!line.Substring(46, 6).Equals("9999.9"))
                            item.MeanSealevelPressure = float.Parse(line.Substring(46, 6));

                        item.MeanSealevelPressureCount = int.Parse(line.Substring(53, 2));

                        if (!line.Substring(57, 6).Equals("9999.9"))
                            item.MeanStationPressure = float.Parse(line.Substring(57, 6));

                        item.MeanStationPressureCount = int.Parse(line.Substring(64, 2));

                        if (!line.Substring(68, 5).Equals("999.9"))
                            item.MeanVisibility = float.Parse(line.Substring(68, 5));

                        item.MeanVisibilityCount = int.Parse(line.Substring(74, 2));

                        if (!line.Substring(78, 5).Equals("999.9"))
                            item.MeanWindSpeed = float.Parse(line.Substring(78, 5));
                    
                        item.MeanWindSpeedCount = int.Parse(line.Substring(84, 2));

                        if (!line.Substring(88, 5).Equals("999.9"))
                            item.MaximumSustainedWindSpeed = float.Parse(line.Substring(88, 5));

                        if (!line.Substring(95, 5).Equals("999.9"))
                            item.MaximumGust = float.Parse(line.Substring(95, 5));

                        if (!line.Substring(102, 6).Equals("9999.9"))
                            item.MaximumTemperature = float.Parse(line.Substring(102, 6));

                        item.MaximumTemperatureFlag = line.Substring(108, 1).ToCharArray().First();

                        if (!line.Substring(110, 6).Equals("9999.9"))
                            item.MinimumTemperature = float.Parse(line.Substring(110, 6));

                        item.MinimumTemperatureFlag = line.Substring(116, 1).ToCharArray().First();

                        if (!line.Substring(118, 5).Equals("99.99"))
                            item.Precipitation = float.Parse(line.Substring(118, 5));

                        item.PrecipitationFlag = line.Substring(123, 1).ToCharArray().First();

                        if (!line.Substring(125, 5).Equals("999.9"))
                            item.SnowDepth = float.Parse(line.Substring(125, 5));

                        item.IsFog = (line.Substring(132, 1).Equals("1"));
                        item.IsRain = (line.Substring(133, 1).Equals("1"));
                        item.IsSnow = (line.Substring(134, 1).Equals("1"));
                        item.IsHail = (line.Substring(135, 1).Equals("1"));
                        item.IsThunder = (line.Substring(136, 1).Equals("1"));
                        item.IsTornado = (line.Substring(137, 1).Equals("1"));

                        var epochDays = item.Date.Subtract(new DateTime(1929, 1, 1)).TotalDays;
                        item.Id = long.Parse(epochDays + item.Station + line.Substring(7, 5));

                        res.Add(item);
                    
                    }
                    catch (FormatException ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.ToString());
                        System.Diagnostics.Trace.WriteLine("Parse error on line: {0}", line );
                    }
                });

            return res.ToList();
        }

        #endregion

        
        #region Classes

        /// <summary>
        /// represents a file we are downloading and saturating from
        /// </summary>
        private class FileData
        {
            //public string Name { get; set; }
            public object Name { get; set; }
            public DateTime ModifiedDate { get; set; }
            public bool Saturating { get; set; }
            public bool Saturated { get; set; }

            public override int GetHashCode()
            {
                return (this.Name + this.ModifiedDate.ToString()).GetHashCode();
            }
        }

        /// <summary>
        /// data required to saturate
        /// </summary>
        private class SaturateFileData
        {
            public ManualResetEvent eventDone { get; set; }
            public FileData Data { get; set; }
        }

        #endregion
    }
}
