using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon;
using Bermuda.Interface;
using Amazon.S3.Model;
using System.IO;
using System.IO.Compression;

namespace Bermuda.FileSaturator
{
    public class S3FileProcessor : IFileProcessor
    {
        #region Interface Implementation

        public ILineProcessor LineProcessor
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool OpenFile(object FileObject, ITableMetadata TableMeta)
        {
            //throw new NotImplementedException();
            var fileName = (S3Object)FileObject;
            AmazonS3 s3 = AWSClientFactory.CreateAmazonS3Client();

            var gor = new GetObjectRequest().WithBucketName(fileName.BucketName).WithKey(fileName.Key);
            var file = s3.GetObject(gor);
            string text = "";

            using (var ms = new MemoryStream())
            {
                System.Diagnostics.Trace.WriteLine("Started reading file");
                file.ResponseStream.CopyTo(ms); //actually fetches the file from S3
                System.Diagnostics.Trace.WriteLine("Finished reading file");
                using (var gzipStream = new GZipStream(new MemoryStream(ms.ToArray()), CompressionMode.Decompress))
                {
                    System.Diagnostics.Trace.WriteLine("Decompressing file");
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
                    System.Diagnostics.Trace.WriteLine("Finished decompressing file");
                }
            }

            Lines = text.Split(TableMeta.ColumnDelimiters, StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Trace.WriteLine("Finished reading file");

            lineCount = -1;
            return true;
        }

        public bool NextLine()
        {
            if (Lines.Count() <= lineCount + 1)
            {
                lineCount++;
                return true;
            }
            else
            {
                return false;
            }
                
        }

        public string GetLine()
        {
            return Lines[lineCount];
        }

        public List<object> GetFileObjects()
        {
            AmazonS3 s3 = AWSClientFactory.CreateAmazonS3Client();
            List<S3Object> Files = new List<S3Object>();

            string nextMarker = "";
            do
            {
                ListObjectsRequest lor = new ListObjectsRequest().WithBucketName(AmazonBucket).WithPrefix(AmazonPrefix).WithMarker(nextMarker);
                var response = s3.ListObjects(lor);

                if (response.IsTruncated)
                    nextMarker = response.NextMarker;
                else
                    nextMarker = "";

                Files.AddRange(response.S3Objects);
                System.Diagnostics.Trace.WriteLine(string.Format("Added {0} files.   Total files: {1}", response.S3Objects.Count, Files.Count));
            } while (nextMarker != "");

            return Files.Cast<object>().ToList();

            //var buckets = ComputeNode.Catalogs.Values.Cast<ICatalog>().Where(c => c.CatalogName == "WikipediaData").First().Buckets;
            //var bucketMods = buckets.Select(b => b.Value.BucketMod).ToList();
            //myFiles = allFiles.Where(f => bucketMods.Contains(f.GetHashCode() % ComputeNode.GlobalBucketCount)).OrderBy(f => f.Key).ToList();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Variables and Properties

        public string AmazonBucket { get; set; }
        public string AmazonPrefix { get; set; }
        private string[] Lines = null;
        int lineCount;

        #endregion

        #region Constructor

        public S3FileProcessor()
        {
        }

        #endregion
    }
}
