using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using System.Windows.Forms;

namespace Bermuda.BermudaConfig.Storage.Azure
{
    public class StorageAzure : IStorageAccess, ICloudStorageBrowser
    {
        #region Variables and Properties

        /// <summary>
        /// the storage type
        /// </summary>
        public StorageFactory.StorageType StorageType { get; set; }

        /// <summary>
        /// the connection for storage
        /// </summary>
        private const string StorageConnection = "DefaultEndpointsProtocol=https;AccountName=evoappdata;AccountKey=Bx0L1WrJ++RyQAAtHYcMNmJOl2XhGd8A3EEm/6pEykfnkcFMd7HxH9f2nXtufRyaO/oHrhn2WUYwUw75DQUJGQ==";

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with type
        /// </summary>
        /// <param name="type"></param>
        public StorageAzure(StorageFactory.StorageType type)
        {
            StorageType = type;
        }

        #endregion

        #region IStorageAccess

        /// <summary>
        /// open the file dialog
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
        /// read blob from a container in azure
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
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(PathName);
                container.CreateIfNotExist();
                CloudBlob blob = container.GetBlobReference(FileName);
                Data = blob.DownloadText();
                
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// save the blob to the container in azure
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool SaveFile(string Data, string PathName, string FileName)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(PathName);
                container.CreateIfNotExist();
                CloudBlob blob = container.GetBlobReference(FileName);
                blob.UploadText(Data);
                
                return true;
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
            get { return "Browse Azure Storage"; }
        }

        /// <summary>
        /// get the containers
        /// </summary>
        /// <param name="Buckets"></param>
        /// <returns></returns>
        public bool GetBuckets(out IEnumerable<string> Buckets)
        {
            Buckets = null;
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                var containers = blobClient.ListContainers();
                List<string> list = new List<string>();
                containers.ToList().ForEach(c => list.Add(c.Name));
                Buckets = list;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// get the blobs in a container in azure
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="Files"></param>
        /// <returns></returns>
        public bool GetFiles(string bucket, out IEnumerable<string> Files)
        {
            Files = null;
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(bucket);
                container.CreateIfNotExist();
                var blobs = container.ListBlobs();
                List<string> list = new List<string>();
                foreach (var blob in blobs)
                {
                    string name = blob.Uri.Segments.Last();
                    list.Add(name);
                }
                Files = list;

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// delete a blob from a container in azure
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool DeleteFile(string PathName, string FileName)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(PathName);
                container.CreateIfNotExist();
                CloudBlob blob = container.GetBlobReference(FileName);
                blob.Delete();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// create a new continaer in azure
        /// </summary>
        /// <param name="PathName"></param>
        /// <returns></returns>
        public bool NewDirectory(string PathName)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(PathName.ToLower());
                container.CreateIfNotExist();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// delete the container in azure
        /// </summary>
        /// <param name="PathName"></param>
        /// <returns></returns>
        public bool DeleteDirectory(string PathName)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccount"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(PathName);
                container.Delete();

                return true;
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
