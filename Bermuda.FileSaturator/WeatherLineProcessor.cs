using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;

namespace Bermuda.FileSaturator
{
    public class WeatherLineProcessor : ILineProcessor
    {
        public bool ProcessColumn(IColumnMetadata ColumnMeta, string data, out object result)
        {
            try
            {
                // deal with nullable columns
                if (nullableValues.ContainsKey(ColumnMeta.ColumnName) && nullableValues[ColumnMeta.ColumnName].Equals(data))
                {
                    result = null;
                }
                else if (ColumnMeta.ColumnName.Equals("Id"))
                {
                    var epochDays = (int)((DateTime)cachedColumns["Date"]).Subtract(new DateTime(1929, 1, 1)).TotalDays;
                    result = long.Parse(epochDays + (string)cachedColumns["Station"] + (string)cachedColumns["Wban"]);
                }
                else if (ColumnMeta.ColumnType.Equals(typeof(DateTime)))
                {
                    result = new DateTime((int)cachedColumns["Year"],
                                          (int)cachedColumns["Month"],
                                          (int)cachedColumns["Day"]);
                }
                else if (ColumnMeta.ColumnType.Equals(typeof(bool)))
                {
                    result = data.Equals("1");
                }
                else
                {
                    result = Convert.ChangeType(data, ColumnMeta.ColumnType);
                }

                cachedColumns[ColumnMeta.ColumnName] = result;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                result = null;
                return false;
            }
        }

        public bool NextLine()
        {
            cachedColumns = new Dictionary<string, object>();
            return true;
        }

        private IDictionary<string, object> cachedColumns;

        private IDictionary<string, string> nullableValues = new Dictionary<string, string>() 
        {
            {"Wban", "99999" },
            {"MeanTemperature", "9999.9"},
            {"MeanDewpoint", "9999.9"},
            {"MeanSealevelPressure", "9999.9"},
            {"MeanStationPressure", "9999.9"},
            {"MeanVisibility", "999.9"},
            {"MeanWindSpeed", "999.9"},
            {"MaximumSustainedWindSpeed", "999.9"},
            {"MaximumGust", "999.9"},
            {"MaximumTemperature", "9999.9"},
            {"MinimumTemperature", "9999.9"},
            {"Precipitation", "99.99"},
            {"SnowDepth", "999.9"}
        };
    }
}
