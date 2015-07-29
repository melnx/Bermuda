using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using ExpressionSerialization;
using System.Linq.Expressions;
using System.Xml.Linq;
using Bermuda.Entities;
using System.IO;
using System.Runtime.InteropServices;

namespace Bermuda
{
    class Program
    {
        static void Main(string[] args) { }

        /*static MapReducerConnection Connection;

        static readonly int workerCount = 150;
        static readonly int shardCount = 5;
        static readonly int mentionsToAdd = 100000;

        static void Main(string[] args)
        {
            for (int i = 0; i < workerCount; i++)
            {
                AppFabricInterface.Instance.SpinUpMapReducer();
            }

            for (int i = 0; i < shardCount; i++)
            {
                AppFabricInterface.Instance.SpinUpShard();
            }

            //use the first server for testing (but you can use any of them)
            Connection = new MapReducerConnection { Endpoint = "mr0.evoapp.com" };

            Tags = new Tag[tagNames.Count];
            for (int j = 0; j < tagNames.Count; j++)
            {
                Tags[j] = new Tag { Name = tagNames[j], Id = j+1 };
            }

            var generator = new EvoApp.TestDataGenerator.SimpleDataGenerator(100, mentionsToAdd, 5000, Tags);
            generator.Run(ProcessCache);
            Finished();
        }

        static void ProcessCache(List<Mention> mentions)
        {
            Connection.InsertMentions(domains[rng.Next(domains.Length)], mentions.ToArray());
        }*/

        /*
        static void Finished()
        {
            //test caching
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine("Iteration:" + i);
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //find stuff that contains a string
                var data = Connection.GetMentions
                (
                    domain: "google",
                    query: x => x.Name.Contains("fantasia"),
                    minDate: DateTime.MinValue,
                    maxDate: DateTime.MaxValue
                );

                //print out mentions
                //data.ToList().ForEach(x => Console.WriteLine(x.Name));

                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Reset();
                sw.Start();
                
                //get a day by day timeseries of count for mentions that contain a string
                var datapoints = Connection.GetDatapoints
                (
                    domain: "google",
                    query: x => x.CreatedOn.Month > 8 && x.Description.Contains("dog"),
                    mapreduce: x => x.GroupBy(y => y.DayPrecision).Select(y => new Datapoint { Value = y.Count(), Timestamp = y.Key }),
                    combine: x => x.Sum(y => y.Value),
                    minDate: DateTime.MinValue,
                    maxDate: DateTime.MaxValue
                );

                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Reset();
                sw.Start();

                //print out the datapoints
                //datapoints.OrderBy(x => x.Timestamp).ToList().ForEach(x => Console.WriteLine(x.Timestamp + " : " + x.Value));


                //get a day by day timeseries of average of sentiment of tags for things tagged "dog"
                var datapoints2 = Connection.GetDatapoints
                (
                    domain: "google",
                    query: x => x.Tags.Any(y => y.TagId == 1),
                    mapreduce: x => x.SelectMany(y => y.Tags).GroupBy(y => y.TagId).Select(y => new Datapoint { EntityId = y.Key, Value = y.Sum(z => z.Sentiment), Count = y.Count() }),
                    combine: x => x.Sum(y => y.Value) / x.Sum(y => y.Count),
                    minDate: DateTime.MinValue,
                    maxDate: DateTime.MaxValue
                );

                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());

                //do tag name lookup for the datapoints
                datapoints2.ToList().ForEach(x => x.Text = Tags.First(y => y.Id == x.EntityId).Name);

                //print out the datapoints
                //datapoints2.ToList().ForEach(x => Console.WriteLine(x.Text + " : " + x.Value));
            }

            List<double> stds = new List<double>();
            List<double> domainChunkShardCounts = new List<double>();
            int maxChunkRank = 0;

            foreach (var kv in AppFabricInterface.Instance.Shards)
            {
                var shard = kv.Value as InMemoryShard;
                var chunks = shard.GetChunks();
                maxChunkRank = Math.Max(chunks.Max(x => x.Rank), maxChunkRank);
                var std = CalculateStdDev(chunks.Select(x => (double)x.Rank));
                stds.Add(std);
                domainChunkShardCounts.Add(chunks.GroupBy(x => x.Color).Select(x => x.Count()).Average());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((kv.Key + ":" + (int)std).PadRight(25) + "\t");
                foreach (var chunk in chunks)
                {
                    Console.ForegroundColor = chunk.Color + 1;
                    Console.Write( (chunk.GetData().Count() + ":" + chunk.Rank).PadRight(10) + "\t");
                }
                Console.WriteLine();
            }

            var perfectDistribution = new double[AppFabricInterface.Instance.maxChunksPerShard];
            var stepSize = (maxChunkRank / (AppFabricInterface.Instance.maxChunksPerShard - 1));
            for (int i = 0; i < AppFabricInterface.Instance.maxChunksPerShard; i++) perfectDistribution[i] = i * stepSize;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Date Distribution : " + (int)stds.Average() + "/" + (int)CalculateStdDev(perfectDistribution));

            //chunk for domain per shard ration
            var mentionsPerDomain = ((double)mentionsToAdd / (double)domains.Length);
            var chunkRequired = mentionsPerDomain / (double)AppFabricInterface.Instance.maxMentionsPerChunk;
            var perfectChunkShardRatio = chunkRequired / (double)AppFabricInterface.Instance.Shards.Count();
            Console.WriteLine("Domain Chunks/Shard Distribution : " + domainChunkShardCounts.Average() + "/" + perfectChunkShardRatio);


            Console.Read();
        }*/

        private static double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        static Tag[] Tags;

        static Random rng = new Random();

        static string[] domains = new string[]
        {
            "ibm",
            "evoapp",
            "google",
            "microsoft",
            "facebook"
        };

        static List<string> tagNames = new List<string>() //50 items
        {
            "apple",
            "ibm",
            "scope",
            "puppy",
            "hamburger",
            "salad",
            "weiner",
            "stocks",
            "dog",
            "acura",
            "pontiac",
            "salami",
            "credit cards",
            "clocks",
            "models",
            "argentinian women",
            "soccer",
            "space",
            "picasso",
            "lump",
            "oil painting",
            "mondrian",
            "evoapp",
            "hot tubs",
            "pomegranate",
            "toast",
            "jackass",
            "keyboards",
            "florence",
            "flouride",
            "ham sandwich",
            "bojangles",
            "british parliament",
            "bondage",
            "9/11",
            "what not to wear",
            "laser printers",
            "shark tank",
            "beck",
            "shower",
            "british parliament",
            "eyes of the world",
            "who wants to be a millionaire",
            "arabian nights",
            "waffle house and assorted places",
            "dogs are not cool",
            "macbook pro",
            "mac mini",
            "hazelnut coffee with cream"
        };
    }
}
