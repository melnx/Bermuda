using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace Bermuda.BermudaConfig.Storage.Local
{
    public class StorageLocal : IStorageAccess
    {
        #region Variables and Properties

        /// <summary>
        /// the storage type
        /// </summary>
        public StorageFactory.StorageType StorageType { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="type"></param>
        public StorageLocal(StorageFactory.StorageType type)
        {
            StorageType = type;
        }

        #endregion

        #region IStorageAccess

        /// <summary>
        /// do the open file dialog using standard windows dialog
        /// </summary>
        /// <param name="InitPath"></param>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool OpenFileDialog(out string PathName, out string FileName)
        {
            PathName = null;
            FileName = null;
            try
            {
                //display the dlg
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open configuration file";
                dlg.Multiselect = false;
                dlg.Filter = "Configuration File|*.Config";
                if (dlg.ShowDialog() != DialogResult.OK)
                    return false;
                
                //check if file exists
                if(!File.Exists(dlg.FileName))
                    return false;

                //get the path and filename
                PathName = Path.GetDirectoryName(dlg.FileName);
                FileName = Path.GetFileName(dlg.FileName);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// do the save file dialog using windows standard dlg
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool SaveFileDialog(out string PathName, out string FileName)
        {
            PathName = null;
            FileName = null;
            try
            {
                //show save dlg
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "Save configuration file";
                dlg.Filter = "Configuration File|*.Config";
                if (dlg.ShowDialog() != DialogResult.OK)
                    return false;

                //get the path and filename
                PathName = Path.GetDirectoryName(dlg.FileName);
                FileName = Path.GetFileName(dlg.FileName);
                
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// read all the text from specified file
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
                //check if exists
                string path = CreatePath(PathName, FileName);
                if (!File.Exists(path))
                    return false;

                //read the file
                Data = File.ReadAllText(path);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }
        
        /// <summary>
        /// save all the text to the specified file
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool SaveFile(string Data, string PathName, string FileName)
        {
            try
            {
                //check if exists
                string path = CreatePath(PathName, FileName);
                //if (!File.Exists(path))
                //    return false;

                //save the file
                File.WriteAllText(path, Data);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// create the path from the path and filename
        /// </summary>
        /// <param name="PathName"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        private string CreatePath(string PathName, string FileName)
        {
            if (string.IsNullOrWhiteSpace(PathName))
                return FileName;
            if (PathName.Last() != '\\')
                PathName += "\\";
            return PathName + FileName;
        }

        #endregion

    }
}
