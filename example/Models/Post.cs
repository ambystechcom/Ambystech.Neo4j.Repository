using Ambystech.Neo4j.Repository.Contracts.Attributes;
using Ambystech.Neo4j.Repository.Contracts.Nodes;

namespace Ambystech.Neo4j.Repository.Example.Models;

public class Post : BaseNode
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    [GraphField(RelationshipTypes.LIKE, "User", isRelationship: true, direction: "Incoming")]
    public List<User> LikedBy { get; set; } = [];
    
    [GraphField(RelationshipTypes.DISLIKE, "User", isRelationship: true, direction: "Incoming")]
    public List<User> DislikedBy { get; set; } = [];
}

