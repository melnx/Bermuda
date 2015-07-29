using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Catalog;
using Bermuda.Interface;
using System.Data.SqlClient;
using System.Data;
using Bermuda.Constants;
using System.Runtime.Serialization;

namespace ComputeNodeMX
{
    [DataContract]
    public class ComputeNodeMX : ComputeNode
    {

        #region Constructor

        /// <summary>
        /// constructor with the compute node index
        /// </summary>
        /// <param name="index"></param>
        public ComputeNodeMX(Int64 index, Int64 bucket_count, Int64 compute_node_count)
            :base(index, bucket_count, compute_node_count)
        {
            
        }


        #endregion

        /// <summary>
        /// debug stuff
        /// </summary>
        private static bool DebugAzureDev = true;
        //static readonly string DevDomain = "StressTest";
        //static readonly string DevDomain = "RegTest";
        static readonly string DevDomain = "BandwidthSF";
        static readonly string DebugDomain = "EvoApp";

        /// <summary>
        /// access connection
        /// </summary>
        static readonly string AzureDevAccessConnectionString = "Data Source=jizqh6hqdn.database.windows.net;Initial Catalog=EvoApp.Access;User ID=adm1n;Password=9a$$word;MultipleActiveResultSets=true";
        static readonly string LocalAccessConnectionString = "Data Source=localhost;Initial Catalog=EvoApp.Access;User ID=EvoApp;Password=9a$$word;MultipleActiveResultSets=true";
        static string AccessConnectionString
        {
            get
            {
#if DEBUG
                return DebugAzureDev ? AzureDevAccessConnectionString : LocalAccessConnectionString;
#else
                return AzureDevAccessConnectionString;
#endif
            }
        }

        
        /// <summary>
        /// refresh the list of saturation tables
        /// </summary>
//        protected override IEnumerable<ICatalog> GetCatalogs()
//        {
//            List<ICatalog> refresh_catalogs = new List<ICatalog>();
//            try
//            {
//                //conection info table
//                System.Data.DataTable dt = new System.Data.DataTable();

//                //create the connection
//                using (SqlConnection connection = new SqlConnection(AccessConnectionString))
//                {
//                    connection.Open();

//                    //data adapter
//                    string sql = "select d.name,d.subdomain,d.bermudaenabled,c.connectionstring  from [Domains] as d LEFT JOIN [Connections] as c ON c.Id = d.ConnectionId";
//                    using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
//                    {
//                        //set the connection
//                        adapter.SelectCommand.CommandTimeout = 1 * 60;
//                        adapter.SelectCommand.Connection = connection;

//                        //fill a table to return
//                        adapter.Fill(dt);
//                    }
//                    //close the connection
//                    connection.Close();
//                }
//                //parse the connection info
//                foreach (DataRow dr in dt.Rows)
//                {
//                    //get the data
//                    string Name = dr[0].ToString();
//                    string SubDomain = dr[1].ToString();
//                    bool BermudaEnabled = (bool)dr[2];
//                    string ConnectionString = dr[3].ToString();

//                    //debug handling to force domains to use
//#if DEBUG
//                    if (DebugAzureDev)
//                    {
//                        if (DevDomain != null && DevDomain != SubDomain) continue;
//                    }
//                    else 
//                    { 
//                        if (SubDomain != DebugDomain) continue; 
//                    }
//#endif
//                    //check if the catalog exists
//                    if (BermudaEnabled)// && !Catalogs.ContainsKey(SubDomain))
//                    {
//                        //init the catalog
//                        refresh_catalogs.Add(InitializeMXCatalog(SubDomain, ConnectionString));
//                        //ICatalog catalog = InitializeMXCatalog(SubDomain, ConnectionString);

//                        //init this catalog for saturation
//                        //SaturationTables.AddRange(catalog.GetSaturationTables());
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Trace.WriteLine(ex.ToString());
//            }
//            return refresh_catalogs;
//        }

