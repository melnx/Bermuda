using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EvoApp.TestDataGenerator.Sources;
using System.Diagnostics;
using EvoApp.TestDataGenerator;
using Bermuda;
using Bermuda.Entities;
using Bermuda.Entities.Thrift;
using System.Security.Cryptography;
using Bermuda.Entities.BSON;

namespace EvoApp.TestDataGenerator
{
    public class SimpleDataGeneratorBSON
    {
        readonly int CacheSize;
        readonly int TotalItems;

        readonly TextSource ShortTextSource = new TextSource(20, 140);
        readonly WordSource WordSource = new WordSource();
        readonly TextSource TextSource = new TextSource(100, 1024);
        readonly RandomDateTimeSource BeforeTodaySource = new RandomDateTimeSource(DateTime.Today.AddMonths(-6), DateTime.Now);
        //readonly List<List<string>> ReusableLabelGroups = new List<List<string>>();
        readonly Tag[] Tags;
        readonly string[] Guids;
        readonly int GuidReusePercentage;
        readonly Random rand = new Random();
        object RandLock = new object();

        private int NextRand(int minValue, int maxValue)
        {
            lock (RandLock)
            {
                return rand.Next(minValue, maxValue);
            }
        }

        public SimpleDataGeneratorBSON(int cacheSize, int totalItems, int numReusableLabelGroups, Tag[] tags, string[] guids = null, int guidReusePercentage = 0)
        {
            if (tags == null || tags.Length == 0)
                throw new Exception("what the hell, you need labels");

            this.CacheSize = Math.Min(cacheSize, totalItems);
            this.TotalItems = totalItems;

            Tags = tags;
            Guids = guids.Count() == 0 ? null : guids;
            GuidReusePercentage = guidReusePercentage;
        }

        public void Run(Action<List<BSONMention>> cacheFullCallback)
        {
            List<BSONMention> cache = new List<BSONMention>();

            Stopwatch sw = new Stopwatch();
            //Console.WriteLine("Starting generation of " + this.TotalItems.ToString() + " items...");

            sw.Start();

            for (int i = 0; i < TotalItems; i++)
            {
                var date = BeforeTodaySource.Next();

                string guidValue;
                if (Guids != null && NextRand(0, 101) < GuidReusePercentage)
                    guidValue = Guids[NextRand(0, Guids.Length)].ToString();
                else
                    guidValue = GetRandomMd5Hash();

                var newItem = new BSONMention
                {
                    Sentiment = NextRand(-100, 101),
                    Influence = NextRand(0, 101),
                    Name = ShortTextSource.Next(),
                    Description = null,//TextSource.Next(),
                    OccurredOnTicks = date.Ticks,
                    Guid = guidValue
                };

                newItem.CreatedOnTicks = new DateTime(newItem.OccurredOnTicks).AddDays(rand.Next(0, 5)).AddHours(NextRand(0, 10)).AddMinutes(NextRand(0, 60)).Ticks;
                int numTags = NextRand(0, 10);
                newItem.Tags = new List<int>();
                for (int j = 0; j < numTags; ++j)
                {
                    var targetTag = Tags[NextRand(0, Tags.Length)];
                    newItem.Tags.Add(targetTag.Id);
                }

                cache.Add(newItem);

                if ((i + 1) % (CacheSize) == 0 && i > 0)
                {
                    cacheFullCallback.Invoke(cache);
                    cache.Clear();
                }
            }
            sw.Stop();
            //Console.WriteLine("Finished generation in: " + sw.Elapsed.ToString());
        }

        public string GetRandomMd5Hash()
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = new byte[1024];

                lock (RandLock)
                {
                    rand.NextBytes(bytes);
                }

