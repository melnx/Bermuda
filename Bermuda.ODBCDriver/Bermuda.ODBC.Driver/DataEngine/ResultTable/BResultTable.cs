using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using System.Reflection;
using Bermuda.ODBC.Driver.DataEngine.Metadata;
using Bermuda.Interface.Connection.External;
using Bermuda.ExpressionGeneration;

namespace Bermuda.ODBC.Driver.DataEngine.ResultTable
{
    class BResultTable : DSISimpleResultSet
    {

        #region Fields

        /// <summary>
        /// The table columns.
        /// </summary>
        private IList<IColumn> m_Columns = new List<IColumn>();

        /// <summary>
        /// The table data.
        /// </summary>
        private IList<object> m_Data = new List<object>();

        /// <summary>
        /// the driver properties from connections string
        /// </summary>
        protected BProperties m_Properties { get; set; }

        /// <summary>
        /// the sql to execute
        /// </summary>
        string Sql { get; set; }

        /// <summary>
        /// the current page you are fetching
        /// </summary>
        long m_CurrentPage = 0;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">The logger to use for logging.</param>
        public BResultTable(ILogger log, string sql, BProperties properties)
            : base(log)
        {
            //get the parameters
            LogUtilities.LogFunctionEntrance(Log, log);
            m_Properties = properties;
            Sql = sql;

            //execute the first fetch
            ExecuteFetch(m_CurrentPage);
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// Retrieve the columns that represent the data provided in the result set. Even if there 
        /// are no rows in the result set, the columns should still be accurate. 
        /// 
        /// The position in the returned list should match the position in the result set. 
        /// The first column should be found at position 0, the second at 1, etc...
        /// </summary>
        public override IList<IColumn> SelectColumns
        {
            get { return m_Columns; }
        }

        /// <summary>
        /// the result type for objects in data
        /// </summary>
        private Type ResultEntityType { get; set; }

        /// <summary>
        /// the list of fields
        /// </summary>
        private List<FieldInfo> ResultFields { get; set; }


        #endregion // Properties

        #region Methods

        /// <summary>
        /// execute the proper fetch
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public bool ExecuteFetch(long page)
        {
            bool ret = false;
            try
            {
                //open the client connection
                using (var client = ExternalServiceClient.GetClient(m_Properties.Server))
                {
                    //temp parameters until we get SQL TO BQL
                    //var mapreduce = "SELECT Count() GROUP BY Date INTERVAL Day";
                    var mapreduce = Sql;
                    //var mapreduce = "Get Mentions";
                    var minDate = DateTime.UtcNow.AddDays(-100);
                    var maxDate = DateTime.UtcNow;
                    string query = "";
                    //string command = "-nocount";
                    string command = "";

                    //create the paging string
                    //??how do I specify the page??
                    string paging = "";// string.Format("__paging__ORDERED BY Date LIMIT {0}", m_Properties.RowsToFetch);

                    //init the data
                    m_Data.Clear();

                    //make the query
                    //var cursor = client.GetCursor(m_Properties.Catalog, "__ql__" + query, "__ql__" + mapreduce, "__default__", null, command);
                    var datapoints = client.GetData(m_Properties.Catalog, "__ql__" + query, "__ql__" + mapreduce, "__default__", null, command);
                    //var datapoints = client.GetData(m_Properties.Catalog, "__ql__" + query, null, null, paging, minDate, maxDate, command);

                    //this is the call to pass the sql through to bermuda
                    //var datapoints = client.GetData(Sql);

                    //check results
                    if (datapoints != null && !string.IsNullOrWhiteSpace(datapoints.Data) && !string.IsNullOrWhiteSpace(datapoints.DataType))
                    {
                        //build the returned data
                        object obj = LinqRuntimeTypeBuilder.DeserializeJson(datapoints.Data, datapoints.DataType, true);
                        Type type = obj.GetType();
                        Array array = obj as Array;

                        //check for results returned
                        if (array.Length > 0)
                        {
                            //parse the results
                            foreach (var item in array)
                            {
                                //check if we need to init the columns
                                if (m_Columns.Count == 0)
                                {
                                    //init the columns
                                    InitializeColumns(item);
                                }
                                //add this row
                                AddRow(item);
                            }
                        }
                        //set the actual row size
                        //RowCount = TotalItemsToFetch;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return ret;
        }

        /// <summary>
        /// Fills in out_data with the data for a given column in the current row.
        /// </summary>
        /// <param name="column">The column to retrieve data from, 0 based.</param>
        /// <param name="offset">The number of bytes in the data to skip before copying.</param>
        /// <param name="maxSize">The maximum number of bytes of data to return.</param>
        /// <param name="out_data">The data to be returned.</param>
        /// <returns>True if there is more data in the column; false otherwise.</returns>
        public override bool GetData(
            int column,
            long offset,
            long maxSize,
            out object out_data)
        {
            LogUtilities.LogFunctionEntrance(Log, column, offset, maxSize, "out_data");

            //check paging cache
            if (CurrentRow < m_CurrentPage * m_Properties.RowsToFetch || CurrentRow > ((m_CurrentPage + 1) * m_Properties.RowsToFetch) - 1)
            {
                //set the current page
                m_CurrentPage = CurrentRow / (long)m_Properties.RowsToFetch;

                //fetch next set of results
                ExecuteFetch(m_CurrentPage);
            }
            //calculate the paging row
            long PageRow = CurrentRow % m_Properties.RowsToFetch;

            //get the object
            object obj = m_Data[(int)PageRow];
            
            //get the value
            object value = ResultFields[column].GetValue(obj);

            //check for boolean
            if (value != null && value.GetType() == typeof(bool))
            {
                out_data = Convert.ToByte(value);
            }
            else
            {
                out_data = value;
            }

            //throw new Exception("testing");

            return false;
        }

        /// <summary>
        /// Closes the result set's internal cursor. After a call to this method, no
        /// more calls will be made to MoveToNextRow() and GetData().
        /// </summary>
        protected override void DoCloseCursor()
        {
            LogUtilities.LogFunctionEntrance(Log);
        }

        /// <summary>
        /// Indicates that the cursor should be moved to the next row.
        /// </summary>
        /// <returns>True if there are more rows; false otherwise.</returns>
        protected override bool MoveToNextRow()
        {
            LogUtilities.LogFunctionEntrance(Log);

            return (CurrentRow < RowCount);
        }

        /// <summary>
        /// Initialize the column metadata for the result set.
        /// </summary>
        public void InitializeColumns(object obj)
        {
            LogUtilities.LogFunctionEntrance(Log);

            //get the info the build the columns
            ResultEntityType = obj.GetType();
            ResultFields = ResultEntityType.GetFields().ToList();

            //fields to remove that we do not want in results
            List<FieldInfo> remove = new List<FieldInfo>();

            //parse the fields
            foreach(var field in ResultFields)
            {
                //make sure this not collection type
                if (field.FieldType.GetInterface("ICollection") != null)
                {
                    //remove this field
                    remove.Add(field);
                }
                else
                {
                    //create the column
                    DSIColumn column = new DSIColumn(TypeMetadataHelper.CreateTypeMetadata(field.FieldType));
                    column.IsNullable = Nullability.Nullable;
                    column.Catalog = m_Properties.Catalog;
                    column.Schema = Driver.B_SCHEMA;
                    column.TableName = "Results";
                    column.Name = field.Name;
                    column.Label = column.Name;
                    if (field.FieldType == typeof(string))
                    {
                        column.Size = 1000;
                        column.IsSearchable = Searchable.Searchable;
                    }
                    else
                        column.IsSearchable = Searchable.PredicateBasic;
                    m_Columns.Add(column);
                }
            }
            //remove the fields
            foreach (var field in remove)
            {
                ResultFields.Remove(field);
            }
        }

        /// <summary>
        /// add a row
        /// </summary>
        /// <param name="obj"></param>
        public void AddRow(object obj)
        {
            m_Data.Add(obj);
            RowCount = m_Data.Count;
        }

        #endregion // Methods
    }
}
