---
title: Contracts
description: Base types, attributes, and search models shared by the repository and its consumers.
---

`Ambystech.Neo4j.Repository.Contracts` is a lightweight package that defines the types the repository reads and writes. Domain projects can reference it without pulling in the Neo4j driver.

## `BaseNode`

Abstract base class for every entity stored in the graph. It provides the audit fields the repository maintains automatically.

| Property    | Type          | Set by                                        |
|-------------|---------------|-----------------------------------------------|
| `Id`        | `string?`     | Neo4j `elementId(n)` on read                  |
| `CreatedAt` | `DateTime?`   | `CreateAsync`                                 |
| `UpdatedAt` | `DateTime?`   | `UpdateAsync`                                 |
| `DeletedAt` | `DateTime?`   | `DeleteAsync` (soft delete)                   |

Queries filter `DeletedAt IS NULL` by default.

## `GraphFieldAttribute`

Marks a property as searchable, as a Neo4j relationship, or both. Three overloads cover the common cases:

```csharp
// 1. Simple searchable scalar field
[GraphField("name", isSearchable: true)]
public string Name { get; set; }

// 2. Searchable relationship property
[GraphField("likedPosts",
  isSearchable: true,
  relationshipTypes: new[] { "LIKE" },
  targetNodeLabel: "Post",
  direction: Direction.Outgoing)]
public List<Post> LikedPosts { get; set; }

// 3. Relationship-only (e.g. reverse link or count-only)
[GraphField(
  relationshipTypes: new[] { "LIKE" },
  targetNodeLabel: "Post",
  direction: Direction.Incoming,
  isCountOnly: true)]
public int LikeCount { get; set; }
```

Key fields: `FieldName`, `IsSearchable`, `IsRelationship`, `RelationshipTypes[]`, `TargetNodeLabel`, `Direction`, `IsCountOnly`.

## `BaseSearchModel`

Pagination and filter contract passed to the repository's search methods.

| Property         | Purpose                                             |
|------------------|-----------------------------------------------------|
| `TextSearch`     | Free-text filter applied to searchable fields       |
| `Page`           | 1-based page number                                 |
| `PageSize`       | Page size                                           |
| `Skip`           | Computed offset (`(Page - 1) * PageSize`)           |
| `IncludeDeleted` | Bypass the soft-delete filter                       |
| `OrderByField`   | Property name to sort by                            |
| `Descending`     | Sort direction                                      |

## `SearchResult<T>`

Wrapper returned by paginated queries.

```csharp
public class SearchResult<T>
{
    public IEnumerable<T> Results { get; set; }
    public long TotalResults { get; set; }
}
```
