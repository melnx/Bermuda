using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Entities;
using Bermuda.Entities.Thrift;
using System.IO;
using System.Diagnostics;

namespace Bermuda
{
    public class InMemoryChunk : IChunk
    {
        string BasePath = @"C:\Bermuda\";

        public InMemoryChunk()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(aTimer_Elapsed);
            // Set the Interval to 5 seconds.
            aTimer.Interval = 5000;
            aTimer.Enabled = true;
        }

        void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (NeedsPersistance)
            {
                PersistData();
            }
        }

        public void PersistData()
        {
            var binary = ThriftMarshaller.Serialize<ThriftMentionChunk>(new ThriftMentionChunk { Mentions = Data });
            File.WriteAllBytes(Path, binary);
            NeedsPersistance = false;
        }

        bool NeedsPersistance = false;
        public ConsoleColor Color;
        public int Rank;
        public Guid Id;

        public string Path
        {
            get
            {
                return BasePath + Id;
            }
        }

        public List<ThriftMention> Data = null;

        public List<ThriftMention> GetData()
        {
            if (Data == null) LoadFromFile();
            return Data;
        }

        public void InsertData(IEnumerable<ThriftMention> data)
        {
            if (Data == null) LoadFromFile();
            Data.AddRange(data);
            NeedsPersistance = true;
        }

        void LoadFromFile()
        {
            if( !File.Exists(Path) )
            {
                Data = new List<ThriftMention>();
            }
            else
            {
                try
                {
                    var binary = File.ReadAllBytes(Path);
                    var set = ThriftMarshaller.Deserialize<ThriftMentionChunk>(binary);
                    Data = set.Mentions ?? new List<ThriftMention>();
                }
                catch
                {
                    Trace.WriteLine("FAILED TO LOAD FILE:" + Path);
                }
            }
        }
    }
}
