using System;
using System.Net;
using System.Linq;

namespace Bermuda.ExpressionGeneration.Converters
{
    [ConverterType(OutputType = typeof(double[]), SpecialName = "Sentiment")]
    public class SentimentConverter : ConverterBase
    {
        public override object Convert(string text)
        {
            if (text.Contains(".."))
            {
                var parts = text.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);

                return new double[] { ConvertInternal(parts.First())[0], ConvertInternal(parts.Last())[1] };
            }

            return ConvertInternal(text);
        }

        private double[] ConvertInternal(string text)
        {
            switch (text.Trim('"').ToLower())
            {
                case "excellent":
                    return new double[] { 50, 100 };
                case "positive":
                    return new double[] { 20, 50 };
                case "neutral":
                    return new double[] { -20, 20 };
                case "negative":
                    return new double[] { -50, -20 };
                case "horrible":
                    return new double[] { -100, -50 };
            }
            double parsed;
            if (!double.TryParse(text, out parsed))
            {
                throw new Exception("Invalid sentiment, only 'Excellent', 'Positive', 'Neutral', 'Negative', 'Horrible' and Numbers between -100 and 100 are allowed");
            }
            return new double[] { parsed, parsed };
        }
    }

    [ConverterType(OutputType = typeof(double[]), SpecialName = "NumberRange")]
    public class NumberRangeConverter : ConverterBase
    {
        public override object Convert(string text)
        {
            if (text.Contains(".."))
            {
                var parts = text.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);

                return new double[] { ConvertInternal(parts.First())[0], ConvertInternal(parts.Last())[1] };
            }

            return ConvertInternal(text);
        }

        private double[] ConvertInternal(string text)
        {
            double parsed;
            if (!double.TryParse(text, out parsed))
            {
                throw new Exception("Invalid number range, only Numbers are allowed");
            }
            return new double[] { parsed, parsed };
        }
    }
}
