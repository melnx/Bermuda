using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Service.Data
{
    public class TagAssociation : IDataItem
    {
        public int MentionId;
        public int TagId;
        public int Id;
        //public DateTime CreatedOn;
        public bool IsDisabled;
        public DateTime UpdatedOn;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

    public class DatasourceMention : IDataItem
    {
        public long Id;
        public int MentionId;
        public long DatasourceId;
        public double Evaluation;
        //public DateTime CreatedOn;
        public bool IsDisabled;
        public DateTime UpdatedOn;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

    public class ThemeMention : IDataItem
    {
        public long Id;
        public int MentionId;
        public long ThemeId;
        public double Evaluation;
        //public DateTime CreatedOn;
        public bool IsDisabled;
        public DateTime UpdatedOn;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

    public enum SqlDownloadType
    {
        Mention,
        TagAssociation,
        Tag,
        Datasource,
        DatasourceMention,
        Theme,
        ThemeMention
    }
}
