using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Bermuda.Interface;

namespace Bermuda.Catalog
{
    public class RelationshipDataTable : BucketDataTable
    {
        #region Variables and Properties

        /// <summary>
        /// the parent relationship metadata 
        /// </summary>
        public IRelationshipMetadata RelationshipMetadata { get; set; }

        /// <summary>
        /// the table of data items
        /// </summary>
        public ConcurrentDictionary<long, ConcurrentDictionary<long, long>> RelationshipParentLookup { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with parent info
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="table_metadata"></param>
        /// <param name="relationship_metadata"></param>
        public RelationshipDataTable(IBucket bucket, ITableMetadata table_metadata, IRelationshipMetadata relationship_metadata)
            : base(bucket, table_metadata)
        {
            RelationshipMetadata = relationship_metadata;
            RelationshipParentLookup = new ConcurrentDictionary<long, ConcurrentDictionary<long, long>>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// get the child list for parent
        /// </summary>
        /// <param name="parent_id"></param>
        /// <returns></returns>
        public List<long> GetChildList(long parent_id)
        {
            ConcurrentDictionary<long, long> children;
            if (RelationshipParentLookup.TryGetValue(parent_id, out children))
            {
                if (RelationshipMetadata.DistinctRelationship)
                    return children.Values.Distinct().ToList();
                else
                    return children.Values.ToList();
            }
            return new List<long>();
            
        }

        #endregion
    }
}
