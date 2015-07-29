using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bermuda.Interface;
using System.Collections.Concurrent;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Bermuda.Catalog
{
    public class DataTable : IDataTable
    {

        #region Variables and Properties

        /// <summary>
        /// the table metadata for this table
        /// </summary>
        public ITableMetadata TableMetadata { get; set; }

        /// <summary>
        /// the table of data items
        /// </summary>
        //public ConcurrentDictionary<Int64, IDataItem> DataItems { get; set; }
        public IDictionary DataItems { get; set; }

        /// <summary>
        /// the bucket data table is currently purging
        /// </summary>
        public bool Purging { get; set; }

        /// <summary>
        /// the last time this table/bucket was purged
        /// </summary>
        public DateTime LastPurge { get; set; }

        /// <summary>
        /// cache the add or update delegate
        /// </summary>
        private ConcurrentDictionary<Type, Delegate> AddOrUpdateDelegateCache { get; set; }

        /// <summary>
        /// cache the method info for add or update
        /// </summary>
        private ConcurrentDictionary<Type, MethodInfo> AddOrUpdateMethodCache { get; set; }

        /// <summary>
        /// cache the method info for try get
        /// </summary>
        private ConcurrentDictionary<Type, MethodInfo> TryGetValueMethodCache { get; set; }

        /// <summary>
        /// cache the method info for try remove
        /// </summary>
        private ConcurrentDictionary<Type, MethodInfo> TryRemoveMethodCache { get; set; }


        private Delegate SelectExpression { get; set; }

        private MethodInfo SelectInvoke { get; set; }

        #endregion

        #region Constructor

        public DataTable(ITableMetadata table_metadata)
        {
            TableMetadata = table_metadata;
            
            //create the concurrent dictionary with the actual type
            var type = typeof(ConcurrentDictionary<,>);
            Type[] type_args = {typeof(Int64), table_metadata.DataType};
            var constructed_type = type.MakeGenericType(type_args);
            DataItems = (IDictionary)Activator.CreateInstance(constructed_type);

            //caches for reflection
            AddOrUpdateDelegateCache = new ConcurrentDictionary<Type, Delegate>();
            AddOrUpdateMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
            TryGetValueMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
            TryRemoveMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// delete an item given its catalog table
        /// </summary>
        /// <param name="item"></param>
        /// <param name="table"></param>
        public void DeleteItem(IDataItem item, bool HardDelete)
        {
            //the table to call delete item with
            IReferenceDataTable delete_table = null;

            //check if this is a reference table
            if (TableMetadata.ReferenceTable)
            {
                //get the reference table
                delete_table = (IReferenceDataTable)this;
            }
            else
            {
                //get the mod field
                IColumnMetadata column = TableMetadata.ColumnsMetadata[TableMetadata.ModField];

                //get the value to mode
                object obj = item.GetType().GetField(column.FieldMapping).GetValue(item);
                Int64 id = Convert.ToInt64(obj);

                //mod to get the bucket index
                Int64 bucket = id % TableMetadata.CatalogMetadata.Catalog.ComputeNode.GlobalBucketCount;

                //get the bucket table
                delete_table = TableMetadata.CatalogMetadata.Catalog.Buckets[bucket].BucketDataTables[TableMetadata.TableName];
            }
            //call delete
            if (delete_table != null)
                DeleteItem(item, delete_table, HardDelete);
        }

        /// <summary>
        /// delete an item and its references
        /// </summary>
        /// <param name="item"></param>
        /// <param name="table"></param>
        private void DeleteItem(IDataItem item, IReferenceDataTable table, bool HardDelete)
        {
            //remove from table
            object objTable;
            object objCatalog;
            IDataItem removed;
            
            //check we need to handle relationship
            if (table is RelationshipDataTable)
            {
                //parse the table's relationships
                RelationshipDataTable rel_table = table as RelationshipDataTable;

                //get the fields
                long parent_id = Convert.ToInt64(item.GetType().GetField(rel_table.RelationshipMetadata.ParentRelationshipField).GetValue(item));
                long child_id = Convert.ToInt64(item.GetType().GetField(rel_table.RelationshipMetadata.ChildRelationshipField).GetValue(item));

                //lock the resource
                lock (rel_table.RelationshipParentLookup)
                {
                    //remove the item
                    if (table.TryRemove(item.PrimaryKey, table.TableMetadata.DataType, out objTable))
                    {
                        removed = (IDataItem)objTable;
                    }
                    //check if this is a hard delete
                    //HardDelete removes the relations
                    //SoftDelete leave the references
                    if (HardDelete)
                    {
                        //remove this relation
                        ConcurrentDictionary<long, long> lookup;
                        if (rel_table.RelationshipParentLookup.TryGetValue(parent_id, out lookup))
                        {
                            //remove all lookup entries for association
                            long id;
                            bool found = true;
                            while (found)
                                found = lookup.TryRemove(item.PrimaryKey, out id);

                            if (lookup.Count == 0)
                            {
                                //remove the lookup for this parent
                                if (rel_table.RelationshipParentLookup.TryRemove(parent_id, out lookup))
                                {
                                    //lookup.Clear();
                                    //lookup = null;
                                }
                            }
                        }
                        //get the parent item
                        object objParent;
                        IDataItem parent_item;
                        //dynamic parent_item;
                        IBucketDataTable parent_table = rel_table.Bucket.BucketDataTables[rel_table.RelationshipMetadata.ParentTable.TableName];
                        if (parent_table.TryGetValue(parent_id, parent_table.TableMetadata.DataType, out objParent))
                        {
                            parent_item = (IDataItem)objParent;
                            //parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).SetValue(parent_item, rel_table.GetChildList(parent_id));
                            var list = (List<Tuple<List<long>,long>>)parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).GetValue(parent_item);
                            if (rel_table.RelationshipMetadata.DistinctRelationship)
                            {
                                var rel_item = list.FirstOrDefault(t => t.Item2 == child_id);
                                if (rel_item != null)
                                {
                                    if (rel_item.Item1.Contains(item.PrimaryKey))
                                        rel_item.Item1.Remove(item.PrimaryKey);
                                    if (rel_item.Item1.Count == 0)
                                        list.Remove(rel_item);
                                }
                            }
                            else
                            {
                                var rel_item = list.FirstOrDefault(t => t.Item1.Any(l => l == item.PrimaryKey));
                                if (rel_item != null)
                                    list.Remove(rel_item);
                            }
                            
                        }
                    }
                }
                //if(rel_table.DataItems.Count % 1000 == 0)
                //{
                //    System.Diagnostics.Trace.WriteLine(
                //    string.Format(
                //        "Lookup Status - Catalog:{0}, Table:{1}, TableCount:{2}, LookupCount:{3}",
                //        rel_table.TableMetadata.CatalogMetadata.Catalog.CatalogName,
                //        rel_table.TableMetadata.TableName,
                //        rel_table.DataItems.Count,
                //        rel_table.RelationshipParentLookup.Count));
                //}
            }
            else if (table is BucketDataTable)
            {
                //remove the item
                if (table.TryRemove(item.PrimaryKey, table.TableMetadata.DataType, out objTable))
                {
                    removed = (IDataItem)objTable;
                }
                //parse the table's relationships
                BucketDataTable bucket_table = table as BucketDataTable;
                foreach (RelationshipMetadata rel in table.Catalog.CatalogMetadata.Relationships.Where(r => r.Value.ParentTable.TableName == table.TableMetadata.TableName).Select(r => r.Value))
                {
                    //get the relationship table
                    RelationshipDataTable rel_table = bucket_table.Bucket.BucketDataTables[rel.RelationTable.TableName] as RelationshipDataTable;

                    //lock the resource
                    lock (rel_table.RelationshipParentLookup)
                    {
                        //remove the lookup for this parent
                        ConcurrentDictionary<long, long> lookup;
                        if (rel_table.RelationshipParentLookup.TryRemove(item.PrimaryKey, out lookup))
                        {
                            lookup.Clear();
                            lookup = null;
                        }
                    }
                }
            }
            //remove from catalog
            if (table.Catalog.CatalogDataTables[table.TableMetadata.TableName].TryRemove(item.PrimaryKey, table.TableMetadata.DataType, out objCatalog))
            {
                removed = (IDataItem)objCatalog;
            }
        }

        /// <summary>
        /// add data item to the table
        /// </summary>
        /// <param name="item"></param>
        public bool AddItem(IDataItem item)
        {
            object obj;
            bool bRet = false;

            //check if table type to handle relations
            if (this is RelationshipDataTable)
            {
                //parse the table's relationships
                RelationshipDataTable rel_table = this as RelationshipDataTable;

                //get the fields
                long parent_id = Convert.ToInt64(item.GetType().GetField(rel_table.RelationshipMetadata.ParentRelationshipField).GetValue(item));
                long child_id = Convert.ToInt64(item.GetType().GetField(rel_table.RelationshipMetadata.ChildRelationshipField).GetValue(item));

                //lock the resource
                lock (rel_table.RelationshipParentLookup)
                {
                    //add the relation
                    //if (ReferenceEquals(AddOrUpdate(item.PrimaryKey, item), item))
                    {
                        //look for the parent item
                        IBucketDataTable bucket_table = rel_table.Bucket.BucketDataTables[rel_table.RelationshipMetadata.ParentTable.TableName];
                        IDataItem parent_item;
                        if (bucket_table.TryGetValue(parent_id, bucket_table.TableMetadata.DataType, out obj))
                        {
                            //add to the parent item
                            parent_item = (IDataItem)obj;
                            //List<long> list = (List<long>)parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).GetValue(parent_item);
                            List<Tuple<List<long>, long>> list = (List<Tuple<List<long>, long>>)parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).GetValue(parent_item);
                            lock (parent_item)
                            {
                                try
                                {
                                    //check the list to init
                                    if(list == null)
                                        list = new List<Tuple<List<long>, long>>();
                                    
                                    //check for no items
                                    if (list.Count == 0)
                                    {
                                        List<long> sub_list = new List<long>();
                                        sub_list.Add(item.PrimaryKey);
                                        list.Add(new Tuple<List<long>,long>(sub_list, child_id));
                                        bRet = true;
                                    }
                                    else
                                    {
                                        //get the sub list
                                        var tuple = list.FirstOrDefault(t => t.Item2 == child_id);
                                        if (tuple == null)
                                        {
                                            List<long> sub_list = new List<long>();
                                            sub_list.Add(item.PrimaryKey);
                                            list.Add(new Tuple<List<long>, long>(sub_list, child_id));
                                            bRet = true;
                                        }
                                        else
                                        {
                                            //is this a distinct relationship
                                            if (rel_table.RelationshipMetadata.DistinctRelationship)
                                            {
                                                //check if item is already added
                                                if (!tuple.Item1.Contains(item.PrimaryKey))
                                                {
                                                    tuple.Item1.Add(item.PrimaryKey);
                                                    bRet = true;
                                                }
                                            }
                                            else
                                            {
                                                if (!list.Any(t => t.Item1.Contains(item.PrimaryKey)))
                                                {
                                                    List<long> sub_list = new List<long>();
                                                    sub_list.Add(item.PrimaryKey);
                                                    list.Add(new Tuple<List<long>, long>(sub_list, child_id));
                                                    bRet = true;
                                                }
                                            }
                                        }
                                    }
                                    //check if should update the field
                                    if(bRet)
                                        parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).SetValue(parent_item, list);


                                    //if (!list.Any(t => t.Item2 == child_id))
                                    //{
                                    //    List<long> sub_list = new List<long>();
                                    //    sub_list.Add(item.PrimaryKey);
                                    //    list.Add(new Tuple<List<long>, long>(sub_list, child_id));
                                    //    if (rel_table.RelationshipMetadata.DistinctRelationship)
                                    //        parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).SetValue(parent_item, list);
                                    //    bRet = true;
                                    //}
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                                }
                            }
                        }
                        else
                        {
                            //update the lookup
                            //rel_table.RelationshipParentLookup.AddOrUpdate
                            //    (
                            //            parent_id,
                            //            new ConcurrentDictionary<long, long>
                            //                (
                            //                new KeyValuePair<long, long>[] 
                            //                { 
                            //                    new KeyValuePair<long, long>
                            //                        (
                            //                            item.PrimaryKey, 
                            //                            child_id
                            //                        ) 
                            //                }
                            //            ),
                            //        (x, y) =>
                            //        {
                            //            y.AddOrUpdate
                            //                (
                            //                    item.PrimaryKey,
                            //                    child_id,
                            //                    (j, k) => { return k; }
                            //                );
                            //            return y;
                            //        }
                            //    );
                        }
                    }
                }
                ////update the parent collection
                //IBucketDataTable bucket_table = rel_table.Bucket.BucketDataTables[rel_table.RelationshipMetadata.ParentTable.TableName];
                //IDataItem parent_item;
                //if (bucket_table.TryGetValue(parent_id, bucket_table.TableMetadata.DataType, out obj))
                //{
                //    parent_item = (IDataItem)obj;
                //    parent_item.GetType().GetField(rel_table.RelationshipMetadata.ParentChildCollection).SetValue(parent_item, rel_table.GetChildList(parent_id));
                //}
            }
            else if (this is BucketDataTable)
            {
                //cast the table
                BucketDataTable bucket_table = this as BucketDataTable;

                //add the bucket reference
                object objItem = AddOrUpdate(item.PrimaryKey, item);
                if (ReferenceEquals(objItem, item))
                {
                    //parse the table's relationships
                    //foreach (RelationshipMetadata rel in this.TableMetadata.CatalogMetadata.Relationships.Where(r => r.Value.ParentTable.TableName == TableMetadata.TableName).Select(r => r.Value))
                    //{
                    //    //get the relationship table
                    //    RelationshipDataTable rel_table = bucket_table.Bucket.BucketDataTables[rel.RelationTable.TableName] as RelationshipDataTable;

                    //    //lock the resource
                    //    lock (rel_table.RelationshipParentLookup)
                    //    {
                    //        //remove the lookup
                    //        ConcurrentDictionary<long, long> lookup;
                    //        if (rel_table.RelationshipParentLookup.TryGetValue(item.PrimaryKey, out lookup))
                    //        {
                    //            //set the child collection
                    //            item.GetType().GetField(rel.ParentChildCollection).SetValue(item, rel_table.GetChildList(item.PrimaryKey));

                    //            //remove the list
                    //            rel_table.RelationshipParentLookup.TryRemove(item.PrimaryKey, out lookup);
                    //        }
                    //    }
                    //}
                    bRet = true;
                }
                else
                {
                    bRet = false;
                    //parse the table's relationships
                    //foreach (RelationshipMetadata rel in this.TableMetadata.CatalogMetadata.Relationships.Where(r => r.Value.ParentTable.TableName == TableMetadata.TableName).Select(r => r.Value))
                    //{
                    //    //get the relationship table
                    //    RelationshipDataTable rel_table = bucket_table.Bucket.BucketDataTables[rel.RelationTable.TableName] as RelationshipDataTable;

                    //    //lock the resource
                    //    lock (rel_table.RelationshipParentLookup)
                    //    {
                    //        //copy the collection
                    //        item.GetType().GetField(rel.ParentChildCollection).SetValue(item, objItem.GetType().GetField(rel.ParentChildCollection).GetValue(objItem));
                    //    }
                    //}
                }
                //update our catalog
                var catalog_table = this.TableMetadata.CatalogMetadata.Catalog.CatalogDataTables[this.TableMetadata.TableName];
                catalog_table.AddOrUpdate(item.PrimaryKey, item);
            }
            else
            {
                string test = "test";
            }

            return bRet;
        }

        #endregion

        #region Reflection For ConcurrentDictionary

        /// <summary>
        /// create the AsParallel expression
        /// </summary>
        /// <param name="paramDictionary"></param>
        /// <returns></returns>
        private MethodCallExpression DataItems_AsParallel(ParameterExpression paramDictionary)
        {
            //get dataitems type
            var dictionaryType = DataItems.GetType();

            //get the generic arguments for the type
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];
            
            //create the generic KeyValuePair type
            var keyvalueType = typeof(KeyValuePair<,>).MakeGenericType(keyType, elementType);

            //get the as parallel method info
            var mi = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "AsParallel" && x.IsGenericMethod && x.GetParameters().Length == 1).FirstOrDefault();

            //make the generic method
            var gen_mi = mi.MakeGenericMethod(keyvalueType);

            //create the call expression
            var call = Expression.Call
            (
                method: gen_mi,
                arg0: paramDictionary
            );

            return call;
        }

        /// <summary>
        /// create the select values expression
        /// </summary>
        /// <param name="ParentExp"></param>
        /// <returns></returns>
        private MethodCallExpression DataItems_Select_Value(MethodCallExpression ParentExp)
        {
            //get dataitems type
            var dictionaryType = DataItems.GetType();

            //get the generic arguments for the type
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];

            //create the generic KeyValuePair type
            var keyvalueType = typeof(KeyValuePair<,>).MakeGenericType(keyType, elementType);

            //get the select method
            var mi = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "Select" && x.IsGenericMethod && x.GetParameters().Length == 2).FirstOrDefault();

            //make the generic method
            var gen_mi = mi.MakeGenericMethod(keyvalueType, elementType);

            //the key value parameter
            var kvParam = Expression.Parameter(keyvalueType, "kv");

            //create the member access
            var valueMemberInfo = keyvalueType.GetProperty("Value");
            var valueMemberAccess = Expression.MakeMemberAccess(kvParam, valueMemberInfo);

            //create the lambda
            var lambda = Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(keyvalueType, elementType),
                body: valueMemberAccess,
                parameters: kvParam
            );
            //create the call
            var call = Expression.Call
            (
                method: gen_mi,
                arg0: ParentExp,
                arg1: lambda
            );
            return call;
        }

        /// <summary>
        /// create the order by expression
        /// </summary>
        /// <param name="ParentExp"></param>
        /// <param name="orderByField"></param>
        /// <param name="asc"></param>
        /// <returns></returns>
        private MethodCallExpression DataItems_OrderBy(MethodCallExpression ParentExp, string orderByField, bool asc)
        {
            //get dataitems type
            var dictionaryType = DataItems.GetType();

            //get the generic arguments for the type
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];

            //get the order by field
            var orderByFieldInfo = elementType.GetField(orderByField);

            //get the method info
            var mi = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == (asc ? "OrderBy" : "OrderByDescending") && x.IsGenericMethod && x.GetParameters().Length == 2).FirstOrDefault();
            
            //create the generic method info
            var gen_mi = mi.MakeGenericMethod(elementType, orderByFieldInfo.FieldType);

            //create the member access
            var param = Expression.Parameter(elementType, "et");
            var orderByMemberAccess = Expression.MakeMemberAccess(param, orderByFieldInfo);

            //create the lambda
            var lambda = Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(elementType, orderByFieldInfo.FieldType),
                body: orderByMemberAccess,
                parameters: param
            );
            //create the call
            var call = Expression.Call
            (
                method: gen_mi,
                arg0: ParentExp,
                arg1: lambda
            );
            return call;
        }

        /// <summary>
        /// create the merge expression
        /// </summary>
        /// <param name="ParentExp"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private MethodCallExpression DataItems_WithMergeOptions(MethodCallExpression ParentExp, ParallelMergeOptions option)
        {
            //get dataitems type
            var dictionaryType = DataItems.GetType();

            //get the generic arguments for the type
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];

            //get the method info
            var mi = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "WithMergeOptions" && x.IsGenericMethod && x.GetParameters().Length == 2).FirstOrDefault();

            //create the generic method info
            var gen_mi = mi.MakeGenericMethod(elementType);

            //create the constant expression
            var param = Expression.Constant(option, option.GetType());
            
            //create the call
            var call = Expression.Call
            (
                method: gen_mi,
                arg0: ParentExp,
                arg1: param
            );
            return call;
        }

        /// <summary>
        /// create the take expression
        /// </summary>
        /// <param name="ParentExp"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        private MethodCallExpression DataItems_Take(MethodCallExpression ParentExp, int take)
        {
            //get dataitems type
            var dictionaryType = DataItems.GetType();

            //get the generic arguments for the type
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];

            //get the method info
            var mi = typeof(ParallelEnumerable).GetMethods().Where(x => x.Name == "Take" && x.IsGenericMethod && x.GetParameters().Length == 2).FirstOrDefault();

            //create the generic method info
            var gen_mi = mi.MakeGenericMethod(elementType);


            //create the constant expression
            var param = Expression.Constant(take, take.GetType());

            //create the call
            var call = Expression.Call
            (
                method: gen_mi,
                arg0: ParentExp,
                arg1: param
            );
            return call;
            //return null;
        }

        /// <summary>
        /// make the expression
        /// </summary>
        /// <param name="dictionaryType"></param>
        /// <returns></returns>
        private Expression MakeSelectValuesInParallelExpression(Type dictionaryType)
        {
            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];
            var paramDictionary = Expression.Parameter(dictionaryType, "d");
            var parallelDictionaryExpression = DataItems_AsParallel(paramDictionary);
            var selectCallExpression = DataItems_Select_Value(parallelDictionaryExpression);

            return Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(dictionaryType, typeof(IEnumerable<>).MakeGenericType(elementType)),
                body: selectCallExpression,
                parameters: paramDictionary
            );
        }

        
        /// <summary>
        /// create the as parallel select of dictionary values
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public object GetValuesInParallel()
        {
            Delegate func = SelectExpression;
            if (func == null)
            {
                var expr = MakeSelectValuesInParallelExpression(DataItems.GetType());

                var type = expr == null ? null : expr.GetType();
                var compileMethod = expr == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
                func = (Delegate)compileMethod.Invoke(expr, new object[0]);
                SelectExpression = func;
            }
            MethodInfo funcInvoke = SelectInvoke;
            if(funcInvoke == null)
            {   
                funcInvoke = func.GetType().GetMethod("Invoke");
                SelectInvoke = funcInvoke;
            }
            var result = funcInvoke.Invoke(func, new object[] { DataItems });

            return result;
        }

        /// <summary>
        /// creates the full expression to return items to purge
        /// </summary>
        /// <param name="order_by_field"></param>
        /// <param name="order_asc"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        private Expression GetPurgeItemsExpression(string order_by_field, bool order_asc, int take)
        {
            //get info for expression generation
            var dic_type = DataItems.GetType();
            var genericArguments = dic_type.GetGenericArguments();
            var keyType = genericArguments[0];
            var elementType = genericArguments[1];
            var paramDictionary = Expression.Parameter(dic_type, "d");

            //create the expression
            var parallelDictionaryExpression = DataItems_AsParallel(paramDictionary);
            var selectCallExpression = DataItems_Select_Value(parallelDictionaryExpression);
            var OrderByCallExpression = DataItems_OrderBy(selectCallExpression, order_by_field, order_asc);
            var TakeCallExpression = DataItems_Take(OrderByCallExpression, take);
            //var MergeCallExpression = DataItems_WithMergeOptions(LimitCallExpression, ParallelMergeOptions.NotBuffered);

            //create the lambda
            return Expression.Lambda
            (
                delegateType: typeof(Func<,>).MakeGenericType(dic_type, typeof(IEnumerable<>).MakeGenericType(elementType)),
                body: TakeCallExpression,
                parameters: paramDictionary
            );
        }

        /// <summary>
        /// returns the items to purge
        /// </summary>
        /// <returns></returns>
        public object GetPurgeItems()
        {
            //get the number of items to purge
            int item_count = DataItems.Count;
            int items_to_purge = Math.Min((int)((double)item_count * (double)TableMetadata.SaturationPurgePercent / 100.0), 5000);

            //get the expression
            var exp = GetPurgeItemsExpression(
                TableMetadata.SaturationPurgeField,
                TableMetadata.SaturationPurgeOperation == Constants.PurgeOperations.PURGE_OP_SMALLEST ? true : false,
                items_to_purge);

            //compile the expression
            var type = exp == null ? null : exp.GetType();
            var compileMethod = exp == null ? null : type.GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
            var func = (Delegate)compileMethod.Invoke(exp, new object[0]);
            
            //invoke the command
            var funcInvoke = func.GetType().GetMethod("Invoke");
            var result = funcInvoke.Invoke(func, new object[] { DataItems });

            //return the ivoked results
            return result;
        }

        /// <summary>
        /// Get the cached lambda expression delegate for add or update
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private Delegate GetAddOrUpdateDelegate(Type keyType, Type itemType)
        {
            Delegate exp;
            if (!AddOrUpdateDelegateCache.TryGetValue(itemType, out exp))
            {
                var xParam = Expression.Parameter(keyType, "x");
                var yParam = Expression.Parameter(itemType, "y");

                var funcType = typeof(Func<,,>).MakeGenericType(keyType, itemType, itemType);

                var updateValueFactoryExpr = Expression.Lambda
                (
                    delegateType: funcType,
                    parameters: new ParameterExpression[] { xParam, yParam },
                    body: yParam
                );

                exp = updateValueFactoryExpr.Compile();

                AddOrUpdateDelegateCache.AddOrUpdate(itemType, exp, (x, y) => y);
            }
            return exp;
        }
        
        /// <summary>
        /// get the method info for add or update
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private MethodInfo GetAddOrUpdateMethod(Type keyType, Type itemType)
        {
            MethodInfo mi;
            if (!AddOrUpdateMethodCache.TryGetValue(itemType, out mi))
            {
                mi = DataItems.GetType().GetMethods().Where(x =>
                    x.Name == "AddOrUpdate" &&
                    x.GetParameters().Length == 3 &&
                    x.GetParameters()[0].ParameterType == keyType &&
                    x.GetParameters()[1].ParameterType == itemType
                ).FirstOrDefault();

                AddOrUpdateMethodCache.AddOrUpdate(itemType, mi, (x, y) => y);
            }
            return mi;
        }

        /// <summary>
        /// get the method info for try get value
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private MethodInfo GetTryGetValueMethod(Type keyType, Type itemType)
        {
            MethodInfo mi;
            if (!TryGetValueMethodCache.TryGetValue(itemType, out mi))
            {
                mi = DataItems.GetType().GetMethods().Where(x =>
                    x.Name == "TryGetValue" &&
                    x.GetParameters().Length == 2 &&
                    x.GetParameters()[0].ParameterType == keyType &&
                    x.GetParameters()[1].IsOut &&
                    x.GetParameters()[1].ParameterType.FullName.Replace("&", "") == itemType.FullName
                ).FirstOrDefault();

                TryGetValueMethodCache.AddOrUpdate(itemType, mi, (x, y) => y);
            }
            return mi;
        }

        /// <summary>
        /// get the method info for try remove
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private MethodInfo GetTryRemoveMethod(Type keyType, Type itemType)
        {
            MethodInfo mi;
            if (!TryRemoveMethodCache.TryGetValue(itemType, out mi))
            {
                mi = DataItems.GetType().GetMethods().Where(x =>
                    x.Name == "TryRemove" &&
                    x.GetParameters().Length == 2 &&
                    x.GetParameters()[0].ParameterType == keyType &&
                    x.GetParameters()[1].IsOut &&
                    x.GetParameters()[1].ParameterType.FullName.Replace("&", "") == itemType.FullName
                ).FirstOrDefault();

                TryRemoveMethodCache.AddOrUpdate(itemType, mi, (x, y) => y);
            }
            return mi;
        }

        /// <summary>
        /// add or update reflection wrapper
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public object AddOrUpdate(object key, object item)
        {
            //get the types
            var itemType = item.GetType();
            var keyType = key.GetType();

            //call through reflection
            MethodInfo mi = GetAddOrUpdateMethod(keyType, itemType);
            Delegate d = GetAddOrUpdateDelegate(keyType, itemType);
            object[] args = new object[] { key, item, d };
            object ret = mi.Invoke(DataItems, args);
            return ret;
        }

        /// <summary>
        /// try remove reflection wrapper
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryRemove(object key, Type itemType, out object item)
        {
            //init out variable
            item = null;

            //get the types
            var keyType = key.GetType();

            //call through reflection
            MethodInfo mi = GetTryRemoveMethod(keyType, itemType);
            object[] args = new object[] { key, item };
            var ret = mi.Invoke(DataItems, args);
            item = args[1];
            return (bool)ret;
        }

        /// <summary>
        /// try get reflection wrapper
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetValue(object key, Type itemType, out object item)
        {
            //init out variable
            item = null;

            //get the types
            var keyType = key.GetType();

            //call through reflection
            MethodInfo mi = GetTryGetValueMethod(keyType, itemType);
            object[] args = new object[] { key, item };
            var ret = mi.Invoke(DataItems, args);
            item = args[1];
            return (bool)ret;
        }

        #endregion
    }
}
