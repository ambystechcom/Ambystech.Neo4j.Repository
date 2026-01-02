namespace Ambystech.Neo4j.Repository.Contracts.Search;

public class SearchResult<T>
{
    public IEnumerable<T> Results { get; set; } = [];
    public int TotalCount => Results.Count();
    public long TotalResults { get; set; } = 0;
}

