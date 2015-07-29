using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Diagnostics;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Web;
using System.Globalization;
using System.Threading.Tasks;

namespace Bermuda.S3Saturator
{
    public class S3Saturator : IDataProcessor
    {
        AmazonS3 s3;
        List<S3Object> allFiles = new List<S3Object>();
        List<S3Object> myFiles = new List<S3Object>();
        readonly char[] delimiters = new char[] { '\r', '\n' };
        readonly string CATALOG = "WikipediaData";
        readonly string TABLE = "PageStats";
        readonly string PREFIX = "wikistats/pagecounts/";
        readonly string DATE_EXTRACTION_PREFIX = "wikistats/pagecounts/pagecounts-";
        readonly string BUCKET = "PublicDataSets";
        readonly CultureInfo PROVIDER = CultureInfo.InvariantCulture;
        readonly string FORMAT = "yyyyMMdd-HH";

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

        public S3Saturator(IComputeNode computeNode)
        {
            this.ComputeNode = computeNode;         
        }

        public bool StartProcessor()
        {
            Trace.WriteLine("");
            Trace.WriteLine("Started S3Saturator");

            GetFileListing();
            Task.Factory.StartNew(StartDownloading);

            return true;
        }

        void StartDownloading()
        {
            myFiles.ForEach(file => ProcessFile(file));
            //myFiles.AsParallel().WithDegreeOfParallelism(4).ForAll(file => ProcessFile(file));
        }

        private void ProcessFile(S3Object file)
        {
            Trace.WriteLine(string.Format("Processing item {0}", file.Key));

            var lines = ReadS3File(file);

            var trimmedDate = file.Key.Substring(DATE_EXTRACTION_PREFIX.Length, 11);
            var date = DateTime.ParseExact(trimmedDate, FORMAT, PROVIDER);

            var fileMod = file.GetHashCode() % ComputeNode.GlobalBucketCount;
            var buckets = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName == CATALOG).First().Buckets;
            var bucketMod = buckets.First(b => b.Value.BucketMod == fileMod).Value;

            Trace.WriteLine(string.Format("Adding data items from {0}", file.Key));
            lines.AsParallel().ForAll(line =>
                {
                    var items = line.Split(' ');
                    Debug.Assert(items.Length == 4);

                    var projectCode = HttpUtility.UrlDecode(items[0]);
                    var pageName = HttpUtility.UrlDecode(items[1]);
                    var pageViews = int.Parse(items[2]);
                    var pageSizeKB = long.Parse(items[3]);

                    var wikiStat = new WikipediaHourlyPageStats(date, projectCode, pageName, pageViews, pageSizeKB);
                    bucketMod.BucketDataTables[TABLE].AddItem(wikiStat);
                });

            Trace.WriteLine(string.Format("Added data items from {0}", file.Key));
        }

        private String[] ReadS3File(S3Object fileName)
        {
            var gor = new GetObjectRequest().WithBucketName(BUCKET).WithKey(fileName.Key);
            var file = s3.GetObject(gor);
            string text = "";

            using (var ms = new MemoryStream())
            {
                Trace.WriteLine("Started reading file");
                file.ResponseStream.CopyTo(ms); //actually fetches the file from S3
                Trace.WriteLine("Finished reading file");
                using (var gzipStream = new GZipStream(new MemoryStream(ms.ToArray()), CompressionMode.Decompress))
                {
                    Trace.WriteLine("Decompressing file");
                    const int size = 4096;
                    byte[] buffer = new byte[size];
                    using (MemoryStream memory = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = gzipStream.Read(buffer, 0, size);
                            if (count > 0)
                            {
                                memory.Write(buffer, 0, count);
                            }
                        }
                        while (count > 0);
                        var memArray = memory.ToArray();
                        text = ASCIIEncoding.ASCII.GetString(memArray);
                    }
                    Trace.WriteLine("Finished decompressing file");
                }
            }

            var lines = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            Trace.WriteLine("Finished reading file");
            return lines;
        }

        private void GetFileListing()
        {
            s3 = AWSClientFactory.CreateAmazonS3Client();

            string nextMarker = "";
            do
            {
                ListObjectsRequest lor = new ListObjectsRequest().WithBucketName(BUCKET).WithPrefix(PREFIX).WithMarker(nextMarker);
                var response = s3.ListObjects(lor);

                if (response.IsTruncated)
                    nextMarker = response.NextMarker;
                else
                    nextMarker = "";

                allFiles.AddRange(response.S3Objects);
                Trace.WriteLine(string.Format("Added {0} files.   Total files: {1}", response.S3Objects.Count, allFiles.Count));
            } while (nextMarker != "");

            var buckets = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName == "WikipediaData").First().Buckets;
            var bucketMods = buckets.Select(b => b.Value.BucketMod).ToList();
            myFiles = allFiles.Where(f => bucketMods.Contains(f.GetHashCode() % ComputeNode.GlobalBucketCount)).OrderBy(f=>f.Key).ToList();
        }

        public bool StopProcessor()
        {
            Trace.WriteLine("Stopped S3Saturator");
            return true;
        }
    }
}
