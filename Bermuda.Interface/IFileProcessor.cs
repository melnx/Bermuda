using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.Interface
{
    public interface IFileProcessor : IDisposable
    {
        ILineProcessor LineProcessor { get; set; }

        bool OpenFile(object FileObject, ITableMetadata TableMeta);
        bool NextLine();
        string GetLine();
        List<object> GetFileObjects();
    }
}
