using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Entities.Thrift;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Bermuda.Entities;
using System.Data;

namespace Bermuda.Service.Data
{
    public class SqlInterface : IDataProvider
    {
        private static bool DebugDev = true;
        static readonly string DevDomain = "BandwidthSF";
        static readonly string DebugDomain = "EvoApp";
        public static readonly int DomainsToLoad = 5;

        static SqlDownloadType[] DontDownloadTypes = new SqlDownloadType[]{ SqlDownloadType.Theme, SqlDownloadType.ThemeMention };
        //static SqlDownloadType[] DontDownloadTypes = new SqlDownloadType[] { SqlDownloadType.Datasource, SqlDownloadType.DatasourceMention, SqlDownloadType.Tag, SqlDownloadType.TagAssociation, SqlDownloadType.Theme, SqlDownloadType.ThemeMention };

        ConcurrentDictionary<long, Mention> Mentions = new ConcurrentDictionary<long, Mention>();

        ConcurrentDictionary<string, Tag> TagLookup = new ConcurrentDictionary<string, Tag>();
        ConcurrentDictionary<long, TagAssociation> TagAssociations = new ConcurrentDictionary<long, TagAssociation>();
        ConcurrentDictionary<long, Tag> Tags = new ConcurrentDictionary<long, Tag>();
        ConcurrentDictionary<long, ConcurrentDictionary<long, long>> MentionTagLookup = new ConcurrentDictionary<long, ConcurrentDictionary<long, long>>();


        ConcurrentDictionary<long, Datasource> Datasources = new ConcurrentDictionary<long, Datasource>();
        ConcurrentDictionary<long, DatasourceMention> DatasourceMentions = new ConcurrentDictionary<long, DatasourceMention>();
        ConcurrentDictionary<long, ConcurrentDictionary<long, long>> MentionDatasourceLookup = new ConcurrentDictionary<long, ConcurrentDictionary<long, long>>();

        ConcurrentDictionary<string, Theme> ThemeLookup = new ConcurrentDictionary<string, Theme>();
        ConcurrentDictionary<long, Theme> Themes = new ConcurrentDictionary<long, Theme>();
        ConcurrentDictionary<long, ThemeMention> ThemeMentions = new ConcurrentDictionary<long, ThemeMention>();
        ConcurrentDictionary<long, ConcurrentDictionary<long, long>> MentionThemeLookup = new ConcurrentDictionary<long, ConcurrentDictionary<long, long>>();

        ConcurrentDictionary<long, string> TypeLookup = new ConcurrentDictionary<long, string>();
        ConcurrentDictionary<long, string> AuthorLookup = new ConcurrentDictionary<long, string>();

        public IEnumerable<Mention> GetData()
        {
            return Mentions.Values;
        }

        public SqlInterface()
        {
            Instances.Add(this);
        }

        static Timer timer;
        static int ForcedRem;
        static List<SqlInterface> Instances = new List<SqlInterface>();
        public static void InitializeBackgroundLoader()
        {
            var idstr = RoleEnvironment.CurrentRoleInstance.Id.Split('_').LastOrDefault();
            ForcedRem = int.Parse( idstr );
            ListAllSqlInterfaces();

            Trace.WriteLine("Loading " + StoredSqlInterfaces.Count() + " domains");
            StartDownload();

            timer = new Timer();
            timer.Interval = 30000;
            timer.AutoReset = false;
            timer.Elapsed +=new ElapsedEventHandler(TimerDownload);
            timer.Start();
        }

        static void TimerDownload(object sender, ElapsedEventArgs e)
        {
            StartDownload();
            timer.Start();
        }

        private static Random rng = new Random();

        private const int softDownloadLimit = 16;

        private static ConcurrentDictionary<Task, SqlInterface> runningDownloaders = new ConcurrentDictionary<Task, SqlInterface>();

        private static void StartDownload()
        {
            foreach (var current in Instances.OrderBy(x => !runningDownloaders.Any(y => y.Value == x)))
            {
                if (runningDownloaders.Count >= softDownloadLimit)
                {
                    return;
                }

                current.StartInstanceDownload();
            }
        }

