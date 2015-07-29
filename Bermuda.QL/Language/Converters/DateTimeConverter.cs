using System;
using System.Linq;

namespace Bermuda.QL.Converters
{
    [ConverterType(OutputType=typeof(DateTime))]
    public class DateTimeConverter : ConverterBase
    {
        public override object Convert(string text)
        {
            return ConvertInternal(text);
        }

        private DateTime ConvertInternal(string text)
        {
            text = text.ToLower();

            if (text == "today")
            {
                return DateTime.Today;
            }
            else if (text == "tomorrow")
            {
                return DateTime.Today.AddDays(1);
            }
            else if (text == "yesterday")
            {
                return DateTime.Today.AddDays(-1);
            }

            DateTime result;
            if (!DateTime.TryParse(text, out result))
            {
                double dayResult;
                if (!double.TryParse(text, out dayResult))
                {
                    throw new Exception("No valid value for Date found");
                }
                result = DateTime.Now.AddDays(dayResult);
            }
            return result;
        }
    }

    [ConverterType(OutputType = typeof(DateTime[]), SpecialName="DateTimeRange")]
    public class DateTimeTangeConverter : ConverterBase
    {
        public override object Convert(string text)
        {
            if (text.Contains(".."))
            {
                var parts = text.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);

                var res1 = new DateTime[] { ConvertInternal(parts.First()), ConvertInternal(parts.Last()) };

                return res1;
            }

            var res = ConvertInternal(text);
            return new DateTime[]{ res, res };
        }

        private DateTime ConvertInternal(string text)
        {
            text = text.Trim('"').ToLower();

            if (text == "today")
            {
                return DateTime.Today;
            }
            else if (text == "tomorrow")
            {
                return DateTime.Today.AddDays(1);
            }
            else if (text == "yesterday")
            {
                return DateTime.Today.AddDays(-1);
            }

            DateTime result;
            if (!DateTime.TryParse(text, out result))
            {
                double dayResult;
                if (!double.TryParse(text, out dayResult))
                {
                    throw new Exception("No valid value for Date found");
                }
                result = DateTime.UtcNow.AddDays(dayResult);
            }
            return result;
        }
    }

    [ConverterType(OutputType = typeof(DateTime[]), SpecialName = "DateTimeRangeLocal")]
    public class DateTimeRangeLocalConverter : ConverterBase
    {
        public int HourOffset;

        public override object Convert(string text)
        {
            if (text.Contains(".."))
            {
                var parts = text.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);

                var res1 = new DateTime[] { ConvertInternal(parts.First()), ConvertInternal(parts.Last()) };

                return res1;
            }

            var res = ConvertInternal(text);
            return new DateTime[] { res, res };
        }

        private DateTime ConvertInternal(string text)
        {
            text = text.Trim('"').ToLower();

            if (text == "today")
            {
                return DateTime.Today;
            }
            else if (text == "tomorrow")
            {
                return DateTime.Today.AddDays(1);
            }
            else if (text == "yesterday")
            {
                return DateTime.Today.AddDays(-1);
            }

            DateTime result;
            if (!DateTime.TryParse(text, out result))
            {
                double dayResult;
                if (!double.TryParse(text, out dayResult))
                {
                    throw new Exception("No valid value for Date found");
                }
                result = DateTime.UtcNow.AddDays(dayResult).AddHours(-HourOffset);
            }
            return result.AddHours(-HourOffset);
        }
    }
}
