using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Concurrent;

namespace Bermuda.Core.Cache
{
    public class SqlCacheAdapter
    {
        static readonly string ConnectionString = "Data Source=localhost;Initial Catalog=Bermuda;Integrated Security=True";

        public List<CacheRow> GetCacheRows(Guid domain)
        {
            List<CacheRow> result = new List<CacheRow>();

            using( var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                
                string sql = "SELECT * FROM Cache WHERE Domain=@domain";
                if( domain == Guid.Empty ) sql = "SELECT * FROM Cache";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue("@domain", domain);

                var reader = command.ExecuteReader();

               

                while (reader.Read())
                {
                    var row = new CacheRow();
                    row.Domain = (Guid)reader["Domain"];
                    row.Shard = (Guid)reader["Shard"];
                    row.Chunk = (Guid)reader["Chunk"];
                    row.ItemCount = (int)reader["ItemCount"];
                    row.Size = (int)reader["Size"];
                    row.HasStrongIndex = (bool)reader["HasStrongIndex"];
                    row.MinDate = (long)reader["MinDate"];
                    row.MaxDate = (long)reader["MaxDate"];
                    row.CreatedOn = (DateTime)reader["CreatedOn"];
                    result.Add(row);
                }
            }

            return result;
        }

        ConcurrentDictionary<Guid, CacheRowCache> refreshTimes = new ConcurrentDictionary<Guid, CacheRowCache>();
        TimeSpan refreshPeriod = TimeSpan.FromSeconds(5);

        public List<CacheRow> GetRows()
        {
            return GetRows(Guid.Empty);
        }

        public List<CacheRow> GetRows(Guid domain)
        {
            var now = DateTime.Now;

            CacheRowCache stored = null;
            //if (!refreshTimes.TryGetValue(domain, out stored) || now - stored.CreatedOn > refreshPeriod)
            {
                var list = GetCacheRows(domain);
                stored = new CacheRowCache { CreatedOn = DateTime.Now, Rows = list };
                refreshTimes[domain] = stored;
            }

            return stored.Rows;
        }
        
        public int Add(CacheRow row)
        {
            int result = 0;

            using( var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = "INSERT INTO Cache (Domain, Shard, CHunk, ItemCount, Size, HasStrongIndex, MinDate, MaxDate, CreatedOn) VALUES (@domain, @shard, @chunk, @itemcount, @size, @hasstrongindex, @mindate, @maxdate, GETDATE())";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@domain", row.Domain);
                command.Parameters.AddWithValue("@shard", row.Shard);
                command.Parameters.AddWithValue("@chunk", row.Chunk);
                command.Parameters.AddWithValue("@itemcount", row.ItemCount);
                command.Parameters.AddWithValue("@size", row.Size);
                command.Parameters.AddWithValue("@hasstrongindex", row.HasStrongIndex);
                command.Parameters.AddWithValue("@mindate", row.MinDate);
                command.Parameters.AddWithValue("@maxdate", row.MaxDate);

                result = command.ExecuteNonQuery();
            }

            return result;
        }

        public int Update(CacheRow row)
        {
            int result = 0;

            if (Guid.Empty.Equals(row.Chunk)) throw new Exception("renaming a shard row");

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = "UPDATE Cache SET ItemCount=@itemcount, Shard=@shard, Size=@size, MinDate=@mindate, MaxDate=@maxdate, CreatedOn=GETDATE() WHERE Chunk=@chunk";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@shard", row.Shard);
                command.Parameters.AddWithValue("@chunk", row.Chunk);
                command.Parameters.AddWithValue("@itemcount", row.ItemCount);
                command.Parameters.AddWithValue("@size", row.Size);
                command.Parameters.AddWithValue("@mindate", row.MinDate);
                command.Parameters.AddWithValue("@maxdate", row.MaxDate);

                result = command.ExecuteNonQuery();
            }

            return result;

        }

        class CacheRowCache
        {
            public DateTime CreatedOn;
            public List<CacheRow> Rows;
        }
    }
}
