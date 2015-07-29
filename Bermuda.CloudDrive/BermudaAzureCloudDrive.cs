using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.IO;

namespace Bermuda.AzureCloudDrive
{
    public class BermudaAzureCloudDrive
    {
        string BermudaDataBlobContainerName; //"bermudadatadrive";
        string BermudaDataBlobName; //"bermudablob.vhd"   --this should be unique to worker role;
        string BermudaCloudDataDir; //"bermudaDataDir";

        private const string BermudaLocalDataDir = "BermudaLocalDataDir";

        private const int MaxDriveSize = 10 * 1024; // in MB
        private const int MountSleep = 30 * 1000; // 30 seconds;

        public BermudaAzureCloudDrive(string blobContainerName, string blobName, string cloudDataDir)
        {
            BermudaDataBlobContainerName = blobContainerName;
            BermudaDataBlobName = blobName;
            BermudaCloudDataDir = cloudDataDir;
        }

        public string Mount()
        {
            CloudDrive drive = null;
            string path = GetMountedPathFromBlob(
                BermudaLocalDataDir, 
                BermudaCloudDataDir, 
                BermudaDataBlobContainerName, 
                BermudaDataBlobName, 
                MaxDriveSize,
                true, 
                out drive);

            Trace.TraceInformation(string.Format("Obtained data drive as {0}", path));
            var dir = Directory.CreateDirectory(Path.Combine(path, @"bermuda"));
            Trace.TraceInformation(string.Format("Data directory is {0}", dir.FullName));
            return dir.FullName;
        }

        public static CloudBlobClient CloudBlobClient
        {
            get
            {
                var blobClient = CloudStorageAccount.CreateCloudBlobClient();
                return blobClient;
            }
        }

        public static CloudStorageAccount CloudStorageAccount
        {
            get
            {
                CloudStorageAccount.SetConfigurationSettingPublisher((s, f) => Console.Write(""));
                //storageAccount = CloudStorageAccount.DevelopmentStorageAccount; //CloudStorageAccount.FromConfigurationSetting(cloudDir);
                CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                {
                    configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
                });
                return CloudStorageAccount.FromConfigurationSetting("CloudDir");
            }
        }

        private string GetMountedPathFromBlob(
            string localCachePath,
            string cloudDir,
            string containerName,
            string blobName,
            int driveSize,
            bool waitOnMount,
            out CloudDrive bermudaDrive)
        {
            Trace.TraceInformation(string.Format("In mounting cloud drive for dir {0}", cloudDir));

            var storageAccount = CloudStorageAccount;
            var blobClient = CloudBlobClient;

            Trace.TraceInformation("Get container");
            var driveContainer = blobClient.GetContainerReference(containerName);

            // create blob container (it has to exist before creating the cloud drive)
            try
            {
                driveContainer.CreateIfNotExist();
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Exception when creating container");
                Trace.TraceInformation(e.Message);
                Trace.TraceInformation(e.StackTrace);
            }

            var bermudaBlobUri = blobClient.GetContainerReference(containerName).GetPageBlobReference(blobName).Uri.ToString();
            Trace.TraceInformation(string.Format("Blob uri obtained {0}", bermudaBlobUri));

            // create the cloud drive
            bermudaDrive = storageAccount.CreateCloudDrive(bermudaBlobUri);
            try
            {
                bermudaDrive.CreateIfNotExist(driveSize);
            }
            catch (Exception e)
            {
                // exception is thrown if all is well but the drive already exists
                Trace.TraceInformation("Exception when creating cloud drive. safe to ignore");
                Trace.TraceInformation(e.Message);
                Trace.TraceInformation(e.StackTrace);

            }

            //Trace.TraceInformation("Initialize cache");
            //var localStorage = RoleEnvironment.GetLocalResource(localCachePath);

            //CloudDrive.InitializeCache(localStorage.RootPath.TrimEnd('\\'),
            //    localStorage.MaximumSizeInMegabytes);

            // mount the drive and get the root path of the drive it's mounted as
            if (!waitOnMount)
            {
                try
                {
                    Trace.TraceInformation(string.Format("Trying to mount blob as azure drive on {0}", RoleEnvironment.CurrentRoleInstance.Id));
                    var driveLetter = bermudaDrive.Mount(0, DriveMountOptions.None);
                    Trace.TraceInformation(string.Format("Write lock acquired on azure drive, mounted as {0}, on role instance", driveLetter, RoleEnvironment.CurrentRoleInstance.Id));
                    return driveLetter;
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("could not acquire blob lock.");
                    Trace.TraceWarning(e.Message);
                    Trace.TraceWarning(e.StackTrace);
                    throw;
                }
            }
            else
            {
                string driveLetter;
                Trace.TraceInformation(string.Format("Trying to mount blob as azure drive on {0}",
                    RoleEnvironment.CurrentRoleInstance.Id));
                while (true)
                {
                    try
                    {
                        driveLetter = bermudaDrive.Mount(0,DriveMountOptions.None);
                        Trace.TraceInformation(string.Format("Write lock acquired on azure drive, mounted as {0}, on role instance", driveLetter, RoleEnvironment.CurrentRoleInstance.Id));
                        return driveLetter;
                    }
                    catch { }
                    Thread.Sleep(MountSleep);
                }
            }
        }
    }
}
