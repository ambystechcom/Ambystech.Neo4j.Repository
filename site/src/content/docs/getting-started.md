---
title: Getting Started
description: Install the packages, wire up the driver, and run your first query.
---

## Install

Both packages target `net8.0`, `net9.0`, and `net10.0`.

```bash
dotnet add package Ambystech.Neo4j.Repository
dotnet add package Ambystech.Neo4j.Repository.Contracts
```

## Configure the driver

Provide the connection details through any `IConfiguration` source. `appsettings.json`, environment variables, and user secrets all work:

```json
{
  "Neo4j-Uri": "bolt://localhost:7687",
  "Neo4j-User": "neo4j",
  "Neo4j-Password": "password"
}
```

## Register services

`AddNeo4jRepository()` reads the configuration keys above and registers `IDriver` as a singleton. Register one converter and one repository per entity type.

```csharp
builder.Services.AddNeo4jRepository();

builder.Services.AddSingleton<INodeConverter<User>, UserConverter>();
builder.Services.AddScoped<IBaseGraphRepository<User>, UserRepository>();
```

## Define an entity

Inherit from `BaseNode` and mark relationship properties with `[GraphField]`:

```csharp
public class User : BaseNode
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [GraphField(relationshipTypes: new[] { RelationshipTypes.LIKE },
                targetNodeLabel: nameof(Post),
                direction: Direction.Outgoing)]
    public List<Post> LikedPosts { get; set; } = new();
}
```

## Use the repository

```csharp
var users = serviceProvider.GetRequiredService<IBaseGraphRepository<User>>();

var created = await users.CreateAsync(new User { Name = "Ada", Email = "ada@example.com" });
var all     = await users.GetAllAsync();
var one     = await users.GetByIdAsync(created.Id);
```

See [Packages → Repository](/Ambystech.Neo4j.Repository/packages/repository/) for the full `IBaseGraphRepository<T>` surface.
