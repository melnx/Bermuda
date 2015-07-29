using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public static class UtilityExtensions
    {
        static Dictionary<string, TimeZoneInfo> timezoneLookup = new Dictionary<string, TimeZoneInfo>();
        static TimeZoneInfo GetTimezone(string name)
        {
            TimeZoneInfo info = null;

            if (timezoneLookup.TryGetValue(name, out info))
            {
                return info;
            }

            info = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.StandardName == name);

            if (info == null) throw new BermudaExpressionGenerationException("Unknown timezone: " + info);


            timezoneLookup[name] = info;

            return info;
        }

        public static bool ContainsCaseInsensitive(this string str, string str2)
        {
            return str == null ? false : str.IndexOf(str2, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static long Interval(DateTime occurred, string interval, int hourOffset)
        {
            if (interval == null) return occurred.Ticks;

            //var tz = GetTimezone(timezone);
            //occurred = TimeZoneInfo.ConvertTime( occurred, tz ); //occurred.AddHours(hourOffset);

            if (occurred != DateTime.MinValue && hourOffset != 0) occurred = occurred.AddHours(hourOffset);

            switch ( interval.ToLower() )
            {
                case "minute": return new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, occurred.Minute, 0).Ticks;
                case "quarterhour": return new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, occurred.Minute - occurred.Minute % 15, 0).Ticks;
                case "day": return occurred.Date.Ticks;
                case "hour": return new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, 0, 0).Ticks;
                case "week": return occurred.Date.AddDays(-(int)occurred.DayOfWeek).Ticks;
                case "month": return new DateTime(occurred.Year, occurred.Month, 1).Ticks;
                case "quarter": return new DateTime(occurred.Year, (((occurred.Month - 1) / 3) * 3 + 1), 1).Ticks;
                case "year": return new DateTime(occurred.Year, 1, 1).Ticks;
            }

            throw new Exception("Unknown DateTime Interval: " + interval);
        }

        public static long Bucket(long num, long size)
        {
            return num - num % size;
        }

        public static long DatePart(DateTime occurred, string part, int hourOffset)
        {
            if (part == null) return occurred.Ticks;

            //var tz = GetTimezone(timezone);
            //occurred = TimeZoneInfo.ConvertTime( occurred, tz ); //occurred.AddHours(hourOffset);

            if (occurred != DateTime.MinValue && hourOffset != 0) occurred = occurred.AddHours(hourOffset);

            switch ( (part as string).ToLower() )
            {
                case "minute": return occurred.Minute;
                case "quarterhour": return occurred.Minute/15 + 1;
                case "day": return occurred.Day;
                case "week": return 0;
                case "hour":  return occurred.Hour;
                case "month": return occurred.Month;
                case "quarter": return occurred.Month/3 + 1;
                case "year": return occurred.Year;
            }

            throw new Exception("Unknown DatePart: " + part);
        }

        #region math canonical functions
        public static long Abs(long num)
        {
            return Math.Abs(num);
        }

        public static double Abs(double num)
        {
            return Math.Abs(num);
        }

        public static double Ceiling(double num)
        {
            return Math.Ceiling(num);
        }

        public static double Power(double num, double exp)
        {
            return Math.Pow(num, exp);
        }

        public static double Round(double num)
        {
            return Math.Round(num);
        }

        public static double Round(double num, int digits)
        {
            return Math.Round(num, digits);
        }

        public static double Truncate(double num, int digits)
        {
            decimal stepper = (decimal)(Math.Pow(10.0, (double)digits));
            int temp = (int)(stepper * digits);
            return (double)(temp / stepper);
        }

        public static long Quarter(DateTime date)
        {
            return date.Month / 3;
        }

        public static DateTime TimestampAdd(string interval, int count, DateTime date)
        {
            switch (interval.ToLower())
            {
                case "sql_tsi_month": return date.AddMonths(count);
            }

            throw new BermudaExpressionGenerationException("Invalid interval: " + interval);
        }

        public static string Concat(string string1, string string2)
        {
            return string1 + string2;
        }

        public static bool Contains(string string1, string string2)
        {
            return string1 != null && string1.Contains(string2);
        }

        public static bool EndsWith(string string1, string string2)
        {
            return string1 != null && string1.EndsWith(string2);
        }

        public static int IndexOf(string string1, string string2)
        {
            if (string1 == null) return -1;
            return string1.IndexOf(string2);
        }

        public static string Left(string string1, int left)
        {
            if (string1 == null) return null;
            if (left > string1.Length) return string1;
            return string1.Substring(0, left);
        }

        public static string LTrim(string str)
        {
            return str.TrimStart(' ');
        }

        public static string Replace(string string1, string string2, string string3)
        {
            if (string1 == null) return null;
            return string1.Replace(string2, string3);
        }

        public static string Reverse(string string1)
        {
            throw new Exception("Reverse not supported");
        }

        public static string Right(string string1, int length)
        {
            if (string1 == null) return null;
            if (length > string1.Length) return string1;

            return string1.Substring(string1.Length - length, string1.Length - string1.Length - length);
        }

        public static string RTrim(string string1)
        {
            if (string1 == null) return null;
            return string1.TrimEnd(' ');
        }

        public static string Substring(string string1, int start, int length)
        {
            if (string1 == null) return null;
            return string1.Substring(start, length);
        }

        public static bool StartsWith(string string1, string string2)
        {
            return string1 != null && string1.StartsWith(string2);
        }

        public static string ToLower(string string1)
        {
            if (string1 == null) return null;
            return string1.ToLower();
        }

        public static string ToUpper(string string1)
        {
            if (string1 == null) return null;
            return string1.ToUpper();
        }

        public static string Trim(string string1)
        {
            if (string1 == null) return null;
            return string1.Trim();
        }

        public static DateTime AddNanoseconds(DateTime datetime, long num)
        {
            return datetime.AddMilliseconds(num * 1000000);
        }

        public static TimeSpan AddNanoseconds(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromMilliseconds(num * 1000000));
        }

        public static DateTime AddMicroseconds(DateTime datetime, long num)
        {
            return datetime.AddMilliseconds(num * 1000);
        }

        public static TimeSpan AddMicroseconds(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromMilliseconds(num * 1000));
        }

        public static DateTime AddMilliseconds(DateTime datetime, long num)
        {
            return datetime.AddMilliseconds(num * 1000);
        }

        public static TimeSpan AddMilliseconds(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromMilliseconds(num * 1000));
        }

        public static DateTime AddSeconds(DateTime datetime, long num)
        {
            return datetime.AddSeconds(num);
        }

        public static TimeSpan AddSeconds(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromSeconds(num));
        }


        public static DateTime AddMinutes(DateTime datetime, long num)
        {
            return datetime.AddMinutes(num);
        }

        public static TimeSpan AddMinutes(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromMinutes(num));
        }


        public static DateTime AddHours(DateTime datetime, long num)
        {
            return datetime.AddHours(num);
        }

        public static TimeSpan AddHours(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromHours(num));
        }

        public static DateTime AddDays(DateTime datetime, long num)
        {
            return datetime.AddDays(num);
        }

        public static TimeSpan AddDays(TimeSpan span, long num)
        {
            return span.Add(TimeSpan.FromDays(num));
        }

        public static DateTime AddMonths(DateTime datetime, int num)
        {
            return datetime.AddMonths(num);
        }

        public static TimeSpan AddMonths(TimeSpan span, int num)
        {
            throw new NotImplementedException("Add months not supported for timespan");
        }

        public static DateTime AddYears(DateTime datetime, int num)
        {
            return datetime.AddYears(num);
        }

        public static TimeSpan AddYears(TimeSpan span, int num)
        {
            throw new NotImplementedException("Add months not supported for timespan");
        }

        public static DateTime CreateDateTime(int year, int month, int day, int hour, int minute, int second)
        {
            return new DateTime(year, month, day, hour, minute, second);
        }

        public static TimeSpan CreateDateTimeOffset(int year, int month, int day, int hour, int minute, int second, int tzoffset)
        {
            throw new NotImplementedException("create datetimeoffset not supported");
        }

        public static DateTime CreateTime(int hour, int minute, int second)
        {
            throw new Exception("Create time not supported");
        }

        public static DateTime CurrentDateTime()
        {
            return DateTime.Now;
        }

        public static TimeSpan CurrentDateTimeOffset()
        {
            return TimeSpan.FromTicks( DateTime.Now.Ticks );
        }

        public static DateTime CurrentUtcDateTime()
        {
            return DateTime.UtcNow;
        }

        public static DateTime Day(DateTime date)
        {
            return date;
        }

        public static TimeSpan Day(TimeSpan timespan)
        {
            throw new NotImplementedException("Day not supported for timespan");
        }

        public static long DayOfYear(DateTime date)
        {
            return date.DayOfYear;
        }

        public static long DiffNanoseconds(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalMilliseconds * 1000000;
        }

        public static long DiffMilliseconds(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalMilliseconds;
        }

        public static long DiffMicroseconds(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalMilliseconds * 1000;
        }

        public static long DiffSeconds(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalSeconds;
        }


        public static long DiffMinutes(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalMinutes;
        }

        public static long DiffHours(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalHours;
        }

        public static long DiffDays(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalDays;
        }


        public static long DiffMonths(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalDays / 30;
        }

        public static long DiffYears(DateTime date1, DateTime date2)
        {
            return (long)(date1 - date2).TotalDays / 365;
        }

        public static long GetTotalOffsetMinutes(TimeSpan span)
        {
            return (long)span.TotalMinutes;
        }

        public static long Hour(DateTime date)
        {
            return date.Hour;
        }

        public static long Millisecond(DateTime date)
        {
            return date.Millisecond;
        }

        public static long Minute(DateTime date)
        {
            return date.Minute;
        }

        public static long Month(DateTime date)
        {
            return date.Month;
        }

        public static long Second(DateTime date)
        {
            return date.Second;
        }

        public static DateTime TruncateTime(DateTime date)
        {
            return date.Date;
        }

        public static long Year(DateTime date)
        {
            return date.Year;
        }

        public static DateTime TimeStampAdd(string unit, int num, DateTime date)
        {
            unit = unit.ToUpper();

            switch (unit)
            {
                case "SQL_TSI_MONTH": return date.AddMonths(num);
            }

            throw new Exception("Unknown unit:" + unit);
        }

        #endregion
    }
}
