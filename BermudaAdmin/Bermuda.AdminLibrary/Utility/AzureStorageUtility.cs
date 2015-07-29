using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Bermuda.AdminLibrary.Utility
{
    public class AzureStorageUtility
    {
        // Variables for the cloud storage objects.
        private static CloudStorageAccount cloudStorageAccount = null;
        private static CloudBlobClient blobClient = null;
        private static CloudBlobContainer blobContainer = null;
        private static BlobContainerPermissions containerPermissions = null;
        private static CloudBlob blob = null;

        public static List<CloudBlob> ListBlobs()
        {
            List<IListBlobItem> blobItemList = null;

            List<CloudBlob> blobList = null;

            try
            {
                string storageAccountConnection = string.Empty;

                storageAccountConnection = ConfigurationManager.AppSettings["StorageAccount.ConnectionString"].ToString();

                Logger.Write("Storage Connection: " + storageAccountConnection);

                // If you want to use Windows Azure cloud storage account, use the following
                // code (after uncommenting) instead of the code above.
                cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnection);

                Logger.Write("Blob Uri: " + cloudStorageAccount.BlobEndpoint.AbsoluteUri);

                // Create the blob client, which provides
                // authenticated access to the Blob service.
                blobClient = cloudStorageAccount.CreateCloudBlobClient();

                string deploymentPackageFolderString = string.Empty;

                deploymentPackageFolderString = ConfigurationManager.AppSettings["DeploymentPackageFolder"].ToString();

                Logger.Write("Deployment Package Folder: " + deploymentPackageFolderString);

                // Get the container reference.
                blobContainer = blobClient.GetContainerReference(deploymentPackageFolderString);

                // Create the container if it does not exist.
                blobContainer.CreateIfNotExist();

                // Set permissions on the container.
                containerPermissions = new BlobContainerPermissions();

                // This sample sets the container to have public blobs. Your application
                // needs may be different. See the documentation for BlobContainerPermissions
                // for more information about blob container permissions.
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;

                blobContainer.SetPermissions(containerPermissions);

                BlobRequestOptions blobReqOptions = new BlobRequestOptions();

                blobReqOptions.BlobListingDetails = BlobListingDetails.All;
                blobReqOptions.Timeout = new TimeSpan(0, 5, 0);
                blobReqOptions.UseFlatBlobListing = true;

                blobItemList = blobContainer.ListBlobs(blobReqOptions).ToList();

                if (blobList == null)
                {
                    blobList = new List<CloudBlob>();
                }

                foreach (IListBlobItem blobItem in blobItemList)
                {
                    Logger.Write("Blob Uri: " + blobItem.Uri.ToString());

                    CloudBlob blobEntry = new CloudBlob(blobItem.Uri.ToString());

                    blobEntry.FetchAttributes();

                    blobList.Add(blobEntry);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in List<IListBlobItem> ListBlobs()  Error: {0}", ex.Message));

                blobList = null;
            }

            return blobList;
        }

        public static void UploadBlobFile(byte[] fileBytes, string fileName)
        {
            try
            {
                string storageAccountConnection = string.Empty;

                storageAccountConnection = ConfigurationManager.AppSettings["StorageAccount.ConnectionString"].ToString();

                // If you want to use Windows Azure cloud storage account, use the following
                // code (after uncommenting) instead of the code above.
                cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnection);

                // Create the blob client, which provides
                // authenticated access to the Blob service.
                blobClient = cloudStorageAccount.CreateCloudBlobClient();

                string deploymentPackageFolderString = string.Empty;

                deploymentPackageFolderString = ConfigurationManager.AppSettings["DeploymentPackageFolder"].ToString();

                // Get the container reference.
                blobContainer = blobClient.GetContainerReference(deploymentPackageFolderString);

                // Create the container if it does not exist.
                blobContainer.CreateIfNotExist();

                // Set permissions on the container.
                containerPermissions = new BlobContainerPermissions();

                // This sample sets the container to have public blobs. Your application
                // needs may be different. See the documentation for BlobContainerPermissions
                // for more information about blob container permissions.
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;

                blobContainer.SetPermissions(containerPermissions);

                blob = blobContainer.GetBlobReference(fileName);

                // Open a stream using the cloud object
                using (BlobStream blobStream = blob.OpenWrite())
                {
                    blobStream.Write(fileBytes, 0, fileBytes.Count());

                    blobStream.Flush();

                    blobStream.Close();
                }
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in UploadBlobFile()  Error: {0}", ex.Message));
            }
        }

        public static string ReadBlobFile(string fileName)
        {
            // byte[] fileBytes = null;
            string fileBytes = string.Empty;

            try
            {
                string storageAccountConnection = string.Empty;

                storageAccountConnection = ConfigurationManager.AppSettings["StorageAccount.ConnectionString"].ToString();

                // If you want to use Windows Azure cloud storage account, use the following
                // code (after uncommenting) instead of the code above.
                cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnection);

                // Create the blob client, which provides
                // authenticated access to the Blob service.
                blobClient = cloudStorageAccount.CreateCloudBlobClient();

                string deploymentPackageFolderString = string.Empty;

                deploymentPackageFolderString = ConfigurationManager.AppSettings["DeploymentPackageFolder"].ToString();

                // Get the container reference.
                blobContainer = blobClient.GetContainerReference(deploymentPackageFolderString);

                // Create the container if it does not exist.
                blobContainer.CreateIfNotExist();

                // Set permissions on the container.
                containerPermissions = new BlobContainerPermissions();

                // This sample sets the container to have public blobs. Your application
                // needs may be different. See the documentation for BlobContainerPermissions
                // for more information about blob container permissions.
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;

                blobContainer.SetPermissions(containerPermissions);

                blob = blobContainer.GetBlobReference(fileName);

                BlobRequestOptions blobReqOptions = new BlobRequestOptions();

                blobReqOptions.Timeout = new TimeSpan(0, 5, 0);

                fileBytes = blob.DownloadText(blobReqOptions);
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in ReadBlobFile()  Error: {0}", ex.Message));

                fileBytes = null;
            }

            return fileBytes;
        }
    }
}