        private ConcurrentDictionary<SqlDownloadType, DateTime> _LastRunTasks = null;
        private ConcurrentDictionary<SqlDownloadType, DateTime> LastRunTasks
        {
            get
            {
                if (_LastRunTasks == null)
                {
                    _LastRunTasks = new ConcurrentDictionary<SqlDownloadType, DateTime>();
                    foreach (SqlDownloadType type in Enum.GetValues(typeof(SqlDownloadType)))
                    {
                        _LastRunTasks.AddOrUpdate(type, DateTime.UtcNow, (x, y) => y);
                    }
                }
                return _LastRunTasks;
            }
        }

        private ConcurrentDictionary<SqlDownloadType, Task> runningTasks = new ConcurrentDictionary<SqlDownloadType, Task>();

        private void StartInstanceDownload()
        {
            List<Task> newTasks = new List<Task>();

            //foreach (SqlDownloadType current in Enum.GetValues(typeof(SqlDownloadType)).Cast<SqlDownloadType>().Where(x => !runningTasks.ContainsKey(x)))
            foreach(var key_value in LastRunTasks.OrderBy(t => t.Value))
            {
                SqlDownloadType download = key_value.Key;

                if (DontDownloadTypes.Contains(download) || runningTasks.ContainsKey(key_value.Key) ) continue;

                Task newTask = new Task(() => DownloadEntities(download));

                if (runningTasks.AddOrUpdate(download, newTask, (x, y) => y) == newTask)
                {
                    runningDownloaders.TryAdd(newTask, this);
                    newTask.ContinueWith(task =>
                        {
                            Task checkTask;
                            SqlInterface checkInterface;
                            runningTasks.TryRemove(download, out checkTask);
                            LastRunTasks.AddOrUpdate(download, DateTime.UtcNow, (x, y) => y);
                            runningDownloaders.TryRemove(task, out checkInterface);
                        });

                    newTasks.Add(newTask);
                }
            }

            newTasks.ForEach(x => x.Start());
        }

        private void AddMention(Mention mention)
        {
            if (!ReferenceEquals(Mentions.AddOrUpdate(mention.Id, mention, (x, y) => y), mention))
            {
                return;
            }

            ConcurrentDictionary<long, long> tagLookup;

            if (MentionTagLookup.TryGetValue(mention.Id, out tagLookup))
            {
                mention.Tags = tagLookup.Select(x => x.Key).ToList();
            }

            ConcurrentDictionary<long, long> datasourceLookup;

            if (MentionDatasourceLookup.TryGetValue(mention.Id, out datasourceLookup))
            {
                mention.Datasources = datasourceLookup.Select(x => x.Key).ToList();
            }

            ConcurrentDictionary<long, long> themeLookup;

            if (MentionThemeLookup.TryGetValue(mention.Id, out themeLookup))
            {
                mention.Themes = themeLookup.Select(x => x.Value).ToList();
            }
        }

        private void AddTagAssociation(TagAssociation tagAssociation)
        {
            if (tagAssociation.IsDisabled)
            {
                TagAssociation ta;
                TagAssociations.TryRemove(tagAssociation.Id, out ta);
                ConcurrentDictionary<long, long> mtl;
                if (MentionTagLookup.TryRemove(tagAssociation.MentionId, out mtl))
                {
                    long id;
                    mtl.TryRemove(tagAssociation.TagId, out id);
                }
                Mention m;
                if (Mentions.TryGetValue(tagAssociation.MentionId, out m))
                {
                    if(m.Tags.Contains(tagAssociation.TagId))
                        m.Tags.Remove(tagAssociation.TagId);
                }
            }
            else
            {
                if (!ReferenceEquals(TagAssociations.AddOrUpdate(tagAssociation.Id, tagAssociation, (x, y) => y), tagAssociation))
                {
                    return;
                }

                MentionTagLookup.AddOrUpdate(tagAssociation.MentionId,
                    new ConcurrentDictionary<long, long>(new KeyValuePair<long, long>[] { new KeyValuePair<long, long>(tagAssociation.TagId, tagAssociation.Id) }),
                    (x, y) => { y.AddOrUpdate(tagAssociation.TagId, tagAssociation.Id, (j, k) => { return k; }); return y; });

                Mention mention;

                if (!Mentions.TryGetValue(tagAssociation.MentionId, out mention))
                {
                    return;
                }

                if (mention.Tags == null || !mention.Tags.Contains(tagAssociation.TagId))
                {
                    if (mention.Tags == null) mention.Tags = new List<long>();
                    mention.Tags.Add(tagAssociation.TagId);
                }
            }
        }

