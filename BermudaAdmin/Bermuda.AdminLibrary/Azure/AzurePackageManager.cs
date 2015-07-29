using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.AdminLibrary.Interfaces;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Bermuda.AdminLibrary.Models;

namespace Bermuda.AdminLibrary.Azure
{
    public class AzurePackageManager
    {
        public bool UploadPackageFiles(string localPackagePath, string localConfigPath, string packageName, string storageAccount)
        {
            bool packageUploaded = false;

            try
            {
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in AzurePackageManager.DeletePackage(string packageName)  Error: {0}", ex.Message));

                packageUploaded = false;
            }

            return packageUploaded;
        }

        public bool DeletePackage(string packageName)
        {
            bool packageDeleted = false;

            try
            {
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in AzurePackageManager.DeletePackage(string packageName)  Error: {0}", ex.Message));

                packageDeleted = false;
            }

            return packageDeleted;
        }

        public string ChangeDeploymentInstanceCount(int newCount, string configuration)
        {
            string newConfiguration = string.Empty;

            try
            {
                string instanceCountSegment = configuration.Substring(configuration.IndexOf("<Instances count=\""));

                string instanceCountPhrase = instanceCountSegment.Substring(0, instanceCountSegment.IndexOf(">") + 1);
        
                string newConfigurationPhrase = string.Format("<Instances count=\"{0}\" />", newCount.ToString());

                newConfiguration = configuration.Replace(instanceCountPhrase, newConfigurationPhrase);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in ChangeDeploymentInstanceCount()  Error: {0}", ex.Message));

                newConfiguration = string.Empty;
            }

            return newConfiguration;
        }

        public List<AzurePackage> ListPackages()
        {
            List<AzurePackage> packageList = null;

            try
            {
            }
            catch (System.Exception ex)
            {
                Logger.Write(string.Format("Error in AzurePackageManager.ListPackages()  Error: {0}", ex.Message));

                packageList = null;
            }

            return packageList;
        }
    }
}
