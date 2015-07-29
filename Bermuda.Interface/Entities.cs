using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.ServiceModel;

namespace Bermuda.Interface
{
    public static class BermudaServiceUtil
    {
        public static void AdjustForBermuda(this NetTcpBinding binding)
        {
            binding.ReaderQuotas.MaxBytesPerRead *= 100;
            binding.ReaderQuotas.MaxStringContentLength *= 100;
            binding.ReaderQuotas.MaxArrayLength *= 100;
            binding.MaxReceivedMessageSize *= 100;
            binding.MaxConnections = 100;
            binding.CloseTimeout = TimeSpan.FromSeconds( binding.CloseTimeout.TotalSeconds * 10);
            binding.OpenTimeout = TimeSpan.FromSeconds(binding.OpenTimeout.TotalSeconds * 10);
            binding.ReceiveTimeout = TimeSpan.FromSeconds(binding.ReceiveTimeout.TotalSeconds * 10);
            binding.SendTimeout = TimeSpan.FromSeconds(binding.SendTimeout.TotalSeconds * 10);
        }
    }

    public class WikipediaHourlyPageStats : IDataItem
    {
        public WikipediaHourlyPageStats(DateTime recordedOn,
                                        string projectCode,
                                        string pageName,
                                        int pageViews,
                                        long pageSizeKB)
        {
            this.RecordedOn = recordedOn;
            this.ProjectCode = projectCode;
            this.PageName = pageName;
            this.PageViews = pageViews;
            this.PageSizeKB = pageSizeKB;

            var hashText = this.RecordedOn.ToString() + this.ProjectCode + this.PageName;
            var s1 = hashText.Substring(0, hashText.Length / 2);
            var s2 = hashText.Substring(hashText.Length / 2);
            var x = ((long)s1.GetHashCode()) << 0x20 | s2.GetHashCode();
            this.primaryKey = x;
        }

        public DateTime RecordedOn;
        public string ProjectCode;
        public string PageName;
        public int PageViews;
        public long PageSizeKB;

        long primaryKey;
        public long PrimaryKey
        {
            get { return primaryKey; }
        }
    }

    public struct ItemMetadata<TElement>
    {
        public TElement Item;
        public long Id;
        public string Text;
    }

    public class MentionMetadata2
    {
        public MentionTest Mention;
        public long Id;
        public MentionMetadata2 Child;
    }

    public struct EnumMetadata<EnumType>
    {
        public EnumType Enum;
        public long Value;
    }

    public struct GroupMetadata
    {
        public IGrouping<long, MentionTest> Group;
        public IEnumerable<MentionTest> Enum;
        public long Value;
    }

    public class MentionTest
    {
        public long Id;
        public string Name;
        public string Description;
        public string Type;
        public List<long> Tags;
        public List<long> Datasources;
        public List<string> Ngrams;
        public List<long> Themes;
        public double Sentiment;
        public long Influence;
        public bool IsDisabled;

        public long OccurredOnTicks;
        public long CreatedOnTicks;
        public long UpdatedOnTicks;

        public DateTime OccurredOn;
        public DateTime CreatedOn;
        public DateTime UpdatedOn;
        public string Guid;

        public string Author;
        public long Followers;
        public long Klout;
        public long Comments;

        double? NulNum;
        bool IsLol;
    }

    public class AssociatedTypesAttribute : Attribute
    {
        public AssociatedTypesAttribute(Type metadataType, Type groupingType)
        {
            MetadataType = metadataType;
            GroupingType = groupingType;
        }

        public Type MetadataType;
        public Type GroupingType;
    }

    public class Datapoint
    {
        public long Id;
        public long Id2;
        public string Text;
        public string Text2;
        public double Value;
        public long Count;
        public long Timestamp;
        public long _Count;
        //public bool IsCount;
        public long CountValue
        {
            get { return Count; }
            set { Value = Count = value; }
        }
    }

    public class BermudaNodeStatistic
    {
        public string NodeId;
        public string Notes;
        public long TotalItems;
        public long FilteredItems;
        public long ReducedItems;
        public TimeSpan LinqExecutionTime;
        public DateTime Initiated;
        public DateTime Completed;
        public BermudaNodeStatistic[] ChildNodes;
        public string Error;
        public TimeSpan OperationTime;

        public BermudaNodeStatistic Clone()
        {
            return new BermudaNodeStatistic
            {
                NodeId = NodeId,
                Notes = Notes,
                TotalItems = TotalItems,
                FilteredItems = FilteredItems,
                ReducedItems = ReducedItems,
                Initiated = Initiated,
                Completed = Completed,
                LinqExecutionTime = LinqExecutionTime,
                OperationTime = OperationTime,
                Error = Error,
                ChildNodes = ChildNodes == null ? null : ChildNodes.Select(x => x.Clone()).ToArray()
            };
        }
    }


    //public struct Tag : IDataItem
    //{
    //    public int Id;
    //    public string Name;
    //    public DateTime CreatedOn;
    //    public bool IsDisabled;

    //    public long PrimaryKey
    //    {
    //        get { return Id; }
    //    }
    //}

    //public struct Datasource : IDataItem
    //{
    //    public long Id;
    //    public string Name;
    //    public DateTime CreatedOn;
    //    public int Type;
    //    public string Value;
    //    public bool IsDisabled;

    //    public long PrimaryKey
    //    {
    //        get { return Id; }
    //    }
    //}

    //public struct Theme : IDataItem
    //{
    //    public long Id;
    //    public string Text;

    //    public long PrimaryKey
    //    {
    //        get { return Id; }
    //    }
    //}

    //public struct Mention
    //{
    //    //public ObjectId _id;
    //    public string Name;
    //    public string Description;
    //    public TagAssociation[] Tags;
    //    public double Sentiment;
    //    public double Influence;
    //    public DateTime OccurredOn;
    //    public DateTime DayPrecision;
    //    public DateTime CreatedOn;
    //}

    //public struct MentionCollection
    //{
    //    public Mention[] Mentions;
    //}

    [DataContract]
    public class BermudaCursor
    {
        [DataMember]
        public string CursorId;

        [DataMember]
        public string Error;
    }

    [DataContract]
    public class BermudaResult
    {
        [DataMember]
        public string CacheKey;

        [DataMember]
        public string DataType;

        [DataMember]
        public BermudaNodeStatistic Metadata;

        [DataMember]
        public string Error;

        [DataMember]
        public long CreatedOn;

        [DataMember]
        public string Data;

        public object OriginalData;
    }

    [DataContract]
    public class TableMetadataResult
    {
        [DataMember]
        public string Catalog;
        [DataMember]
        public string Table;
    }

    [DataContract]
    public class ColumnMetadataResult
    {
        [DataMember]
        public string Catalog;
        [DataMember]
        public string Table;
        [DataMember]
        public string Column;
        [DataMember]
        public string DataType;
        [DataMember]
        public int ColumnLength;
        [DataMember]
        public int ColumnPrecision;
        [DataMember]
        public bool Nullable;
        [DataMember]
        public bool Visible;
        [DataMember]
        public int OrdinalPosition;
    }

    public class BermudaTextSearchMemberAttribute : Attribute { }
}
