using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Interface
{
    public interface ILineProcessor
    {
        bool ProcessColumn(IColumnMetadata ColumnMeta, string data, out object result);
        bool NextLine();
    }
}
