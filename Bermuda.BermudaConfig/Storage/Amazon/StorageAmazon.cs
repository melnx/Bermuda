using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics;
using System.IO;

namespace Bermuda.BermudaConfig.Storage.Amazon
{
    public class StorageAmazon : IStorageAccess, ICloudStorageBrowser
    {
        #region Variables and Properties

        /// <summary>
        /// the storage type
        /// </summary>
        public StorageFactory.StorageType StorageType { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with type
        /// </summary>
        /// <param name="type"></param>
        public StorageAmazon(StorageFactory.StorageType type)
        {
            StorageType = type;
        }

        #endregion

        #region IStorageAccess

        /// <summary>
        /// open file dlg using cloud browser
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool OpenFileDialog(out string PathName, out string FileName)
        {
            PathName = null;
            FileName = null;
            try
            {
                //show browser
                CloudStorageBrowser dlg = new CloudStorageBrowser(true, this);
                var ret = dlg.ShowDialog();

                //check for cancel return
                if (!ret.HasValue || ret.Value == false)
                {
                    return false;
                }
                //get the selected info
                PathName = dlg.SelectedBucket;
                FileName = dlg.SelectedFile;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// Save file dialog using cloud browser
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool SaveFileDialog(out string PathName, out string FileName)
        {
            PathName = null;
            FileName = null;
            try
            {
                //show browser
                CloudStorageBrowser dlg = new CloudStorageBrowser(false, this);
                var ret = dlg.ShowDialog();

                //check for cancel return
                if (!ret.HasValue || ret.Value == false)
                {
                    return false;
                }
                //get the selected info
                PathName = dlg.SelectedBucket;
                FileName = dlg.SelectedFile;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// downloads the file from S3 to temp storage and reads it
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public bool ReadFile(string PathName, string FileName, out string Data)
        {
            Data = null;
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //download request
                    using(var response = client.GetObject(new GetObjectRequest()
                        .WithBucketName(PathName)
                        .WithKey(FileName)))
                    {
                        using (StreamReader reader = new StreamReader(response.ResponseStream))
                        {
                            //read the file
                            Data = reader.ReadToEnd();

                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// save the file to s3
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool SaveFile(string Data, string PathName, string FileName)
        {
            try
            {
                //check if the file exists
                IEnumerable<string> files;
                if (GetFiles(PathName, out files))
                {
                    if (files.Any(f => f.Equals(FileName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        //delete first if it exists
                        DeleteFile(PathName, FileName);
                    }
                }
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //save request
                    using(var response = client.PutObject(new PutObjectRequest()
                        .WithBucketName(PathName)
                        .WithKey(FileName)
                        .WithContentBody(Data)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// delete the filr from s3
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool DeleteFile(string PathName, string FileName)
        {
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //delete request
                    using (var response = client.DeleteObject(new DeleteObjectRequest()
                        .WithBucketName(PathName)
                        .WithKey(FileName)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// create a new bucket in s3
        /// </summary>
        /// <param name="PathName"></param>
        /// <returns></returns>
        public bool NewDirectory(string PathName)
        {
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //put bucket request
                    using (var response = client.PutBucket(new PutBucketRequest()
                        .WithBucketName(PathName)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// delete bucket from s3
        /// </summary>
        /// <param name="PathName"></param>
        /// <returns></returns>
        public bool DeleteDirectory(string PathName)
        {
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //delete bucket request
                    using (var response = client.DeleteBucket(new DeleteBucketRequest()
                        .WithBucketName(PathName)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }
        
        #endregion

        #region ICloudStorageBrowser

        /// <summary>
        /// the title for the browser window
        /// </summary>
        public string BrowserTitle
        {
            get { return "Browse Amazon S3"; }
        }

        /// <summary>
        /// get the buckets from s3
        /// </summary>
        /// <param name="Buckets"></param>
        /// <returns></returns>
        public bool GetBuckets(out IEnumerable<string> Buckets)
        {
            Buckets = null;
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //get bucket request
                    using (var response = client.ListBuckets(new ListBucketsRequest()))
                    {
                        List<string> list = new List<string>();
                        response.Buckets.ForEach(b => list.Add(b.BucketName));
                        Buckets = list;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// get the files in s3 from a bucket
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="Files"></param>
        /// <returns></returns>
        public bool GetFiles(string bucket, out IEnumerable<string> Files)
        {
            Files = null;
            try
            {
                //amazon client
                using (var client = new AmazonS3Client())
                {
                    //get object request
                    using (var response = client.ListObjects(new ListObjectsRequest()
                        .WithBucketName(bucket)))
                    {
                        List<string> list = new List<string>();
                        response.S3Objects.ForEach(o => list.Add(o.Key));
                        Files = list;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        #endregion


    }
}
