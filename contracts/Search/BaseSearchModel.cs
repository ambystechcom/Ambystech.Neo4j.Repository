namespace Ambystech.Neo4j.Repository.Contracts.Search;

public class BaseSearchModel
{
    public string? TextSearch { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public int Skip => (Page - 1) * PageSize;

    public bool IncludeDeleted { get; set; } = false;

    public string OrderByField { get; set; } = "created_at";

    public bool Descending { get; set; } = true;
}

