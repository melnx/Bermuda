using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.IO;

namespace Bermuda.FileSaturator
{
    public class FileSystemFileProcessor : IFileProcessor
    {
        #region Interface Implementations

        public bool OpenFile(object FileObject, ITableMetadata TableMeta)
        {
            try
            {
                reader = new StreamReader((string)FileObject);

                lineCount = -1;
                currentLine = null;
                
                // advance past header rows
                while (TableMeta.HeaderRowCount > (lineCount + 1))
                {
                    lineCount++;
                    currentLine = reader.ReadLine();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                return false;
            }
        }

        public bool NextLine()
        {
            string tmp;
            if ((tmp = reader.ReadLine()) != null)
            {
                currentLine = tmp;
                lineCount++;
                LineProcessor.NextLine();
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetLine()
        {
            return currentLine;
        }


        public List<object> GetFileObjects()
        {
            if (Directory.Exists(FilesRootPath))
            {
                //return Directory.GetFiles(FilesRootPath, "*", SearchOption.AllDirectories).Cast<object>().ToList();
                List<object> temp = Directory.GetFiles(FilesRootPath, "*", SearchOption.AllDirectories).Cast<object>().ToList();
                return temp;
            }
            else
            {
                return null;
            }
               
        }

        public ILineProcessor LineProcessor { get; set; }

        public void Dispose()
        {
            reader.Close();
        }

        # endregion

        #region Variables and Properties

        private StreamReader reader;
        private int lineCount;
        private string currentLine;
        public string FilesRootPath { get; set; }

        #endregion

        #region Constructor

        public FileSystemFileProcessor()
        {
        }

        #endregion
    }
}
