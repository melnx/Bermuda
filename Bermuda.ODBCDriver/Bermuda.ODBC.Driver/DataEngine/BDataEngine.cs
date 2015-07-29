using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using Bermuda.ODBC.Driver.DataEngine.Metadata;

namespace Bermuda.ODBC.Driver.DataEngine
{
    class BDataEngine : DSIDataEngine
    {
        #region Fields

        /// <summary>
        /// the driver properties from connections string
        /// </summary>
        private BProperties m_Properties { get; set; }

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="statement">The parent statement of the data engine.</param>
        public BDataEngine(IStatement statement, BProperties properties) : base(statement)
        {
            LogUtilities.LogFunctionEntrance(Statement.Connection.Log, statement);
            m_Properties = properties;
        }

        #endregion // Constructor

        #region Methods

        /// <summary>
        /// Creates a custom metadata source which filters out unneeded rows according to the given
        /// filters.
        /// 
        /// Note that filtering must be implemented by the custom result set.
        /// 
        /// This function works in conjunction with SConnection.GetCustomSchemas().
        /// Override GetCustomSchemas() in SConnection to return the list of custom schemas.
        /// Note that this is only available when using the Simba.NET components.
        /// </summary>
        /// <param name="metadataID">Identifier to create the appropriate metadata source.</param>
        /// <param name="filterValues">
        ///     Filters to be applied to the metadata table. Appear in column order, with null
        ///     values for columns that are not filtered on.
        /// </param>
        /// <param name="escapeChar">Escape character used in filtering.</param>
        /// <param name="identifierQuoteChar">Character used as a quote around identifiers.</param>
        /// <param name="filterAsIdentifier">Indicates if string filters are treated as identifiers.</param>
        /// <returns>A result set object representing the requested metadata.</returns>
        public override IResultSet MakeNewCustomMetadataResult(
            string metadataID,
            IList<object> filterValues,
            string escapeChar,
            string identifierQuoteChar,
            bool filterAsIdentifier)
        {
            
            LogUtilities.LogFunctionEntrance(
                Log,
                metadataID,
                filterValues,
                escapeChar,
                identifierQuoteChar,
                filterAsIdentifier);


            throw ExceptionBuilder.CreateException(
                Simba.DotNetDSI.Properties.Resources.INVALID_METADATA_ID,
                metadataID);
        }

        /// <summary>
        /// Creates a metadata source which filters out unneeded rows according to the given
        /// filters.
        /// </summary>
        /// <param name="metadataID">Identifier to create the appropriate metadata source.</param>
        /// <param name="restrictions">
        ///     Restrictions to be applied to the metadata table. Only columns that have 
        ///     restrictions appear in the collection of restrictions.
        /// </param>
        /// <param name="escapeChar">Escape character used in filtering.</param>
        /// <param name="identifierQuoteChar">Character used as a quote around identifiers.</param>
        /// <param name="filterAsIdentifier">Indicates if string filters are treated as identifiers.</param>
        /// <returns>An IMetadataSource object representing the requested metadata.</returns>
        public override IMetadataSource MakeNewMetadataSource(
            MetadataSourceID metadataID,
            IDictionary<MetadataSourceColumnTag, string> restrictions,
            string escapeChar,
            string identifierQuoteChar,
            bool filterAsIdentifier)
        {
            // TODO(ODBC) #05: Create and return your Metadata Sources.
            // TODO(ADO)  #07: Create and return your Metadata Sources.
            LogUtilities.LogFunctionEntrance(
                Log, 
                metadataID, 
                restrictions, 
                escapeChar, 
                identifierQuoteChar, 
                filterAsIdentifier);

            // At the very least, ODBC conforming applications will require the following metadata 
            // sources:
            //
            //  Tables
            //      List of all tables defined in the data source.
            //
            //  CatalogOnly
            //      List of all catalogs defined in the data source.
            //
            //  SchemaOnly
            //      List of all schemas defined in the data source.
            //
            //  TableTypeOnly
            //      List of all table types (TABLE,VIEW,SYSTEM) defined within the data source.
            //
            //  Columns
            //      List of all columns defined across all tables in the data source.
            //
            //  TypeInfo
            //      List of the supported types by the data source.
            //
            //  In some cases applications may provide values to restrict the metadata sources.
            //  These restrictions are stored within restrictions and can be used to restrict
            //  the number of rows that are returned from the metadata source.

            // Columns, Tables, CatalogOnly, SchemaOnly, TableTypeOnly, TypeInfo.
            switch (metadataID)
            {
                case MetadataSourceID.Tables:
                {
                    return new BTablesMetadataSource(Log, m_Properties);
                }

                case MetadataSourceID.CatalogOnly:
                {
                    return new BCatalogOnlyMetadataSource(Log, m_Properties);
                }

                case MetadataSourceID.SchemaOnly:
                {
                    return new BSchemaOnlyMetadataSource(Log);
                }

                case MetadataSourceID.TableTypeOnly:
                {
                    return new DSITableTypeOnlyMetadataSource(Log);
                }

                case MetadataSourceID.Columns:
                {
                    return new BColumnsMetadataSource(Log, m_Properties);
                }

                case MetadataSourceID.TypeInfo:
                {
                    return new BTypeInfoMetadataSource(Log);
                }

                default:
                {
                    return new DSIEmptyMetadataSource();
                }
            }
        }

        /// <summary>
        /// Prepare the given SQL query for execution.
        /// 
        /// An IQueryExecutor is returned which will be used to handle query execution.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to prepare.</param>
        /// <returns>An IQueryExecutor instance that will handle the query execution.</returns>
        public override IQueryExecutor Prepare(string sqlQuery)
        {
            

            // TODO(ODBC) #06: Prepare a query.
            // TODO(ADO)  #08: Prepare a query.
            LogUtilities.LogFunctionEntrance(Log, sqlQuery);

            // This is the point where you will send the request to your SQL-enabled data source for
            // query preparation. You will need to provide your own implementation of IQueryExecutor
            // which should wrap your statement context to the prepared query.
            //
            // Query preparation is really a 3 part process and is described as follows:
            //      1. Generate and send the request to your data source for query preparation.
            //      2. Handle the response and for each statement in the query retrieve its column 
            //         metadata information prior to query execution.  You will need to derive from 
            //         DSISimpleResultSet to create your representation of a result set.  
            //         See ULPersonTable.
            //      3. Create an instance of IQueryExector seeding it with the results of the query.
            //         See ULQueryExecutor.

            // Determine if doing a SELECT or DML/DDL via very, very simple parsing.
            //string query = sqlQuery.ToLower();

            //bool isSelect = (-1 != query.IndexOf("select"));
            //bool isParameterized = (-1 != query.IndexOf("?"));
            //bool isProcedure = (-1 != query.IndexOf("{call"));

            //// Example of how to throw a parsing error.
            //if (isProcedure)
            //{
            //    throw ExceptionBuilder.CreateException(
            //        String.Format(Simba.DotNetDSI.Properties.Resources.INVALID_QUERY, query));
            //}

            //trace the execute
            System.Diagnostics.Trace.WriteLine(string.Format("**********************\r\nQueryExecution\r\n{0}\r\n***************************", sqlQuery));

            return new BQueryExecutor(Log, m_Properties, sqlQuery);
        }

        #endregion // Methods
    }
}


 