        /// <summary>
        /// init an mx catalog
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        ICatalog InitializeMXCatalog(string Name, string ConnectionString)
        {
            //create a catalog
            Catalog catalog = new Catalog(this);
            catalog.CatalogName = Name;
            catalog.ConnectionString = ConnectionString;
            catalog.ConnectionType = ConnectionTypes.SQLServer;
            catalog.CatalogMetadata = new CatalogMetadata(catalog);

            //mentions
            TableMetadata mention_metadata = new TableMetadata(catalog.CatalogMetadata);
            mention_metadata.DataType = typeof(Mention);
            mention_metadata.Filter = "InstanceType in (461, 424)";
            mention_metadata.MaxSaturationItems = 2000;
            mention_metadata.ModField = "Id";
            mention_metadata.OrderBy = "UpdatedOn";
            mention_metadata.PrimaryKey = "Id";
            //mention_metadata.Query = "Id, OccurredOn, UpdatedOn, Name, Evaluation, UniqueId, Description, CreatedOn, Type, Username, Influence, Followers, KloutScore, ChildCount, IsDisabled FROM Instances with(NOLOCK)";
            mention_metadata.Query =
                "i.Id, i.OccurredOn, i.UpdatedOn, i.Name, i.Evaluation, i.UniqueId, i.Description, i.CreatedOn, i.Type, i.Username, i.Influence, i.Followers, i.KloutScore, i.ChildCount, i.IsDisabled, " +
                    "Tags = substring((SELECT ( ',' + Convert(varchar(30), ta.Id) + '|' + Convert(varchar(30), ta.TagId) ) " +
                                "FROM TagAssociations ta with(nolock) " +
                                "WHERE i.Id = ta.InstanceId " +
                                "ORDER BY " + 
                                    "InstanceId, " +
                                    "TagId " +
                                "FOR XML PATH( '' ) " +
                                "), 2, 1000000000000000000 ), " +
                    "Datasources = substring((SELECT ( ',' + Convert(varchar(30), dm.Id) + '|' + Convert(varchar(30), dm.DownloadItemId) ) " +
                                "FROM DownloadItemMentions dm with(nolock) " +
                                "WHERE i.Id = dm.MentionId " +
                                "ORDER BY " + 
                                    "MentionId, " +
                                    "DownloadItemId " +
                                "FOR XML PATH( '' ) " +
                                "), 2, 1000000000000000000 ), " +
                    "Themes = substring((SELECT ( ',' + Convert(varchar(30), p.Id) + '|' + Convert(varchar(30), p.PhraseId) ) " +
                                "FROM PhraseInstances p with(nolock) " +
                                "WHERE i.Id = p.InstanceId " +
                                "ORDER BY " + 
                                    "InstanceId, " +
                                    "PhraseId " +
                                "FOR XML PATH( '' ) " +
                                "), 2, 1000000000000000000 ) " +
                "FROM Instances i with(NOLOCK) ";
            mention_metadata.ReferenceTable = false;
            mention_metadata.SaturationDeleteField = "IsDisabled";
            mention_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            mention_metadata.SaturationDeleteType = typeof(bool);
            mention_metadata.SaturationDeleteValue = true;
            mention_metadata.SaturationFrequency = 30 * 1000;
            mention_metadata.SaturationPurgeField = "UpdatedOn";
            mention_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            mention_metadata.SaturationPurgePercent = 5;
            mention_metadata.SaturationPurgeType = typeof(DateTime);
            mention_metadata.SaturationUpdateField = "UpdatedOn";
            mention_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            mention_metadata.SaturationUpdateType = typeof(DateTime);
            mention_metadata.TableName = "Mentions";
            catalog.CatalogMetadata.Tables.Add(mention_metadata.TableName, mention_metadata);
            ColumnMetadata col;
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "OccurredOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "Date",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "UpdatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "UpdatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Name",
                ColumnType = typeof(string),
                FieldMapping = "Name",
                Nullable = false,
                ColumnLength = 1000,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Evaluation",
                ColumnType = typeof(double),
                FieldMapping = "Sentiment",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "UniqueId",
                ColumnType = typeof(string),
                FieldMapping = "Guid",
                Nullable = false,
                ColumnLength = 1000,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Description",
                ColumnType = typeof(string),
                FieldMapping = "Description",
                Nullable = false,
                ColumnLength = 1000,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "CreatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "CreatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Type",
                ColumnType = typeof(string),
                FieldMapping = "Type",
                Nullable = false,
                ColumnLength = 100,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Username",
                ColumnType = typeof(string),
                FieldMapping = "Author",
                Nullable = false,
                ColumnLength = 1000,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Influence",
                ColumnType = typeof(Int64),
                FieldMapping = "Influence",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Followers",
                ColumnType = typeof(Int64),
                FieldMapping = "Followers",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "KloutScore",
                ColumnType = typeof(Int64),
                FieldMapping = "Klout",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "ChildCount",
                ColumnType = typeof(Int64),
                FieldMapping = "Comments",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Tags",
                ColumnType = typeof(List<Tuple<List<long>, long>>),
                FieldMapping = "Tags",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = false
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Datasources",
                ColumnType = typeof(List<Tuple<List<long>, long>>),
                FieldMapping = "Datasources",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = false
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(mention_metadata)
            {
                ColumnName = "Themes",
                ColumnType = typeof(List<Tuple<List<long>, long>>),
                FieldMapping = "Themes",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = false
            };
            mention_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //tags
            TableMetadata tag_metadata = new TableMetadata(catalog.CatalogMetadata);
            tag_metadata.DataType = typeof(Tag);
            tag_metadata.Filter = "";
            //tag_metadata.MaxSaturationItems = 100;
            tag_metadata.MaxSaturationItems = -1;
            tag_metadata.ModField = "Id";
            tag_metadata.OrderBy = "CreatedOn";
            tag_metadata.PrimaryKey = "Id";
            tag_metadata.Query = "Id, Name, CreatedOn, IsDisabled FROM Tags with(NOLOCK)";
            tag_metadata.ReferenceTable = true;
            tag_metadata.SaturationDeleteField = "IsDisabled";
            tag_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            tag_metadata.SaturationDeleteType = typeof(bool);
            tag_metadata.SaturationDeleteValue = true;
            tag_metadata.SaturationFrequency = 30 * 1000;
            tag_metadata.SaturationPurgeField = "";
            tag_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_NONE;
            tag_metadata.SaturationPurgePercent = 0;
            tag_metadata.SaturationPurgeType = null;
            tag_metadata.SaturationUpdateField = "CreatedOn";
            tag_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            tag_metadata.SaturationUpdateType = typeof(DateTime);
            tag_metadata.TableName = "Tags";
            catalog.CatalogMetadata.Tables.Add(tag_metadata.TableName, tag_metadata);
            col = new ColumnMetadata(tag_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tag_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tag_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tag_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tag_metadata)
            {
                ColumnName = "Name",
                ColumnType = typeof(string),
                FieldMapping = "Name",
                Nullable = false,
                ColumnLength = 100,
                ColumnPrecision = 0,
                Visible = true
            };
            tag_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tag_metadata)
            {
                ColumnName = "CreatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "CreatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tag_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //download items
            TableMetadata datasource_metadata = new TableMetadata(catalog.CatalogMetadata);
            datasource_metadata.DataType = typeof(Datasource);
            datasource_metadata.Filter = "IsVisible = 1";
            //datasource_metadata.MaxSaturationItems = 100;
            datasource_metadata.MaxSaturationItems = -1;
            datasource_metadata.ModField = "Id";
            datasource_metadata.OrderBy = "CreatedOn";
            datasource_metadata.PrimaryKey = "Id";
            datasource_metadata.Query = "Id, Name, CreatedOn, Value, IsDisabled FROM DownloadItems with(NOLOCK)";
            datasource_metadata.ReferenceTable = true;
            datasource_metadata.SaturationDeleteField = "IsDisabled";
            datasource_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            datasource_metadata.SaturationDeleteType = typeof(bool);
            datasource_metadata.SaturationDeleteValue = true;
            datasource_metadata.SaturationFrequency = 30 * 1000;
            datasource_metadata.SaturationPurgeField = "";
            datasource_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_NONE;
            datasource_metadata.SaturationPurgePercent = 0;
            datasource_metadata.SaturationPurgeType = null;
            datasource_metadata.SaturationUpdateField = "CreatedOn";
            datasource_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            datasource_metadata.SaturationUpdateType = typeof(DateTime);
            datasource_metadata.TableName = "DownloadItems";
            catalog.CatalogMetadata.Tables.Add(datasource_metadata.TableName, datasource_metadata);
            col = new ColumnMetadata(datasource_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            datasource_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(datasource_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            datasource_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(datasource_metadata)
            {
                ColumnName = "Name",
                ColumnType = typeof(string),
                FieldMapping = "Name",
                Nullable = false,
                ColumnLength = 100,
                ColumnPrecision = 0,
                Visible = true
            };
            datasource_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(datasource_metadata)
            {
                ColumnName = "CreatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "CreatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            datasource_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(datasource_metadata)
            {
                ColumnName = "Value",
                ColumnType = typeof(string),
                FieldMapping = "Value",
                Nullable = false,
                ColumnLength = 1000,
                ColumnPrecision = 0,
                Visible = true
            };
            datasource_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //themes
            TableMetadata theme_metadata = new TableMetadata(catalog.CatalogMetadata);
            theme_metadata.DataType = typeof(Theme);
            theme_metadata.Filter = "";
            //theme_metadata.MaxSaturationItems = 50000;
            theme_metadata.MaxSaturationItems = -1;
            theme_metadata.ModField = "Id";
            theme_metadata.OrderBy = "Id";
            theme_metadata.PrimaryKey = "Id";
            theme_metadata.Query = "Id, Text FROM Phrases with(NOLOCK)";
            theme_metadata.ReferenceTable = true;
            theme_metadata.SaturationDeleteField = "";
            theme_metadata.SaturationDeleteComparator = "";
            theme_metadata.SaturationDeleteType = typeof(bool);
            theme_metadata.SaturationFrequency = 30 * 1000;
            theme_metadata.SaturationPurgeField = "";
            theme_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_NONE;
            theme_metadata.SaturationPurgePercent = 0;
            theme_metadata.SaturationPurgeType = null;
            theme_metadata.SaturationUpdateField = "Id";
            theme_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            theme_metadata.SaturationUpdateType = typeof(Int64);
            theme_metadata.TableName = "Themes";
            catalog.CatalogMetadata.Tables.Add(theme_metadata.TableName, theme_metadata);
            col = new ColumnMetadata(theme_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            theme_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(theme_metadata)
            {
                ColumnName = "Text",
                ColumnType = typeof(string),
                FieldMapping = "Text",
                ColumnLength = 400,
                ColumnPrecision = 0,
                Visible = true
            };
            theme_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //tag associations
            TableMetadata ta_metadata = new TableMetadata(catalog.CatalogMetadata);
            ta_metadata.DataType = typeof(TagAssociation);
            ta_metadata.Filter = "";
            ta_metadata.MaxSaturationItems = 50000;
            ta_metadata.ModField = "InstanceId";
            ta_metadata.OrderBy = "UpdatedOn";
            ta_metadata.PrimaryKey = "Id";
            ta_metadata.Query = "Id, TagId, InstanceId, IsDisabled, UpdatedOn FROM TagAssociations with(NOLOCK)";
            ta_metadata.ReferenceTable = false;
            ta_metadata.SaturationDeleteField = "IsDisabled";
            ta_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            ta_metadata.SaturationDeleteType = typeof(bool);
            ta_metadata.SaturationDeleteValue = true;
            ta_metadata.SaturationFrequency = 30 * 1000;
            ta_metadata.SaturationPurgeField = "UpdatedOn";
            ta_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            ta_metadata.SaturationPurgePercent = 30;
            ta_metadata.SaturationPurgeType = typeof(DateTime);
            ta_metadata.SaturationUpdateField = "UpdatedOn";
            ta_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            ta_metadata.SaturationUpdateType = typeof(DateTime);
            ta_metadata.TableName = "TagAssociations";
            catalog.CatalogMetadata.Tables.Add(ta_metadata.TableName, ta_metadata);
            col = new ColumnMetadata(ta_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int32),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            ta_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(ta_metadata)
            {
                ColumnName = "TagId",
                ColumnType = typeof(Int32),
                FieldMapping = "TagId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            ta_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(ta_metadata)
            {
                ColumnName = "InstanceId",
                ColumnType = typeof(Int32),
                FieldMapping = "MentionId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            ta_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(ta_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            ta_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(ta_metadata)
            {
                ColumnName = "UpdatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "UpdatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            ta_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //datasource mentions
            TableMetadata dm_metadata = new TableMetadata(catalog.CatalogMetadata);
            dm_metadata.DataType = typeof(DatasourceMention);
            dm_metadata.Filter = "";
            dm_metadata.MaxSaturationItems = 50000;
            dm_metadata.ModField = "MentionId";
            dm_metadata.OrderBy = "UpdatedOn";
            dm_metadata.PrimaryKey = "Id";
            dm_metadata.Query = "Id, DownloadItemId, MentionId, IsDisabled, UpdatedOn, Evaluation FROM DownloadItemMentions with(NOLOCK)";
            dm_metadata.ReferenceTable = false;
            dm_metadata.SaturationDeleteField = "IsDisabled";
            dm_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            dm_metadata.SaturationDeleteType = typeof(bool);
            dm_metadata.SaturationDeleteValue = true;
            dm_metadata.SaturationFrequency = 30 * 1000;
            dm_metadata.SaturationPurgeField = "UpdatedOn";
            dm_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            dm_metadata.SaturationPurgePercent = 30;
            dm_metadata.SaturationPurgeType = typeof(DateTime);
            dm_metadata.SaturationUpdateField = "UpdatedOn";
            dm_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            dm_metadata.SaturationUpdateType = typeof(DateTime);
            dm_metadata.TableName = "DatasourceMention";
            catalog.CatalogMetadata.Tables.Add(dm_metadata.TableName, dm_metadata);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "DownloadItemId",
                ColumnType = typeof(Int32),
                FieldMapping = "DatasourceId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "MentionId",
                ColumnType = typeof(Int32),
                FieldMapping = "MentionId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "UpdatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "UpdatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(dm_metadata)
            {
                ColumnName = "Evaluation",
                ColumnType = typeof(double),
                FieldMapping = "Evaluation",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true
            };
            dm_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //theme mentions
            TableMetadata tm_metadata = new TableMetadata(catalog.CatalogMetadata);
            tm_metadata.DataType = typeof(ThemeMention);
            tm_metadata.Filter = "";
            tm_metadata.MaxSaturationItems = 5000;
            tm_metadata.ModField = "InstanceId";
            tm_metadata.OrderBy = "UpdatedOn";
            tm_metadata.PrimaryKey = "Id";
            tm_metadata.Query = "Id, InstanceId, PhraseId, IsDisabled, UpdatedOn, Evaluation FROM PhraseInstances with(NOLOCK)";
            tm_metadata.ReferenceTable = false;
            tm_metadata.SaturationDeleteField = "IsDisabled";
            tm_metadata.SaturationDeleteComparator = Comparators.EQUAL;
            tm_metadata.SaturationDeleteType = typeof(bool);
            tm_metadata.SaturationDeleteValue = true;
            tm_metadata.SaturationFrequency = 30 * 1000;
            tm_metadata.SaturationPurgeField = "UpdatedOn";
            tm_metadata.SaturationPurgeOperation = PurgeOperations.PURGE_OP_SMALLEST;
            tm_metadata.SaturationPurgePercent = 10;
            tm_metadata.SaturationPurgeType = typeof(DateTime);
            tm_metadata.SaturationUpdateField = "UpdatedOn";
            tm_metadata.SaturationUpdateComparator = Comparators.GREATER_THAN_EQUAL_TO;
            tm_metadata.SaturationUpdateType = typeof(DateTime);
            tm_metadata.TableName = "ThemeMention";
            catalog.CatalogMetadata.Tables.Add(tm_metadata.TableName, tm_metadata);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "Id",
                ColumnType = typeof(Int64),
                FieldMapping = "Id",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "PhraseId",
                ColumnType = typeof(Int64),
                FieldMapping = "ThemeId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "InstanceId",
                ColumnType = typeof(Int32),
                FieldMapping = "MentionId",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "IsDisabled",
                ColumnType = typeof(bool),
                FieldMapping = "IsDisabled",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "UpdatedOn",
                ColumnType = typeof(DateTime),
                FieldMapping = "UpdatedOn",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 0,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);
            col = new ColumnMetadata(tm_metadata)
            {
                ColumnName = "Evaluation",
                ColumnType = typeof(double),
                FieldMapping = "Evaluation",
                Nullable = false,
                ColumnLength = 0,
                ColumnPrecision = 5,
                Visible = true
            };
            tm_metadata.ColumnsMetadata.Add(col.ColumnName, col);

            //mention to tag relation
            RelationshipMetadata ta_rel = new RelationshipMetadata(catalog.CatalogMetadata);
            ta_rel.ChildField = "Id";
            ta_rel.ChildRelationshipField = "TagId";
            ta_rel.ChildTable = tag_metadata;
            ta_rel.DistinctRelationship = true;
            ta_rel.ParentChildCollection = "Tags";
            ta_rel.ParentField = "Id";
            ta_rel.ParentRelationshipField = "MentionId";
            ta_rel.ParentTable = mention_metadata;
            ta_rel.RelationshipName = "Mention_Tag";
            ta_rel.RelationTable = ta_metadata;
            catalog.CatalogMetadata.Relationships.Add(ta_rel.RelationshipName, ta_rel);

            //mention to datasource relation
            RelationshipMetadata dm_rel = new RelationshipMetadata(catalog.CatalogMetadata);
            dm_rel.ChildField = "Id";
            dm_rel.ChildRelationshipField = "DatasourceId";
            dm_rel.ChildTable = datasource_metadata;
            dm_rel.DistinctRelationship = true;
            dm_rel.ParentChildCollection = "Datasources";
            dm_rel.ParentField = "Id";
            dm_rel.ParentRelationshipField = "MentionId";
            dm_rel.ParentTable = mention_metadata;
            dm_rel.RelationshipName = "Mention_Datasource";
            dm_rel.RelationTable = dm_metadata;
            catalog.CatalogMetadata.Relationships.Add(dm_rel.RelationshipName, dm_rel);

            //mention to theme relation
            RelationshipMetadata tm_rel = new RelationshipMetadata(catalog.CatalogMetadata);
            tm_rel.ChildField = "Id";
            tm_rel.ChildRelationshipField = "ThemeId";
            tm_rel.ChildTable = theme_metadata;
            tm_rel.DistinctRelationship = false;
            tm_rel.ParentChildCollection = "Themes";
            tm_rel.ParentField = "Id";
            tm_rel.ParentRelationshipField = "MentionId";
            tm_rel.ParentTable = mention_metadata;
            tm_rel.RelationshipName = "Mention_Theme";
            tm_rel.RelationTable = tm_metadata;
            catalog.CatalogMetadata.Relationships.Add(tm_rel.RelationshipName, tm_rel);

            return catalog;
        }
    }
}
