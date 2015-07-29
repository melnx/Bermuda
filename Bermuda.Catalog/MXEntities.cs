using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class Mention : IDataItem
    {
        public long Id;
        public string Name;
        public string Description;
        public string Type;
        public List<Tuple<List<long>, long>> Tags;
        public List<Tuple<List<long>, long>> Datasources;
        public List<string> Ngrams;
        public List<Tuple<List<long>, long>> Themes;
        public double Sentiment;
        public long Influence;
        public bool IsDisabled;

        public long OccurredOnTicks;
        public long CreatedOnTicks;
        public long UpdatedOnTicks;

        //public DateTime OccurredOn;
        public DateTime Date;
        public DateTime CreatedOn;
        public DateTime UpdatedOn;
        public string Guid;

        public string Author;
        public long Followers;
        public long Klout;
        public long Comments;

        public long PrimaryKey
        {
            get
            {
                return Id;
            }
        }

        public Mention()
        {
            //Tags = new List<long>();
            //Datasources = new List<long>();
            //Themes = new List<long>();
        }
    }

    public class UDPTestDataItems : IDataItem
    {
        public long Id;
        public DateTime Date;
        public string Log;

        public long PrimaryKey
        {
            get
            {
                return Id;
            }
        }
    }

    public class WeatherDataItem : IDataItem
    {
        public string Station;
        public string Wban;
        public int Year;
        public int Month;
        public int Day;
        public DateTime Date;
        public float? MeanTemperature;
        public int MeanTemperatureCount;
        public float? MeanDewpoint;
        public int MeanDewpointCount;
        public float? MeanSealevelPressure;
        public int MeanSealevelPressureCount;
        public float? MeanStationPressure;
        public int MeanStationPressureCount;
        public float? MeanVisibility;
        public int MeanVisibilityCount;
        public float? MeanWindSpeed;
        public int MeanWindSpeedCount;
        public float? MaximumSustainedWindSpeed;
        public float? MaximumGust;
        public float? MaximumTemperature;
        public char MaximumTemperatureFlag;
        public float? MinimumTemperature;
        public char MinimumTemperatureFlag;
        public float? Precipitation;
        public char PrecipitationFlag;
        public float? SnowDepth;
        public bool IsFog;
        public bool IsRain;
        public bool IsSnow;
        public bool IsHail;
        public bool IsThunder;
        public bool IsTornado;
        public long Id;

        public long PrimaryKey
        {
            get
            {
                return Id;
            }
        }
    }

    public struct Tag : IDataItem
    {
        public int Id;
        public string Name;
        public DateTime CreatedOn;
        public bool IsDisabled;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

    public struct Datasource : IDataItem
    {
        public long Id;
        public string Name;
        public DateTime CreatedOn;
        public int Type;
        public string Value;
        public bool IsDisabled;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

    public struct Theme : IDataItem
    {
        public long Id;
        public string Text;

        public long PrimaryKey
        {
            get { return Id; }
        }
    }

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
}
