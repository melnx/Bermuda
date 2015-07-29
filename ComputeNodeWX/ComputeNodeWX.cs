using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Catalog;
using Bermuda.Interface;
using Bermuda.Constants;
using System.Runtime.Serialization;

namespace ComputeNodeWX
{
    [DataContract]
    public class ComputeNodeWX : ComputeNode
    {
        #region Constructor

        /// <summary>
        /// constructor with the compute node index
        /// </summary>
        /// <param name="index"></param>
        public ComputeNodeWX(Int64 index, Int64 bucket_count, Int64 compute_node_count)
            :base(index, bucket_count, compute_node_count)
        {
            //Catalogs = new List<ICatalog>();
            //Catalogs.Add(InitializeWXCatalog("Weather", ""));
        }


        #endregion


        //protected override IEnumerable<ICatalog> GetCatalogs()
        //{
        //    //throw new NotImplementedException();
        //    return new List<ICatalog> { InitializeWXCatalog("Weather", "") };
        //}

       

        /// <summary>
        /// init an wx catalog
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        public ICatalog InitializeWXCatalog(string Name, string ConnectionString)
        {
            //throw new NotImplementedException();

            //create a catalog
            Catalog catalog = new Catalog(this);
            catalog.CatalogName = Name;
            catalog.ConnectionString = ConnectionString;
            catalog.ConnectionType = ConnectionTypes.FileSystem;
            catalog.CatalogMetadata = new CatalogMetadata(catalog);

            //WeatherDataItems
            TableMetadata weather_metadata = new TableMetadata(catalog.CatalogMetadata);
            weather_metadata.DataType = typeof(WeatherDataItem);
            weather_metadata.Filter = "";
            weather_metadata.MaxSaturationItems = 2000;
            weather_metadata.ModField = "Id";
            weather_metadata.OrderBy = "UpdatedOn";
            weather_metadata.PrimaryKey = "Id";
            //weather_metadata.Query = "Id, OccurredOn, UpdatedOn, Name, Evaluation, UniqueId, Description, CreatedOn, Type, Username, Influence, Followers, KloutScore, ChildCount, IsDisabled FROM Instances with(NOLOCK)";
            weather_metadata.Query ="";
            weather_metadata.ReferenceTable = false;
            weather_metadata.SaturationDeleteField = "";
            weather_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            weather_metadata.SaturationDeleteType = null;
            weather_metadata.SaturationDeleteValue = null;
            weather_metadata.SaturationFrequency = 30 * 1000;
            weather_metadata.SaturationPurgeField = "UpdatedOn";
            weather_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            weather_metadata.SaturationPurgePercent = 5;
            weather_metadata.SaturationPurgeType = typeof(DateTime);
            weather_metadata.SaturationUpdateField = "";
            weather_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            weather_metadata.SaturationUpdateType = null;
            weather_metadata.TableName = "Weather";
            weather_metadata.IsFixedWidth = true;
            weather_metadata.HeaderRowCount = 1;

            catalog.CatalogMetadata.Tables.Add(weather_metadata.TableName, weather_metadata);
            ColumnMetadata col;
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Station",
                ColumnType = typeof(string),
                FieldMapping = "Station",
                Nullable = false,
                ColumnLength = 6,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 0,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Wban",
                ColumnType = typeof(string),
                FieldMapping = "Wban",
                Nullable = false,
                ColumnLength = 5,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 7,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Year",
                ColumnType = typeof(int),
                FieldMapping = "Year",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 14,
                FixedWidthLength = 4
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Month",
                ColumnType = typeof(int),
                FieldMapping = "Month",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 18,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Day",
                ColumnType = typeof(int),
                FieldMapping = "Day",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 20,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Date",
                ColumnType = typeof(DateTime),
                FieldMapping = "Date",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 14,
                FixedWidthLength = 8
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanTemperature",
                ColumnType = typeof(float),
                FieldMapping = "MeanTemperature",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 24,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanTemperatureCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanTemperatureCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 31,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanDewpoint",
                ColumnType = typeof(float),
                FieldMapping = "MeanDewpoint",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 35,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanDewpointCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanDewpointCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 42,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanSealevelPressure",
                ColumnType = typeof(float),
                FieldMapping = "MeanSealevelPressure",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 46,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanSealevelPressureCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanSealevelPressureCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 53,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanStationPressure",
                ColumnType = typeof(float),
                FieldMapping = "MeanStationPressure",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 57,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanStationPressureCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanStationPressureCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 64,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanVisibility",
                ColumnType = typeof(float),
                FieldMapping = "MeanVisibility",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 68,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanVisibilityCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanVisibilityCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 74,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanWindSpeed",
                ColumnType = typeof(float),
                FieldMapping = "MeanWindSpeed",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 78,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MeanWindSpeedCount",
                ColumnType = typeof(int),
                FieldMapping = "MeanWindSpeedCount",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 84,
                FixedWidthLength = 2
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MaximumSustainedWindSpeed",
                ColumnType = typeof(float),
                FieldMapping = "MaximumSustainedWindSpeed",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 88,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MaximumGust",
                ColumnType = typeof(float),
                FieldMapping = "MaximumGust",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 95,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MaximumTemperature",
                ColumnType = typeof(float),
                FieldMapping = "MaximumTemperature",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 102,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MaximumTemperatureFlag",
                ColumnType = typeof(char),
                FieldMapping = "MaximumTemperatureFlag",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 108,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MinimumTemperature",
                ColumnType = typeof(float),
                FieldMapping = "MinimumTemperature",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 6,
                Visible = true,
                FixedWidthStartIndex = 110,
                FixedWidthLength = 6
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "MinimumTemperatureFlag",
                ColumnType = typeof(char),
                FieldMapping = "MinimumTemperatureFlag",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 116,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Precipitation",
                ColumnType = typeof(float),
                FieldMapping = "Precipitation",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 118,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "PrecipitationFlag",
                ColumnType = typeof(char),
                FieldMapping = "PrecipitationFlag",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 123,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "SnowDepth",
                ColumnType = typeof(float),
                FieldMapping = "SnowDepth",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true,
                FixedWidthStartIndex = 125,
                FixedWidthLength = 5
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsFog",
                ColumnType = typeof(bool),
                FieldMapping = "IsFog",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 132,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsRain",
                ColumnType = typeof(bool),
                FieldMapping = "IsRain",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 133,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsSnow",
                ColumnType = typeof(bool),
                FieldMapping = "IsSnow",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 134,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsHail",
                ColumnType = typeof(bool),
                FieldMapping = "IsHail",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 135,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsThunder",
                ColumnType = typeof(bool),
                FieldMapping = "IsThunder",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 136,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "IsTornado",
                ColumnType = typeof(bool),
                FieldMapping = "IsTornado",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
                FixedWidthStartIndex = 137,
                FixedWidthLength = 1
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(weather_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true,
            };
            weather_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            
            return catalog;
        }
    }
}
