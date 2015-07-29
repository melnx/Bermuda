using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bermuda.BermudaConfig.Storage
{
    public static class CloudStorageUtil
    {
        public const string TempFileName = "DownloadFile.tmp";

        public static bool TempFileExists()
        {
            try
            {
                if (File.Exists(TempFileName))
                    return true;
            }
            catch (Exception) { }
            return false;
        }

        public static void CleanTempFile()
        {
            try
            {
                File.Delete(TempFileName);
            }
            catch (Exception) { }
        }
    }
}