        private void AddTag(Tag tag)
        {
            Tags.TryAdd(tag.Id, tag);
            TagLookup.TryAdd(tag.Name.ToLower(), tag);
        }

        private void AddDatasourceMention(DatasourceMention datasourceMention)
        {
            if (datasourceMention.IsDisabled)
            {
                DatasourceMention dm;
                DatasourceMentions.TryRemove(datasourceMention.Id, out dm);
                ConcurrentDictionary<long, long> dml;
                if (MentionDatasourceLookup.TryRemove(datasourceMention.MentionId, out dml))
                {
                    long id;
                    dml.TryRemove(datasourceMention.DatasourceId, out id);
                }
                Mention m;
                if (Mentions.TryGetValue(datasourceMention.MentionId, out m))
                {
                    if (m.Datasources.Contains(datasourceMention.DatasourceId))
                        m.Datasources.Remove(datasourceMention.DatasourceId);
                }
            }
            else
            {
                if (!ReferenceEquals(DatasourceMentions.AddOrUpdate(datasourceMention.Id, datasourceMention, (x, y) => y), datasourceMention))
                {
                    return;
                }

                MentionDatasourceLookup.AddOrUpdate(datasourceMention.MentionId,
                    new ConcurrentDictionary<long, long>(new KeyValuePair<long, long>[] { new KeyValuePair<long, long>(datasourceMention.DatasourceId, datasourceMention.Id) }),
                    (x, y) => { y.AddOrUpdate(datasourceMention.DatasourceId, datasourceMention.Id, (j, k) => { return k; }); return y; });

                Mention mention;

                if (!Mentions.TryGetValue(datasourceMention.MentionId, out mention))
                {
                    return;
                }

                if (mention.Datasources == null || !mention.Datasources.Contains(datasourceMention.DatasourceId))
                {
                    if (mention.Datasources == null) mention.Datasources = new List<long>();
                    mention.Datasources.Add(datasourceMention.DatasourceId);
                }
            }
        }

        private void AddDatasource(Datasource datasource)
        {
            Datasources.TryAdd(datasource.Id, datasource);
            //DatasourceLookup.TryAdd(datasource.Name.ToLower(), datasource);
        }

        private ConcurrentDictionary<SqlDownloadType, Tuple<DateTime, long>> lastUpdates = new ConcurrentDictionary<SqlDownloadType, Tuple<DateTime, long>>();

