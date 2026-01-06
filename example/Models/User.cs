using Ambystech.Neo4j.Repository.Contracts.Attributes;
using Ambystech.Neo4j.Repository.Contracts.Nodes;

namespace Ambystech.Neo4j.Repository.Example.Models;

public class User : BaseNode
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [GraphField(RelationshipTypes.LIKE, "Post", isRelationship: true, direction: "Outgoing")]
    public List<Post> LikedPosts { get; set; } = [];
    
    [GraphField(RelationshipTypes.DISLIKE, "Post", isRelationship: true, direction: "Outgoing")]
    public List<Post> DislikedPosts { get; set; } = [];
}