                return GetMd5Hash(md5Hash, bytes);
            }
        }

        public string GetMd5Hash(MD5 md5Hash, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }


    public class SimpleDataGenerator
    {
        readonly int CacheSize;
        readonly int TotalItems;

        readonly TextSource ShortTextSource = new TextSource(20, 140);
        readonly WordSource WordSource = new WordSource();
        readonly TextSource TextSource = new TextSource(100, 1024);
        readonly RandomDateTimeSource BeforeTodaySource = new RandomDateTimeSource(DateTime.Today.AddMonths(-6), DateTime.Now);
        //readonly List<List<string>> ReusableLabelGroups = new List<List<string>>();
        readonly Tag[] Tags;
        readonly string[] Guids;
        readonly int GuidReusePercentage;
        readonly Random rand = new Random();
        object RandLock = new object();

        private int NextRand(int minValue, int maxValue)
        {
            lock (RandLock)
            {
                return rand.Next(minValue, maxValue);
            }
        }

        public SimpleDataGenerator(int cacheSize, int totalItems, int numReusableLabelGroups, Tag[] tags, string[] guids = null, int guidReusePercentage = 0)
        {
            if (tags == null || tags.Length ==0)
                throw new Exception("what the hell, you need labels");

            this.CacheSize = Math.Min(cacheSize,totalItems);         
            this.TotalItems = totalItems;

            Tags = tags;
            Guids = guids.Count() == 0 ? null : guids;
            GuidReusePercentage = guidReusePercentage;
        }

        public void Run(Action<List<ThriftMention>> cacheFullCallback)
        {
            List<ThriftMention> cache = new List<ThriftMention>();

            Stopwatch sw = new Stopwatch();
            //Console.WriteLine("Starting generation of " + this.TotalItems.ToString() + " items...");

            sw.Start();

            for (int i = 0; i < TotalItems; i++)
            {
                var date = BeforeTodaySource.Next();

                string guidValue;
                if (Guids != null && NextRand(0, 101) < GuidReusePercentage)
                    guidValue = Guids[NextRand(0, Guids.Length)].ToString();
                else
                    guidValue = GetRandomMd5Hash();

                var newItem = new ThriftMention
                {
                    Sentiment = NextRand(-100, 101),
                    Influence = NextRand(0, 101),
                    Name = ShortTextSource.Next(),
                    Description = null, //TextSource.Next(),
                    OccurredOnTicks = date.Ticks,
                    Guid = guidValue
                };

                newItem.CreatedOnTicks = new DateTime(newItem.OccurredOnTicks).AddDays(rand.Next(0, 5)).AddHours(NextRand(0, 10)).AddMinutes(NextRand(0, 60)).Ticks;
                int numTags = NextRand(0,10);
                newItem.Tags = new List<int>();
                for (int j = 0; j < numTags; ++j)
                {
                    var targetTag = Tags[NextRand(0,Tags.Length)];
                    newItem.Tags.Add(targetTag.Id);
                }

                cache.Add(newItem);

                if ((i + 1) % (CacheSize) == 0 && i > 0)
                {
                    cacheFullCallback.Invoke(cache);
                    cache.Clear();
                }         
            }
            sw.Stop();
            //Console.WriteLine("Finished generation in: " + sw.Elapsed.ToString());
        }

        public string GetRandomMd5Hash()
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = new byte[1024];

                lock (RandLock)
                {
                    rand.NextBytes(bytes);
                }
                
                return GetMd5Hash(md5Hash, bytes);
            }
        }

        public string GetMd5Hash(MD5 md5Hash, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }

    public class SimpleDataGeneratorDateSpecific
    {
        readonly int CacheSize;
        readonly int TotalItems;

        readonly TextSource ShortTextSource = new TextSource(20, 140);
        readonly WordSource WordSource = new WordSource();
        readonly TextSource TextSource = new TextSource(100,1024);
        readonly RandomDateTimeSource BeforeTodaySource = new RandomDateTimeSource(DateTime.Today.AddMonths(-6), DateTime.Now);
        //readonly List<List<string>> ReusableLabelGroups = new List<List<string>>();
        readonly Tag[] Tags;
        readonly string[] Guids;
        readonly int GuidReusePercentage;
        readonly Random rand = new Random();
        object RandLock = new object();

        int day, hour, interval;

        private int NextRand(int minValue, int maxValue)
        {
            lock (RandLock)
            {
                return rand.Next(minValue, maxValue);
            }
        }

        public SimpleDataGeneratorDateSpecific(int day, int hour, int interval, int cacheSize, int totalItems, int numReusableLabelGroups, Tag[] tags, string[] guids = null, int guidReusePercentage = 0)
        {
            if (tags == null || tags.Length == 0)
                throw new Exception("what the hell, you need labels");

            this.CacheSize = Math.Min(cacheSize, totalItems);
            this.TotalItems = totalItems;

            Tags = tags;
            Guids = guids.Count() == 0 ? null : guids;
            GuidReusePercentage = guidReusePercentage;
            this.day = day;
            this.hour = hour;
            this.interval = interval;
        }

        public void Run(Action<List<ThriftMention>> cacheFullCallback)
        {
            List<ThriftMention> cache = new List<ThriftMention>();

            Stopwatch sw = new Stopwatch();
            //Console.WriteLine("Starting generation of " + this.TotalItems.ToString() + " items...");

            sw.Start();

            for (int i = 0; i < TotalItems; i++)
            {
                //var date = BeforeTodaySource.Next();
                var date = new DateTime(2011, 9, day + 1, hour, NextRand(interval*10, (interval*10+10)),NextRand(0,60));


                string guidValue;
                if (Guids != null && NextRand(0, 101) < GuidReusePercentage)
                    guidValue = Guids[NextRand(0, Guids.Length)].ToString();
                else
                    guidValue = GetRandomMd5Hash();

                var newItem = new ThriftMention
                {
                    Sentiment = NextRand(-100, 101),
                    Influence = NextRand(0, 101),
                    Name = ShortTextSource.Next(),
                    Description = null, //TextSource.Next(),
                    OccurredOnTicks = date.Ticks,
                    Guid = guidValue
                };

                newItem.CreatedOnTicks = new DateTime(newItem.OccurredOnTicks).AddDays(rand.Next(0, 5)).AddHours(NextRand(0, 10)).AddMinutes(NextRand(0, 60)).Ticks;
                int numTags = NextRand(0, 10);
                newItem.Tags = new List<int>();
                for (int j = 0; j < numTags; ++j)
                {
                    var targetTag = Tags[NextRand(0, Tags.Length)];
                    newItem.Tags.Add(targetTag.Id);
                }

                cache.Add(newItem);

                if ((i + 1) % (CacheSize) == 0 && i > 0)
                {
                    cacheFullCallback.Invoke(cache);
                    cache.Clear();
                }
            }
            sw.Stop();
            //Console.WriteLine("Finished generation in: " + sw.Elapsed.ToString());
        }

        public string GetRandomMd5Hash()
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = new byte[1024];

                lock (RandLock)
                {
                    rand.NextBytes(bytes);
                }

                return GetMd5Hash(md5Hash, bytes);
            }
        }

        public string GetMd5Hash(MD5 md5Hash, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
