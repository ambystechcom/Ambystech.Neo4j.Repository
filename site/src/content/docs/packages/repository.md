---
title: Repository
description: The generic repository, converters, and DI extensions.
---

`Ambystech.Neo4j.Repository` provides the runtime implementation of the pattern: session management, Cypher generation, soft delete, and relationship loading.

## `IBaseGraphRepository<T>`

The generic contract. `T` must inherit from `BaseNode`.

### CRUD

| Method                                   | Notes                                                           |
|------------------------------------------|-----------------------------------------------------------------|
| `CreateAsync(T entity)`                  | Stamps `CreatedAt`, runs `CREATE (n:T {...})`                   |
| `GetByIdAsync(string id)`                | Matches on `elementId(n) = $id`, filters soft-deleted           |
| `GetByFieldAsync(field, value)`          | Single-property lookup                                          |
| `GetAllAsync()` / `GetAllAsync(search)`  | Unpaged or paged (`BaseSearchModel` input, `SearchResult<T>` out) |
| `UpdateAsync(T entity)`                  | Stamps `UpdatedAt`, merges properties                           |
| `DeleteAsync(string id)`                 | Soft delete — sets `DeletedAt`                                  |
| `DetachDeleteAsync(string id)`           | Hard delete — `DETACH DELETE`                                   |
| `CountAsync(search?)`                    | Count matching nodes                                            |

### Relationships

| Method                                                           | Purpose                                      |
|------------------------------------------------------------------|----------------------------------------------|
| `GetRelatedEntitiesAsync<TOther>(id, type, direction)`           | Load the other ends of a relationship        |
| `GetRelationshipsAsync(id)`                                      | List all relationships for a node            |
| `CreateRelationshipAsync(sourceId, targetId, type)`              | Create a single edge                         |
| `SyncRelationshipsAsync(sourceId, targetIds, type)`              | Replace the set of outgoing edges of a type  |

### Raw queries

Escape hatches for bespoke Cypher: `ExecuteAsync`, `ExecuteReadAsync<T>`, and related overloads. Useful for aggregations, traversals, or anything the generic surface does not express.

## `BaseGraphRepository<T>`

Abstract implementation. Subclass it to create per-entity repositories — usually a one-liner:

```csharp
public class UserRepository(
    IDriver driver,
    ILogger<UserRepository> logger,
    INodeConverter<User> converter)
    : BaseGraphRepository<User>(driver, logger, converter);
```

Internally it caches relationship metadata per type in a `ConcurrentDictionary`, opens async sessions in `try/finally` blocks, and always filters `deleted_at IS NULL` unless `IncludeDeleted` is set.

## `INodeConverter<T>` & `DefaultNodeConverter<T>`

The bridge between `INode` / `IRecord` from the driver and your entity type.

```csharp
public interface INodeConverter<T> where T : BaseNode
{
    T ConvertFromNode(INode node);
    T ConvertFromRecord(IRecord record);
    IDictionary<string, object> ConvertToProperties(T entity);
}
```

`DefaultNodeConverter<T>` uses reflection with a cached property map. It handles primitives, `DateTime`, `Guid`, `Nullable<T>`, enums, and collections. Override `ConvertFromRecord` when you need to hydrate relationship collections from the same record (see the [example](/Ambystech.Neo4j.Repository/example/)).

## `AddNeo4jRepository()`

The DI extension. Reads three keys from `IConfiguration` and registers `IDriver` as a singleton:

```csharp
builder.Services.AddNeo4jRepository();
```

| Configuration key  | Meaning                   |
|--------------------|---------------------------|
| `Neo4j-Uri`        | e.g. `bolt://localhost:7687` |
| `Neo4j-User`       | Username                  |
| `Neo4j-Password`   | Password                  |

You still register one `INodeConverter<T>` and one `IBaseGraphRepository<T>` per entity — the library intentionally does not scan assemblies.

## `NodeExtensions`

Typed, null-safe property readers for `INode`: `GetSafeStringProperty`, `GetSafeIntProperty`, `GetSafeBoolProperty`, `GetSafeDateTimeProperty`, `GetSafeListProperty<T>`, plus a generic `GetProperty<T>`. Handy when writing custom converters.
