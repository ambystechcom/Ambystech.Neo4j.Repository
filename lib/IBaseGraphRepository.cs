using Ambystech.Neo4j.Repository.Contracts.Search;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository;

public interface IBaseGraphRepository<T> where T : class
{
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<T?> GetByFieldAsync(string fieldName, string fieldValue, CancellationToken cancellationToken = default);

    Task<SearchResult<T>> GetAllAsync(int? skip = null, int? limit = null, CancellationToken cancellationToken = default);

    Task<SearchResult<T>> GetAllAsync(BaseSearchModel searchModel, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetRelatedEntitiesAsync(string nodeId, string relationshipType, RelationshipDirection direction = RelationshipDirection.Outgoing, string? targetNodeLabel = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<IRecord>> GetRelationshipsAsync(string nodeId, string relationshipType, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default);

    Task<T?> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> DetachDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> SearchAsync(string query, int? skip = null, int? limit = null, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<IRecord>> ExecuteQueryAsync(string query, object? parameters = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> ExecuteQueryAsync<TEntity>(string query, object? parameters = null, CancellationToken cancellationToken = default) where TEntity : class;

    Task<bool> CreateRelationshipAsync(string sourceElementId, string relationshipType, string targetElementId, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default);

    Task<bool> SyncRelationshipsAsync(string sourceElementId, string relationshipType, IEnumerable<string> targetElementIds, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default);
}

public enum RelationshipDirection
{
    Outgoing,
    Incoming,
    Both
}