        private void DownloadEntities(SqlDownloadType downloadType)
        {

            //if (downloadType != SqlDownloadType.Mention)
            //    return;

            Tuple<DateTime, long> cutoff;

            lastUpdates.TryGetValue(downloadType, out cutoff);

            var updatedCutoff = cutoff != null ? cutoff.Item1 : default(DateTime);
            var idCutoff = cutoff!=null ? cutoff.Item2 : default(long);

            Trace.WriteLine("Starting to load with Mod:" + Mod + " Rem:" + Rem + " Domain:" + Name + " Type:" + downloadType);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //int ii = 0;
            //object iil = new object();

            int total = 0;

            int DegreeOfParallelism = GetDegreeOfParallelism(downloadType);

            System.Threading.ManualResetEvent eventError = new System.Threading.ManualResetEvent(false);

            Enumerable.Range(0, DegreeOfParallelism).AsParallel().ForAll(p =>
            {
                Stopwatch sw2 = new Stopwatch();
                sw2.Start();

                var localUpdatedCutoff = updatedCutoff;
                var localIdCutoff = idCutoff;

                //int i = 0;
                int current = 0;
                try
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        
                        connection.Open();

                        int mod = Mod * DegreeOfParallelism;
                        int rem = Rem * DegreeOfParallelism + p;

                        //PrintLoadingMetrics(Name, downloadType.ToString(), sw.Elapsed, TimeSpan.MinValue, i, 0, mod, rem);

                        string query = GetQueryString(downloadType, mod, rem, localUpdatedCutoff, localIdCutoff);
                        //var iii = ii;

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.CommandTimeout = 5 * 60;

                            if (localUpdatedCutoff > default(DateTime))
                            {
                                command.Parameters.AddWithValue("@updated", localUpdatedCutoff);
                            }

                            if (localIdCutoff > default(long))
                            {
                                command.Parameters.AddWithValue("@id", localIdCutoff);
                            }

                            using (SqlDataReader dr = command.ExecuteReader(CommandBehavior.SequentialAccess))
                            {
                                while (dr.Read())
                                {
                                    switch (downloadType)
                                    {
                                        case SqlDownloadType.Mention:
                                            var mention = GetMentionFromDataReader(dr);
                                            if (mention.UpdatedOnTicks > localUpdatedCutoff.Ticks) localUpdatedCutoff = mention.UpdatedOn;
                                            AddMention(mention);
                                            //var anothermMention = GetMentionFromDataReader(dr);
                                            //anothermMention.Id = anothermMention.Id + 1000000000;
                                            //if (anothermMention.UpdatedOnTicks > localUpdatedCutoff.Ticks) localUpdatedCutoff = anothermMention.UpdatedOn;
                                            //AddMention(anothermMention);
                                            break;

                                        case SqlDownloadType.TagAssociation:
                                            var tagAssociation = GetTagAssociationFromDataReader(dr);
                                            if (tagAssociation.UpdatedOn > localUpdatedCutoff) localUpdatedCutoff = tagAssociation.UpdatedOn;
                                            AddTagAssociation(tagAssociation);
                                            break;

                                        case SqlDownloadType.Tag:
                                            var tag = GetTagFromDataReader(dr);
                                            if (tag.CreatedOn > localUpdatedCutoff) localUpdatedCutoff = tag.CreatedOn;
                                            AddTag(tag);
                                            break;

                                        case SqlDownloadType.Datasource:
                                            var datasource = GetDatasourceFromDataReader(dr);
                                            if (datasource.CreatedOn > localUpdatedCutoff) localUpdatedCutoff = datasource.CreatedOn;
                                            AddDatasource(datasource);
                                            break;

                                        case SqlDownloadType.DatasourceMention:
                                            var datasourceMention = GetDatasourceMentionFromDataReader(dr);
                                            if (datasourceMention.UpdatedOn > localUpdatedCutoff) localUpdatedCutoff = datasourceMention.UpdatedOn;
                                            AddDatasourceMention(datasourceMention);
                                            break;

                                        case SqlDownloadType.Theme:
                                            var theme = GetThemeFromDataReader(dr);
                                            if (theme.Id > localIdCutoff) localIdCutoff = theme.Id;
                                            AddTheme(theme);
                                            break;

                                        case SqlDownloadType.ThemeMention:
                                            var themeMention = GetThemeMentionFromDataReader(dr);
                                            if (themeMention.UpdatedOn > localUpdatedCutoff) localUpdatedCutoff = themeMention.UpdatedOn;
                                            AddThemeMention(themeMention);
                                            break;

                                    }


                                    current++;
                                    System.Threading.Interlocked.Increment(ref total);
                                    //i++;
                                    //iii++;
                                    if (current % 1000 == 0)
                                        PrintLoadingMetrics(Name, downloadType.ToString(), sw.Elapsed, sw2.Elapsed, total, current, mod, rem);

                                    if (eventError.WaitOne(0))
                                        return;
                                }
                                PrintLoadingMetrics(Name, downloadType.ToString(), sw.Elapsed, sw2.Elapsed, total, current, mod, rem);
                            }
                        }

                        //lock (iil)
                        //{
                        //    ii += i;
                        //}

                    }        
                }
                catch (SqlException sql)
                {
                    Trace.WriteLine("ERROR TALKING TO SQL: " + sql.Message);
                    eventError.Set();
                }
                catch (InvalidOperationException ix)
                {
                    Trace.WriteLine("ERROR: " + ix.Message);
                    eventError.Set();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("ERROR: " + ex.Message);
                    eventError.Set();
                }
                
                sw.Stop();

