using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using Bermuda.ODBC.Driver.DataEngine.Metadata;
using Bermuda.Interface;
using Bermuda.Interface.Connection.External;

namespace Bermuda.ODBC.Driver.DataEngine.ResultTable
{
    class BFakeResultTable : BResultTable
    {

        #region Fields

        /// <summary>
        /// The table columns.
        /// </summary>
        private IList<IColumn> m_Columns = new List<IColumn>();

        /// <summary>
        /// The table data.
        /// </summary>
        private IList<List<object>> m_Data = new List<List<object>>();

        /// <summary>
        /// the table columns to help with result column types
        /// </summary>
        ColumnMetadataResult[] m_TableColumns;

        /// <summary>
        /// the sql to execute
        /// </summary>
        string Sql { get; set; }

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">The logger to use for logging.</param>
        public BFakeResultTable(ILogger log, string sql, BProperties properties)
            : base(log, sql, properties)
        {
            //set parameters
            LogUtilities.LogFunctionEntrance(Log, log);
            m_Properties = properties;
            Sql = sql;

            //init the columns
            try
            {
                using (var client = ExternalServiceClient.GetClient(m_Properties.Server))
                {
                    m_TableColumns = client.GetMetadataColumns();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            //make the fake data
            InitializeFakeData(Sql);
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

        #endregion // Properties

        #region Methods

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

            out_data = null;
            try
            {
                out_data = m_Data[(int)CurrentRow][column];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());

            }
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
        /// Initialize the fake data
        /// </summary>
        public void InitializeFakeData(string sql)
        {
            LogUtilities.LogFunctionEntrance(Log);

            List<object> fake_row = new List<object>();

            string sql_no_select = sql.Substring(sql.IndexOf("SELECT ") + 7);
            string sql_no_top = sql_no_select;
            if (sql_no_select.IndexOf("TOP ") != -1)
            {
                sql_no_top = sql_no_select.Substring(sql_no_select.IndexOf("TOP ") + 4).Trim();
                sql_no_top = sql_no_top.Substring(sql_no_top.IndexOf(" ") + 1).Trim();
            }
            string sql_no_from = sql_no_top.Substring(0, sql_no_top.IndexOf("FROM "));
            string[] columns_with_alias = sql_no_from.Split(',');

            foreach (var field_alias in columns_with_alias)
            {
                string[] aliases = field_alias.Split(new string[] { " AS ", " as " }, StringSplitOptions.None);

                string field = "";
                string alias = "";
                if (aliases.Length > 0)
                    field = aliases[0].Trim();
                if (aliases.Length > 1)
                    alias = aliases[1].Trim();

                field = field.Replace("\"", "");
                alias = alias.Replace("\"", "");

                string table = "";
                if (field.IndexOf('.') != -1)
                {
                    table = field.Substring(0, field.IndexOf('.'));
                    field = field.Substring(field.IndexOf('.') + 1);

                }

                var column_meta = m_TableColumns.Where(c => c.Table == table && c.Column == field).FirstOrDefault();
                Type type;
                bool added = false;
                if (column_meta != null)
                {
                    type = Type.GetType(column_meta.DataType);
                    
                }
                else
                {
                    double num;
                    if (double.TryParse(field, out num))
                    {
                        type = typeof(double);
                        fake_row.Add(num);
                        added = true;
                    }
                    else
                    {
                        type = typeof(string);
                        fake_row.Add(field);
                    }
                }
                if (!added)
                {
                    if (type == typeof(string))
                    {
                        fake_row.Add("FakeData");
                    }
                    else if (type == typeof(DateTime))
                    {
                        fake_row.Add(DateTime.UtcNow);
                    }
                    else if (type == typeof(bool))
                    {
                        byte b = 0;
                        fake_row.Add(b);
                    }
                    else
                    {
                        fake_row.Add(Activator.CreateInstance(type));
                    }
                }


                DSIColumn column = new DSIColumn(TypeMetadataHelper.CreateTypeMetadata(type));
                column.IsNullable = Nullability.Nullable;
                column.Catalog = m_Properties.Catalog;
                column.Schema = Driver.B_SCHEMA;
                column.TableName = "Results";
                column.Name = alias;
                column.Label = alias;
                if (type == typeof(string))
                {
                    column.Size = 1000;
                    column.IsSearchable = Searchable.Searchable;
                }
                else
                    column.IsSearchable = Searchable.PredicateBasic;
                m_Columns.Add(column);
            }
            RowCount = 10;
            for(int x=0;x<RowCount;x++)
                m_Data.Add(fake_row);
            
        }

        #endregion // Methods
    }
}
