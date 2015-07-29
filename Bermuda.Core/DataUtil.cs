using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.ExpressionGeneration;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Bermuda.Interface;

namespace Bermuda.Core
{
    public class DataHelper
    {
        public static bool TryAddToDictionary(object dictionary, object key, object item)
        {
            var dictionaryType = dictionary.GetType();

            var tryAdd = dictionaryType.GetMethod("TryAdd");

            return (bool)tryAdd.Invoke(dictionary, new object[] { key, item });
        }

        public static object AddOrUpdateToDictionary(object dictionary, object key, object item)
        {
            var itemType = item.GetType();
            var keyType = key.GetType();

            var xParam = Expression.Parameter(keyType, "x");
            var yParam = Expression.Parameter(itemType, "y");
            //var rParam = Expression.Parameter(keyType, "r");

            var funcType = typeof(Func<,,>).MakeGenericType(keyType, itemType, itemType);

            var updateValueFactoryExpr = Expression.Lambda
            (
                delegateType: funcType,
                parameters: new ParameterExpression[]{ xParam, yParam },
                body: yParam
            );

            var updateValueFactory = updateValueFactoryExpr.Compile();

            var dictionaryType = dictionary.GetType();

            var tryAdds = dictionaryType.GetMethods().Where(x => x.GetParameters().Length == 3);
            var tryAdd = tryAdds.LastOrDefault();

            return tryAdd.Invoke(dictionary, new object[] { key, item, updateValueFactory });
        }

        public static IEnumerable<MentionTest> GetPremadeMentions()
        {
            

            var sample = new MentionTest[]
            {
                new MentionTest{ OccurredOnTicks = 0, Name = "lol", Description = "happy", Sentiment = 100, Tags = new List<long> {1}, Type = "tweet" },
                new MentionTest{ OccurredOnTicks = 0, Name = "happy", Description = "lol", Sentiment = 50, Tags = new List<long> {1}, Type = "tweet" },
                new MentionTest{ OccurredOnTicks = 0, Name = "lol", Description = "happy", Sentiment = 100, Tags = new List<long> {1}, Type = "tweet" },
                new MentionTest{ OccurredOnTicks = 0, Name = "happy", Description = "lol", Sentiment = 50, Tags = new List<long> {1}, Type = "tweet" },
                new MentionTest{ OccurredOnTicks = 0, Name = "lol", Description = "happy", Sentiment = 100, Tags = new List<long> {1}, Type = "tweet" },
                new MentionTest{ OccurredOnTicks = 1, Name = "meh", Description = "dont care", Sentiment = 0, Tags = new List<long> {2,3}, Type = "status" },
                new MentionTest{ OccurredOnTicks = 1, Name = "dont care", Description = "meh", Sentiment = -50, Tags = new List<long> {2,3}, Type = "status" },
                new MentionTest{ OccurredOnTicks = 1, Name = "meh", Description = "dont care", Sentiment = 0, Tags = new List<long> {2,3}, Type = "status" },
                new MentionTest{ OccurredOnTicks = 1, Name = "dont care", Description = "meh", Sentiment = -50, Tags = new List<long> {2,3}, Type = "status" },
                new MentionTest{ OccurredOnTicks = 1, Name = "meh", Description = "dont care", Sentiment = 0, Tags = new List<long> {2,3}, Type = "status" },
                new MentionTest{ OccurredOnTicks = 2, Name = "sob", Description = "very sad", Sentiment = -100, Tags = new List<long> {4}, Type = "blog" },
                new MentionTest{ OccurredOnTicks = 2, Name = "very sad", Description = "sob", Sentiment = -75, Tags = new List<long> {4}, Type = "blog" },
                new MentionTest{ OccurredOnTicks = 2, Name = "sob", Description = "very sad", Sentiment = -100, Tags = new List<long> {4}, Type = "blog" },
                new MentionTest{ OccurredOnTicks = 2, Name = "very sad", Description = "sob", Sentiment = -75, Tags = new List<long> {4}, Type = "blog" },
                new MentionTest{ OccurredOnTicks = 2, Name = "sob", Description = "very sad", Sentiment = -100, Tags = new List<long> {4}, Type = "blog" },
                new MentionTest{ OccurredOnTicks = 3, Name = "ha ha", Description = "very funny", Sentiment = 100, Tags = new List<long> {5}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 3, Name = "very funny", Description = "ha ha", Sentiment = 75, Tags = new List<long> {5}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 3, Name = "ha ha", Description = "very funny", Sentiment = 100, Tags = new List<long> {5}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 3, Name = "very funny", Description = "ha ha", Sentiment = 75, Tags = new List<long> {5}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 3, Name = "ha ha", Description = "very funny", Sentiment = 100, Tags = new List<long> {5}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 4, Name = "blah", Description = "really bored", Sentiment = -20, Tags = new List<long> {6}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 4, Name = "really bored", Description = "blah", Sentiment = -5, Tags = new List<long> {6}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 4, Name = "blah", Description = "really bored", Sentiment = -20, Tags = new List<long> {6}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 4, Name = "really bored", Description = "blah", Sentiment = -5, Tags = new List<long> {6}, Type = "forum" },
                new MentionTest{ OccurredOnTicks = 4, Name = "blah", Description = "really bored", Sentiment = -20, Tags = new List<long> {6}, Type = "forum" },
            };

            return sample;
        }

        public static DateTime NearestQuarterEnd(DateTime date)
        {
            IEnumerable<DateTime> candidates =
                QuartersInYear(date.Year).Union(QuartersInYear(date.Year - 1));
            return candidates.Where(d => d <= date).OrderBy(d => d).Last();
        }

        static IEnumerable<DateTime> QuartersInYear(int year)
        {
            return new List<DateTime>() {
                new DateTime(year, 3, 31),
                new DateTime(year, 6, 30),
                new DateTime(year, 9, 30),
                new DateTime(year, 12, 31),
            };
        }

        /*
        public static IEnumerable<Tag> GetTagAssocis(int mentionCount)
        {
            var result = new List<Tag>();

            for (int i = 0; i < mentionCount; i++)
            {
                for(int j=0; j<5; j++)
                {
                    result.Add(new Tag { TagId = j, MentionId = i });
                }
            }

            return result;
        }*/

        public static IEnumerable<MentionTest> GetMentions(int count)
        {
            var result = new List<MentionTest>();

            var today = DateTime.Today;

            Random rng = new Random();

            var tagList = new List<long> { 0, 1, 2, 3, 4 };
            var dataSourceList = new List<long> { 0, 1, 2, 3, 4 };
            var themeList = Enumerable.Range(0, 16).SelectMany(x => Enumerable.Repeat(x, x / 5)).Select(x => (long)x).ToList();

            for (int i = 0; i < count; i++)
            {
                var occurred = today.AddHours(rng.Next(0, 10000));

                result.Add(new MentionTest
                {
                    Id = i,
                    OccurredOnTicks = occurred.Ticks,
                    OccurredOn = occurred,
                    Tags = tagList,
                    Datasources = dataSourceList,
                    Name = Guid.NewGuid().ToString(),
                    Description = Guid.NewGuid() + "-" + Guid.NewGuid(),
                    Type = rng.Next(0, 10).ToString(),
                    Themes = themeList
                });
            }

            return result;
        }
    }

}
