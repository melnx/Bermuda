using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Bermuda.Entities.Thrift;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Bermuda.Service.Data
{
    public class BlobInterface
    {
        public static readonly TimeSpan PersistInterval = TimeSpan.FromMilliseconds(5000);
        public static readonly TimeSpan BlobListInterval = TimeSpan.FromMilliseconds(5000);

        public static ConcurrentBag<string> FilesOnDisk = new ConcurrentBag<string>();
        public static readonly bool UseBlobStorage = false;
        public static readonly bool UseLocalFileStorage = true;

        public BlobInterface(string storageAccount, string domain, string id, long minTime, long maxTime, bool hasStrongIndex, IEnumerable<ThriftMention> data)
        {
            _storageAccount = storageAccount;
            _domain = domain;
            _minTime = minTime;
            _maxTime = maxTime;
            _id = id;
            _hasStrongIndex = hasStrongIndex;

            MakeNameFromMetadata();
            MakePathFromMetadata();
        }

        public BlobInterface(string storageAccount, string domain, string name)
        {
            _name = name;
            _storageAccount = storageAccount;
            _domain = domain;

            ParseMetadataFromName();
            MakePathFromMetadata();
        }

        public BlobInterface()
        {
            
        }

        System.Timers.Timer PersistanceTimer;

        private void EnsurePersistanceTimer()
        {
            if (PersistanceTimer == null)
            {
                PersistanceTimer = new System.Timers.Timer();
                PersistanceTimer.Elapsed += new System.Timers.ElapsedEventHandler(aTimer_Elapsed);
                // Set the Interval to 5 seconds.
                PersistanceTimer.Interval = PersistInterval.TotalMilliseconds;
                PersistanceTimer.Enabled = true;
            }
        }

        void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (NeedsPersistance)
            {
                if(UseLocalFileStorage) PersistDataToLocalFile();
                if(UseBlobStorage) PersistDataToBlob();
            }
        }

        private void PersistDataToBlob()
        {
            //iota blob code goes here
        }

        private string _storageAccount;
        public string StorageAccount
        {
            get
            {
                return _storageAccount;
            }

            set
            {
                _storageAccount = value;
                MakePathFromMetadata();
            }
        }

        static ConcurrentDictionary<string, BlobInterface> StoredBlobInterfaces = new ConcurrentDictionary<string, BlobInterface>();
        static ConcurrentDictionary<string, Tuple<DateTime, IEnumerable<BlobInterface>>> storedBlobSets = new ConcurrentDictionary<string, Tuple<DateTime, IEnumerable<BlobInterface>>>();

        public static IEnumerable<BlobInterface> ListAllBlobInterfaces(string storageAccount, string domain)
        {
            if (!storedBlobSets.ContainsKey(storageAccount) || DateTime.Now - storedBlobSets[storageAccount].Item1 > BlobListInterval)
            {
                try
                {
                    var blobsInStorageAccount = Directory.GetFiles(Path.Combine(storageAccount, domain)).Select(x => x.Split('\\').Last()).Select(x => StoredBlobInterfaces.ContainsKey(x) ? StoredBlobInterfaces[x] : (StoredBlobInterfaces[x] = new BlobInterface(storageAccount, domain, x)));

                    storedBlobSets[storageAccount] = new Tuple<DateTime, IEnumerable<BlobInterface>>(DateTime.Now, blobsInStorageAccount);
                }
                catch
                {
                    Trace.WriteLine("FAILED TO LIST BLOBS: StorageAccount[" + storageAccount + "] Domain[" + domain + "]");
                }
            }

            return storedBlobSets[storageAccount].Item2;
        }

        public static IEnumerable<BlobInterface> GetBlobInterfacesByName(string storageAccount, string domain, IEnumerable<string> blobs)
        {
            try
            {
                return blobs.Select(x => StoredBlobInterfaces.ContainsKey(x) ? StoredBlobInterfaces[x] : (StoredBlobInterfaces[x] = new BlobInterface(storageAccount, domain, x)));
            }
            catch
            {
                Trace.WriteLine("FAILED TO GET REQUESTED BLOB INTERFACES: StorageAccount[" + storageAccount + "] Domain[" + domain + "] Blobs[" + string.Join(",", blobs) + "]");
            }

            return null;
        }

        private string _id;
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                MakeNameFromMetadata();
                MakePathFromMetadata();
            }
        }

        private long _minTime;
        public long MinTime
        {
            get
            {
                return _minTime;
            }
            set
            {
                _minTime = value;
                MakeNameFromMetadata();
                MakePathFromMetadata();
            }
        }

        private long _maxTime;
        public long MaxTime
        {
            get
            {
                return _maxTime;
            }
            set
            {
                _maxTime = value;
                MakeNameFromMetadata();
                MakePathFromMetadata();
            }
        }

        private string _domain;
        public string Domain
        {
            get
            {
                return _domain;
            }
            set
            {
                _domain = value;
                MakePathFromMetadata();
            }
        }

        private bool _hasStrongIndex;
        public bool HasStrongIndex
        {
            get
            {
                return _hasStrongIndex;
            }
            set
            {
                _hasStrongIndex = value;
                MakeNameFromMetadata();
                MakePathFromMetadata();
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    MakeNameFromMetadata();
                }
                return _name;
            }
            set
            {
                _name = value;
                MakePathFromMetadata();
            }
        }

        private void MakeNameFromMetadata()
        {
            _name = Id + "_" + MinTime + "_" + MaxTime + "_" + HasStrongIndex;
        }

        public void MakePathFromMetadata()
        {
            PersistPath = Path.Combine(StorageAccount, Domain, Name);
        }

        private void ParseMetadataFromName()
        {
            if (_name != null)
            {
                var parts = _name.Split('_');

                if (parts.Length == 4)
                {
                    _id = parts[0];
                    long.TryParse(parts[1], out _minTime);
                    long.TryParse(parts[2], out _maxTime);
                    bool.TryParse(parts[3], out _hasStrongIndex);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        bool NeedsPersistance = false;

        public void PersistDataToLocalFile()
        {
            try
            {
                var binary = ThriftMarshaller.Serialize<ThriftMentionChunk>(new ThriftMentionChunk { Mentions = Data });

                string domainPath = Path.Combine(StorageAccount, Domain);
                if (!Directory.Exists(StorageAccount)) Directory.CreateDirectory(StorageAccount);
                if (!Directory.Exists(domainPath)) Directory.CreateDirectory(domainPath);

                File.WriteAllBytes(PersistPath, binary);
            }
            catch
            {
                Trace.WriteLine("FAILED TO PERSIST DATA:" + PersistPath);
            }
            finally
            {
                NeedsPersistance = false;
            }
        }

        private string _persistPath;
        public string PersistPath
        {
            get
            {
                if (_persistPath == null)
                {
                    MakePathFromMetadata();
                }
                return _persistPath;
            }
            set
            {
                _persistPath = value;
            }
        }


        private List<ThriftMention> Data = null;

        public IEnumerable<ThriftMention> GetData()
        {
            if (Data == null)
            {
                if ( (!UseBlobStorage && UseLocalFileStorage) || FilesOnDisk.Contains(PersistPath))
                {
                    LoadFromFile();
                }
                else if (UseBlobStorage)
                {
                    LoadFromBlob();
                }
                else
                {
                    return (Data = new List<ThriftMention>());
                }
            }
            return Data;
        }

        void LoadFromBlob()
        {
            //iota code goes here

            FilesOnDisk.Add(PersistPath);
        }

        public void InsertData(IEnumerable<ThriftMention> data)
        {
            if (Data == null) LoadFromFile();
            Data.AddRange(data);
            NeedsPersistance = true;
            EnsurePersistanceTimer();
        }

        void LoadFromFile()
        {
            if (!File.Exists(PersistPath))
            {
                Data = new List<ThriftMention>();
            }
            else
            {
                try
                {
                    var binary = File.ReadAllBytes(PersistPath);
                    var set = ThriftMarshaller.Deserialize<ThriftMentionChunk>(binary);
                    Data = set.Mentions ?? new List<ThriftMention>();
                }
                catch
                {
                    Trace.WriteLine("FAILED TO LOAD FILE:" + PersistPath);
                }
            }
        }

        internal void SetData(IEnumerable<ThriftMention> data)
        {
            Data = data.ToList();
            NeedsPersistance = true;
            EnsurePersistanceTimer();
        }

        internal static int GetBlobLoadedCount()
        {
            return StoredBlobInterfaces.Count(x => x.Value.Data != null);
        }

        internal static int GetBlobCount()
        {
            return StoredBlobInterfaces.Count();
        }
    }
}