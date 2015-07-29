using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simba.DotNetDSI.DataEngine;
using Simba.DotNetDSI;
using System.Diagnostics;
using Bermuda.Interface.Connection.External;
using Bermuda.Interface;
using Bermuda.ExpressionGeneration;
using Bermuda.ODBC.Driver.DataEngine.ResultTable;

namespace Bermuda.ODBC.Driver.DataEngine
{
    public class BQueryExecutor : IQueryExecutor
    {
        #region Fields

        /// <summary>
        /// Properties
        /// </summary>
        private BProperties m_Properties { get; set; }

        #endregion

        #region Constructor

        public BQueryExecutor(ILogger log, BProperties properties, string sql)
        {
            //get function data
            LogUtilities.LogFunctionEntrance(log, log, sql);
            Log = log;
            m_Properties = properties;
            Sql = sql;

            // Create the prepared results.
            Results = new List<IResult>();
            
            // Create the parameters.
            ParameterMetadata = new List<ParameterMetadata>();

            //create our data
            IResult result = BResultTableFactory.CreateResultTable(log, sql, properties);
            if (result != null)
            {
                Results.Add(result);
            }
            else
            {
                throw new Exception("Failed to create the result table.");
            }
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// the sql statement to execute
        /// </summary>
        private string Sql { get; set; }

        /// <summary>
        /// Get or set the logger to use for the query executor.
        /// </summary>
        private ILogger Log
        {
            get;
            set;
        }

        /// <summary>
        /// Get the metadata for the parameters in the query.
        /// 
        /// The position of the ParameterMetadata in the list should match their position in the
        /// query, so parameter 1 should be at position 0, parameter 2 should be at position 1, 
        /// etc...
        /// 
        /// Even if parameters are named instead of numbered, this positioning should be 
        /// maintained. Positions should correlate with the parameter numbers in the 
        /// ParameterMetadata objects.
        /// </summary>
        public IList<ParameterMetadata> ParameterMetadata
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the results of query.
        /// 
        /// Results may be fetched before query execution but only column metadata will be available
        /// if possible. Other operations should throw exceptions.
        /// </summary>
        public IList<IResult> Results
        {
            get;
            private set;
        }

        #endregion // Properties

        #region Methods

        

        /// <summary>
        /// Cancels the currently executing query.
        /// </summary>
        public void CancelExecute()
        {
            LogUtilities.LogFunctionEntrance(Log);
           
            // It's not possible to cancel execution in the UltraLight driver, as there is no actual
            // 'execution', everything is already hardcoded within the driver.
        }

        /// <summary>
        /// Clears any state that might have been set by CancelExecute().
        /// </summary>
        public void ClearCancel()
        {
            LogUtilities.LogFunctionEntrance(Log);
        }

        /// <summary>
        /// Clears any parameter data that has been pushed down using PushParamData(). The 
        /// ULQueryExecutor may be re-used for execution following this call.
        /// </summary>
        public void ClearPushedParamData()
        {
            LogUtilities.LogFunctionEntrance(Log);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting 
        /// unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            ; // Do nothing.
        }

        /// <summary>
        /// Executes the prepared statement for each parameter set provided, or once if there are
        /// no parameters supplied.
        /// </summary>
        /// <param name="contexts">
        ///     Holds ExecutionContext objects and parameter metadata for execution. There is one
        ///     ExecutionContext for each parameter set to be executed.
        /// </param>
        /// <param name="warningListener">Used to post warnings about the execution.</param>
        public void Execute(ExecutionContexts contexts, IWarningListener warningListener)
        {
            LogUtilities.LogFunctionEntrance(Log, contexts, warningListener);

            //mark all context as success
            foreach (ExecutionContext context in contexts)
                context.Succeeded = true;
            


        }

        /// <summary>
        /// Informs the ULQueryExecutor that all parameter values which will be pushed have been 
        /// pushed prior to query execution. After the next Execute() call has finished, this pushed 
        /// parameter data may be cleared from memory, even if the Execute() call results in an 
        /// exception being thrown.
        /// 
        /// The first subsequent call to PushParamData() should behave as if the executor has a 
        /// clear cache of pushed parameter values.
        /// </summary>
        public void FinalizePushedParamData()
        {
            LogUtilities.LogFunctionEntrance(Log);
        }

        /// <summary>
        /// Pushes part of an input parameter value down before execution. This value
        /// should be stored for use later during execution.
        /// 
        /// This method may only be called once for any parameter set/parameter
        /// combination (a "parameter cell") where the parameter has a
        /// non-character/binary data type.
        /// 
        /// For parameters with character or binary data types, this method may be
        /// called multiple times for the same parameter set/parameter combination.
        /// The multiple parts should be concatenated together in order to get the
        /// complete value. For character data, the byte array passed down for one
        /// chunk may NOT necessarily be a complete UTF-8 string representation.
        /// There may be bytes provided in the previous or subsequent chunk to
        /// complete codepoints at the start and/or end.
        /// 
        /// The metadata passed in should be taken notice of because it may not match
        /// metadata supplied by a possible call to GetMetadataForParameters(), as
        /// the ODBC consumer is able to change parameter metadata themselves.
        /// </summary>
        /// <param name="parameterSet">The parameter set the pushed value belongs to.</param>
        /// <param name="value">The pushed parameter value, including metadata for identification.</param>
        public void PushParamData(int parameterSet, ParameterInputValue value)
        {
            LogUtilities.LogFunctionEntrance(Log, parameterSet, value);

            // This is where pushed parameter data would be cached, to be used during execution.
        }

        #endregion // Methods
    }
}

    