                var newCutoff = new Tuple<DateTime, long>(localUpdatedCutoff, localIdCutoff);
                lastUpdates.AddOrUpdate(downloadType, newCutoff, (x, y) => y != null && (y.Item1 > localUpdatedCutoff || y.Item2 > localIdCutoff) ? y : newCutoff);
            });

            sw.Stop();

            if (eventError.WaitOne(0))
            {
                var newCutoff = new Tuple<DateTime, long>(updatedCutoff, idCutoff);
                lastUpdates.AddOrUpdate(downloadType, newCutoff, (x, y) => y);
            }

            //var newCutoff = new Tuple<DateTime, long>(updatedCutoff, idCutoff);
            //lastUpdates.AddOrUpdate(downloadType, newCutoff, (x, y) => y != null && (y.Item1 > updatedCutoff || y.Item2 > idCutoff) ? y : newCutoff);
        }

        private int GetDegreeOfParallelism(SqlDownloadType type)
        {
            switch (type)
            {
                case SqlDownloadType.Datasource:
                    return 4;
                case SqlDownloadType.DatasourceMention:
                case SqlDownloadType.Tag:
                case SqlDownloadType.TagAssociation:
                case SqlDownloadType.Theme:
                case SqlDownloadType.ThemeMention:
                    return 1;
                case SqlDownloadType.Mention:
                    return 4;
                default:
                    return 1;
            }
        }

        private void AddThemeMention(ThemeMention themeMention)
        {
            if (themeMention.IsDisabled)
            {
                ThemeMention tm;
                ThemeMentions.TryRemove(themeMention.Id, out tm);
                ConcurrentDictionary<long, long> mtl;
                if (MentionThemeLookup.TryGetValue(themeMention.MentionId, out mtl))
                {
                    long id;
                    mtl.TryRemove(themeMention.Id, out id);
                }
                Mention m;
                if (Mentions.TryGetValue(themeMention.MentionId, out m))
                {
                    while (m.Themes != null && m.Themes.Contains(themeMention.ThemeId))
                        m.Themes.Remove(themeMention.ThemeId);
                }
            }
            else
            {
                if (!ReferenceEquals(ThemeMentions.AddOrUpdate(themeMention.Id, themeMention, (x, y) => y), themeMention))
                {
                    return;
                }

                MentionThemeLookup.AddOrUpdate
                    (
                        themeMention.MentionId,
                        new ConcurrentDictionary<long, long>
                            (
                                new KeyValuePair<long, long>[] 
                                { 
                                    new KeyValuePair<long, long>(themeMention.Id, themeMention.ThemeId) 
                                }
                            ),
                        (x, y) => 
                        { 
                            y.AddOrUpdate
                                (
                                    themeMention.Id, 
                                    themeMention.ThemeId, 
                                    (j, k) => 
                                    {    
                                        return k; 
                                    }
                                ); 
                            return y; 
                        }
                    );

                Mention mention;

                if (!Mentions.TryGetValue(themeMention.MentionId, out mention))
                {
                    return;
                }

                if (mention.Themes == null || !mention.Themes.Contains(themeMention.ThemeId))
                {
                    if (mention.Themes == null) mention.Themes = new List<long>();
                    mention.Themes.Add(themeMention.ThemeId);
                }
            }
        }

        private ThemeMention GetThemeMentionFromDataReader(SqlDataReader dr)
        {
            var id = dr.GetInt64(0);
            var instanceid = dr.GetInt32(1);
            var themeid = dr.GetInt64(2);
            var isdisabled = dr.IsDBNull(3) ? false : dr.GetBoolean(3);
            var updatedon = dr.IsDBNull(4) ? new DateTime(1970, 1, 1) : dr.GetDateTime(4);

            return new ThemeMention
            {
                Id = id,
                MentionId = instanceid,
                ThemeId = themeid,
                IsDisabled = isdisabled,
                UpdatedOn = updatedon
            };
        }

        private void AddTheme(Theme theme)
        {
            Themes.TryAdd(theme.Id, theme);
            ThemeLookup.TryAdd(theme.Text.ToLower(), theme);
        }

        private Theme GetThemeFromDataReader(SqlDataReader dr)
        {
            var id = dr.GetInt64(0);
            var text = dr.IsDBNull(1) ? "" : dr.GetString(1);

            return new Theme
            {
                Id = id,
                Text = text
            };
        }

        private string GetQueryString(SqlDownloadType downloadType, int mod, int rem, DateTime updated, long maxid)
        {
            //Tuple<DateTime,long> cutoff;

            //lastUpdates.TryGetValue(downloadType, out cutoff);

            //var updated = cutoff == null ? default(DateTime) : cutoff.Item1;
            //var maxid = cutoff == null ? default(long) : cutoff.Item2;

            bool isDeltaUpdate = updated > default(DateTime) || maxid > default(long);

            string query = null;

            switch (downloadType)
            {
                case SqlDownloadType.Mention:
                    query = string.Format("SELECT Id, OccurredOn, UpdatedOn, Name, Evaluation, UniqueId, Description, CreatedOn, Type, Username, Influence, Followers, KloutScore, ChildCount FROM Instances with(NOLOCK) WHERE UniqueId IS NOT NULL AND OccurredOn IS NOT NULL AND Id%{0}={1}", mod, rem);
                    if (isDeltaUpdate) query += " AND UpdatedOn IS NOT NULL AND UpdatedOn >= @updated";
                    query += " ORDER BY UpdatedOn";
                    break;

                case SqlDownloadType.TagAssociation:
                    query = string.Format("SELECT Id, TagId, InstanceId, IsDisabled, UpdatedOn FROM TagAssociations with(NOLOCK) WHERE InstanceId%{0}={1}", mod, rem);
                    if (isDeltaUpdate) query += " AND UpdatedOn IS NOT NULL AND UpdatedOn >= @updated";
                    query += " ORDER BY UpdatedOn";
                    break;

                case SqlDownloadType.Tag:
                    query = string.Format("SELECT Id, Name, CreatedOn FROM Tags with(NOLOCK)");
                    if (isDeltaUpdate) query += " WHERE CreatedOn >= @updated";
                    query += " ORDER BY CreatedOn";
                    break;

                case SqlDownloadType.Datasource:
                    query = string.Format("SELECT Id, Name, CreatedOn, Value FROM DownloadItems with(NOLOCK) WHERE IsDisabled = 0 AND IsVisible=1");// AND IsVisible = 1");
                    if (isDeltaUpdate) query += " AND CreatedOn >= @updated";
                    query += " ORDER BY CreatedOn";
                    break;

                case SqlDownloadType.DatasourceMention:
                    query = string.Format("SELECT Id, DownloadItemId, MentionId, IsDisabled, UpdatedOn FROM DownloadItemMentions with(NOLOCK) WHERE MentionId%{0}={1}", mod, rem);
                    if (isDeltaUpdate) query += " AND UpdatedOn >= @updated";
                    query += " ORDER BY UpdatedOn";
                    break;

                case SqlDownloadType.Theme:
                    query = string.Format("SELECT Id, Text FROM Phrases with(NOLOCK)");
                    if (isDeltaUpdate) query += " WHERE Id >= @id";
                    query += " ORDER BY Id";
                    break;

                case SqlDownloadType.ThemeMention:
                    query = string.Format("SELECT Id, InstanceId, PhraseId, IsDisabled, UpdatedOn FROM PhraseInstances with(NOLOCK) WHERE InstanceId%{0}={1}", mod, rem);
                    if (isDeltaUpdate) query += " WHERE UpdatedOn >= @updated";
                    query += " ORDER BY UpdatedOn";
                    break;
            }

            return query;
        }

        private static Mention GetMentionFromDataReader(SqlDataReader dr)
        {
            var id = dr.GetInt32(0);
            var occurred = dr.GetDateTime(1);
            var updated = dr.GetDateTime(2);
            var name = dr.IsDBNull(3) ? "" : dr.GetString(3);
            var sent = dr.GetDouble(4);
            var guid = dr.IsDBNull(5) ? "" : dr.GetString(5);
            var desc = dr.IsDBNull(6) ? "" : dr.GetString(6);
            var created = dr.GetDateTime(7);
            var type = dr.IsDBNull(8) ? "" : dr.GetString(8);
            
            var author = dr.IsDBNull(9) ? "" : dr.GetString(9);
            var influence = dr.IsDBNull(10) ? 0L : (long)dr.GetInt32(10);
            var followers = dr.IsDBNull(11) ? 0L : (long)dr.GetInt32(11);
            var klout = dr.IsDBNull(12) ? 0L : (long)dr.GetInt32(12);
            var comments = dr.IsDBNull(13) ? 0L : (long)dr.GetInt32(13);

            var mention = new Mention
            {
                Type = type,
                Name = name,
                Sentiment = sent,
                Guid = guid,
                Description = desc,
                Id = id,
                Author = author,
                Influence = influence,
                Followers = followers,
                Klout = klout,
                Comments = comments,

                //dates
                OccurredOn = occurred,
                UpdatedOn = updated,
                CreatedOn = created,

                //ticks 
                OccurredOnTicks = occurred.Ticks,
                UpdatedOnTicks = updated.Ticks,
                CreatedOnTicks = created.Ticks,
                //OccurredOnMinuteTicks = new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, occurred.Minute, 0).Ticks,
                //OccurredOnQuarterHourTicks = new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, occurred.Minute - occurred.Minute % 15, 0).Ticks,
                //OccurredOnDayTicks = new DateTime(occurred.Year, occurred.Month, occurred.Day).Ticks,
                //OccurredOnWeekTicks = new DateTime(occurred.Year, occurred.Month, occurred.Day).Ticks,
                //OccurredOnMonthTicks = new DateTime(occurred.Year, occurred.Month, 1).Ticks,
                //OccurredOnQuarterTicks = new DateTime(occurred.Year, (((occurred.Month - 1) / 3) * 3 + 1), 1).Ticks,
                //OccurredOnYearTicks = new DateTime(occurred.Year, 1, 1).Ticks
            };

            return mention;
        }

        private static TagAssociation GetTagAssociationFromDataReader(SqlDataReader dr)
        {
            return new TagAssociation
            {
                Id = dr.GetInt32(0),
                TagId = dr.GetInt32(1),
                MentionId = dr.GetInt32(2),
                IsDisabled = dr.IsDBNull(3) ? false : dr.GetBoolean(3),
                UpdatedOn = dr.IsDBNull(4) ? new DateTime(1970, 1, 1) : dr.GetDateTime(4)
            };
        }

        private static Tag GetTagFromDataReader(SqlDataReader dr)
        {
            return new Tag
            {
                Id = dr.GetInt32(0),
                Name = dr.IsDBNull(1) ? "" : dr.GetString(1),
                CreatedOn = dr.GetDateTime(2)
            };
        }

        private static Datasource GetDatasourceFromDataReader(SqlDataReader dr)
        {
            return new Datasource
            {
                Id = dr.GetInt64(0),
                Name = dr.IsDBNull(1) ? "" : dr.GetString(1),
                CreatedOn = dr.GetDateTime(2),
                Value = dr.IsDBNull(3) ? "" : dr.GetString(3)
            };
        }

        private static DatasourceMention GetDatasourceMentionFromDataReader(SqlDataReader dr)
        {
            return new DatasourceMention
            {
                Id = dr.GetInt64(0),
                DatasourceId = dr.GetInt64(1),
                MentionId = dr.GetInt32(2),
                IsDisabled = dr.IsDBNull(3) ? false : dr.GetBoolean(3),
                UpdatedOn = dr.IsDBNull(4) ? new DateTime(1970, 1, 1) : dr.GetDateTime(4),
            };
        }

        private void PrintLoadingMetrics(string domain, string caption, TimeSpan elapsed, TimeSpan elapsed2, int count, int count2, int mod, int rem)
        {
            var kmps = Math.Round(((double)count / 1000d) / elapsed.TotalSeconds, 2);
            var kmps2 = Math.Round(((double)count2 / 1000d) / elapsed2.TotalSeconds, 2);
            var initial = caption.ToLower().First();
            Trace.WriteLine("[" + domain + "][" + caption + "][Id%" + mod + "=" + rem + "] Loaded " + count + initial + " in " + elapsed + " [total:" + kmps + "k" + initial + "ps | current:" + kmps2 + "k" + initial + "ps]  " );
        }

        public string Name { get; set; }
        public string Id { get; set; }

        int? _mod;
        public int Mod
        {
            get
            {
                #if DEBUG
                return _mod ?? (_mod = AzureInterface.Instance.PeerEndpoints.Count() + 1).Value;
                #else
                return _mod ?? (_mod = AzureInterface.Instance.PeerEndpoints.Count() + 1).Value;
                #endif
            }
        }
        public int Rem = 0;
        //public static readonly int SubPageCount = 4;
        //public static readonly int SubPageCount = Environment.ProcessorCount;
        //public static readonly int SubPageCount = 1;
        
        public static readonly TimeSpan BlobListInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ConnectionListInterval = TimeSpan.FromSeconds(30);
        public static ConcurrentBag<SqlInterface> StoredSqlInterfaces = new ConcurrentBag<SqlInterface>();

        static readonly string DevAccessConnectionString = "Data Source=jizqh6hqdn.database.windows.net;Initial Catalog=EvoApp.Access;User ID=adm1n;Password=9a$$word;MultipleActiveResultSets=true";
        static readonly string LocalAccessConnectionString = "Data Source=localhost;Initial Catalog=EvoApp.Access;User ID=EvoApp;Password=9a$$word;MultipleActiveResultSets=true";
        static string AccessConnectionString
        {
            get
            {
                #if DEBUG
                return DebugDev ? DevAccessConnectionString : LocalAccessConnectionString;
                #else
                return DevAccessConnectionString;
                #endif
            }
        }
    
        static DateTime lastConnectionInfoRefresh;
        static IEnumerable<DomainConnectionInfo> storedConnectionInfo;
        private string ConnectionString;
        
        private static IEnumerable<DomainConnectionInfo> GetDomainConnectionInfo()
        {
            if (storedConnectionInfo == null || DateTime.UtcNow - lastConnectionInfoRefresh > ConnectionListInterval)
            {
                lastConnectionInfoRefresh = DateTime.UtcNow;
                List<DomainConnectionInfo> result = new List<DomainConnectionInfo>();
                string query = "select d.name,d.subdomain,c.connectionstring  from [Domains] as d LEFT JOIN [Connections] as c ON c.Id = d.ConnectionId";
                using (SqlConnection connection = new SqlConnection(AccessConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        using (SqlDataReader dr = command.ExecuteReader())
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    var info = new DomainConnectionInfo { Name = dr.GetString(0), Subdomain = dr.GetString(1), ConnectionString = dr.GetString(2) };
                                    info.ConnectionString += ";Max Pool Size=1000;Min Pool Size=50;Connection Timeout=300";


                                    #if DEBUG
                                    if (DebugDev) 
                                    { 
                                        if (result.Count >= DomainsToLoad) break;
                                        if (DevDomain != null && DevDomain != info.Subdomain) continue;
                                    }
                                    else { if (info.Subdomain != DebugDomain) continue; }
                                    #else
                                    if (result.Count >= DomainsToLoad) break;
                                    #endif

                                    result.Add(info);
                                }
                            }
                        }


                    }
                }

                storedConnectionInfo = result;
                return result;
            }
            else
            {
                return storedConnectionInfo;
            }
        }

        public static IEnumerable<SqlInterface> ListAllSqlInterfaces()
        {
            RefreshStoredInterfaces();

            return StoredSqlInterfaces;   
        }

        private static void RefreshStoredInterfaces()
        {
            var connectionInfo = GetDomainConnectionInfo();

            foreach (var c in connectionInfo)
            {
                if (StoredSqlInterfaces.FirstOrDefault(x => x.ConnectionString == c.ConnectionString) != null) continue;


                var newInterface = new SqlInterface { Name = c.Subdomain, ConnectionString = c.ConnectionString, Rem = ForcedRem };
                StoredSqlInterfaces.Add(newInterface);
            }
        }

        public static IEnumerable<IDataProvider> GetSqlInterfacesForDomain(string domain)
        {
            RefreshStoredInterfaces();

            return StoredSqlInterfaces.Where(x => String.Equals(x.Name.ToLower(), domain.ToLower(), StringComparison.InvariantCultureIgnoreCase));
        }

    }
}
