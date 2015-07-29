using System;
namespace Bermuda.Interface
{
    public interface IRelationshipMetadata
    {
        void Init(ICatalogMetadata catalog_metadata);
        ICatalogMetadata CatalogMetadata { get; set; }
        string ChildField { get; set; }
        string ChildRelationshipField { get; set; }
        ITableMetadata ChildTable { get; set; }
        string ChildTableName { get; set; }
        bool DistinctRelationship { get; set; }
        string ParentChildCollection { get; set; }
        string ParentField { get; set; }
        string ParentRelationshipField { get; set; }
        ITableMetadata ParentTable { get; set; }
        string ParentTableName { get; set; }
        string RelationshipName { get; set; }
        ITableMetadata RelationTable { get; set; }
        string RelationTableName { get; set; }
    }
}
