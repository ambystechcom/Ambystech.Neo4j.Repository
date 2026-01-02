namespace Ambystech.Neo4j.Repository.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class GraphFieldAttribute : Attribute
{
    public string FieldName { get; }
    public bool IsSearchable { get; }
    public bool IsRelationship { get; }
    public string[]? RelationshipTypes { get; }
    public string? TargetNodeLabel { get; }
    public string? TargetFieldName { get; }
    public string Direction { get; }
    public bool IsCountOnly { get; }

    public GraphFieldAttribute(string fieldName, bool isSearchable = false)
    {
        FieldName = fieldName;
        IsSearchable = isSearchable;
        IsRelationship = false;
        Direction = "Outgoing";
        IsCountOnly = false;
    }

    public GraphFieldAttribute(
        string fieldName,
        bool isSearchable,
        bool isRelationship,
        string[] relationshipTypes,
        string targetNodeLabel,
        string targetFieldName,
        string direction = "Outgoing")
    {
        FieldName = fieldName;
        IsSearchable = isSearchable;
        IsRelationship = isRelationship;
        RelationshipTypes = relationshipTypes;
        TargetNodeLabel = targetNodeLabel;
        TargetFieldName = targetFieldName;
        Direction = direction;
        IsCountOnly = false;
    }

    public GraphFieldAttribute(
        string relationshipType,
        string targetNodeLabel,
        bool isRelationship,
        string direction = "Outgoing",
        bool isCountOnly = false)
    {
        FieldName = string.Empty;
        IsSearchable = false;
        IsRelationship = isRelationship;
        RelationshipTypes = [relationshipType];
        TargetNodeLabel = targetNodeLabel;
        TargetFieldName = null;
        Direction = direction;
        IsCountOnly = isCountOnly;
    }
}

