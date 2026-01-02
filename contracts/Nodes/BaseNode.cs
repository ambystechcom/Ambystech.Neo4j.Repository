namespace Ambystech.Neo4j.Repository.Contracts.Nodes;

public class BaseNode
{
    public string? Id { get; init; }
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

