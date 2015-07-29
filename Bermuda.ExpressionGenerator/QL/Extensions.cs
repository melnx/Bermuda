using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.ExpressionGeneration
{
    public static class QLExtensions
    {
        //public static IEnumerable<Mention> Where(this IEnumerable<Mention> mentions, string query)
        //{
        //    return mentions.Where(EvoQLBuilder.GetFilterLambda(query, typeof(Mention)).Compile());
        //}

        //public static IEnumerable<Datapoint> Reduce(this IEnumerable<Mention> mentions, string query)
        //{
        //    return EvoQLBuilder.GetReduceExpression(query, typeof(Mention))(mentions);
            
        //}

        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
        {
            for (int i = 0; i < (source.Count / size) + (source.Count % size > 0 ? 1 : 0); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
        {
            int i = 0;
            var splits = from item in list
                         group item by i++ % parts into part
                         select part.AsEnumerable();
            return splits;
        }
    }
}
