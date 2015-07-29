using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Constants;
using System.Reflection;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class ReferenceDataTable : DataTable, IReferenceDataTable
    {
        #region Variables and Properties

        /// <summary>
        /// the parent catalog
        /// </summary>
        public ICatalog Catalog { get; set; }

        /// <summary>
        /// the last time this table/bucket was saturated
        /// </summary>
        public DateTime LastSaturation { get; set; }

        /// <summary>
        /// calculate the next saturation update
        /// </summary>
        public DateTime NextSaturation
        {
            get
            {
                return LastSaturation.AddMilliseconds(TableMetadata.SaturationFrequency);
            }
        }

        /// <summary>
        /// the last value found to update staurated data
        /// </summary>
        public object LastUpdateValue { get; set; }

        /// <summary>
        /// the bucket data table is currently saturating
        /// </summary>
        public bool Saturating { get; set; }

        /// <summary>
        /// this data table has been saturated
        /// </summary>
        public bool Saturated { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor with parent and table definition
        /// </summary>
        /// <param name="bucket"></param>
        public ReferenceDataTable(ICatalog catalog, ITableMetadata table_metadata)
            :base(table_metadata)
        {
            Catalog = catalog;
            Saturating = false;
            Saturated = false;
            LastSaturation = DateTime.UtcNow.AddDays(-1);
            if (table_metadata.SaturationUpdateType == null)
                LastUpdateValue = null;
            else if (table_metadata.SaturationUpdateType == typeof(DateTime))
                LastUpdateValue = new DateTime(1970, 1, 1);
            else if (table_metadata.SaturationUpdateType == typeof(string))
                LastUpdateValue = "";
            else
                LastUpdateValue = Convert.ChangeType(0, table_metadata.SaturationUpdateType);
        }

        #endregion

        #region Methods

        /// <summary>
        /// construct the reference table query
        /// </summary>
        /// <returns></returns>
        public virtual string ConstructQuery()
        {
            //base query
            StringBuilder sb = new StringBuilder("SELECT");

            //handle top
            if (TableMetadata.MaxSaturationItems == 0)
                sb.AppendFormat(" {0}", TableMetadata.Query);
            else if(TableMetadata.MaxSaturationItems > 0)
                sb.AppendFormat(" TOP {0} {1}", TableMetadata.MaxSaturationItems, TableMetadata.Query);
            else
                sb.AppendFormat(" TOP {0} {1}", 0, TableMetadata.Query);

            //mod
            sb.AppendFormat(" WHERE 1 = 1");

            //base filter
            if (!string.IsNullOrWhiteSpace(TableMetadata.Filter))
                sb.AppendFormat(" AND ({0})", TableMetadata.Filter);

            //update value
            if (this.TableMetadata.SaturationUpdateType == typeof(DateTime))
            {
                sb.AppendFormat(
                    " AND {0} {1} '{2} {3}'",
                    TableMetadata.SaturationUpdateField,
                    TableMetadata.SaturationUpdateComparator,
                    ((DateTime)LastUpdateValue).ToShortDateString(),
                    ((DateTime)LastUpdateValue).ToShortTimeString());
            }
            else if (this.TableMetadata.SaturationUpdateType == typeof(string))
            {
                sb.AppendFormat(
                    " AND {0} {1} '{2}'",
                    TableMetadata.SaturationUpdateField,
                    TableMetadata.SaturationUpdateComparator,
                    LastUpdateValue.ToString());
            }
            else
            {
                sb.AppendFormat(
                    " AND {0} {1} {2}",
                    TableMetadata.SaturationUpdateField,
                    TableMetadata.SaturationUpdateComparator,
                    LastUpdateValue.ToString());
            }
            //order by
            if (!string.IsNullOrWhiteSpace(TableMetadata.OrderBy))
                sb.AppendFormat(" ORDER BY {0}", TableMetadata.OrderBy);

            return sb.ToString();
        }

        /// <summary>
        /// update the last value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool UpdateLastValue(object obj)
        {
            //check for null
            if (obj == null)
                return false;

            //check for init last value
            if (LastUpdateValue == null)
            {
                LastUpdateValue = obj;
                return true;
            }
            //value was updated
            bool bUpdated = false;

            try
            {
                //compare the objects
                int comparison = (Convert.ChangeType(obj, LastUpdateValue.GetType()) as IComparable).CompareTo(LastUpdateValue);

                //handle update logic
                switch (TableMetadata.SaturationUpdateComparator)
                {
                    case Comparators.GREATER_THAN:
                        if (comparison > 0)
                        {
                            LastUpdateValue = obj;
                            bUpdated = true;
                        }
                        break;

                    case Comparators.GREATER_THAN_EQUAL_TO:
                        if (comparison >= 0)
                        {
                            LastUpdateValue = obj;
                            bUpdated = true;
                        }
                        break;

                    case Comparators.LESS_THAN:
                        if (comparison < 0)
                        {
                            LastUpdateValue = obj;
                            bUpdated = true;
                        }
                        break;

                    case Comparators.LESS_THAN_EQUAL_TO:
                        if (comparison >= 0)
                        {
                            LastUpdateValue = obj;
                            bUpdated = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return bUpdated;
        }

        /// <summary>
        /// determine if an item is deleted
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsDeleted(object item)
        {
            try
            {
                //check for valid delete info
                if (string.IsNullOrWhiteSpace(TableMetadata.SaturationDeleteField) || string.IsNullOrWhiteSpace(TableMetadata.SaturationDeleteComparator))
                    return false;

                //get the field info
                FieldInfo field = item.GetType().GetField(TableMetadata.SaturationDeleteField);

                //check the field
                if (field == null)
                    return false;

                //get the field value
                object value = field.GetValue(item);
                if (value == null)
                    return false;

                //check the item
                int compare = (value as IComparable).CompareTo(TableMetadata.SaturationDeleteValue);
                switch (TableMetadata.SaturationDeleteComparator)
                {
                    case Comparators.EQUAL:
                        if (compare == 0)
                            return true;
                        break;
                    case Comparators.GREATER_THAN:
                        if (compare > 1)
                            return true;
                        break;
                    case Comparators.GREATER_THAN_EQUAL_TO:
                        if (compare >= 0)
                            return true;
                        break;
                    case Comparators.LESS_THAN:
                        if (compare < 0)
                            return true;
                        break;
                    case Comparators.LESS_THAN_EQUAL_TO:
                        if (compare <= 0)
                            return true;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// return if we can saturate this table
        /// </summary>
        /// <returns></returns>
        public virtual bool CanSaturate()
        {
            return true;
        }

        #endregion
    }
}
