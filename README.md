# Ambystech.Neo4j.Repository

Neo4j Repository pattern implementation for .NET.

## Overview

This library provides a generic base repository implementation for Neo4j graph databases, enabling easy CRUD operations, relationship management, and search functionality.

## Packages

- **Ambystech.Neo4j.Repository** - Main repository implementation
- **Ambystech.Neo4j.Repository.Contracts** - Base contracts and attributes

## Installation

```bash
dotnet add package Ambystech.Neo4j.Repository
dotnet add package Ambystech.Neo4j.Repository.Contracts
```

## Quick Start

1. Configure Neo4j connection in `appsettings.json` or `user secrets`:

```json
{
  "Neo4j-Uri": "bolt://localhost:7687",
  "Neo4j-User": "neo4j",
  "Neo4j-Password": "password"
}
```

2. Register services:

```csharp
builder.Services.AddNeo4jRepository();
builder.Services.AddSingleton<INodeConverter<YourEntity>, YourEntityConverter>();
builder.Services.AddScoped<IBaseGraphRepository<YourEntity>, YourEntityRepository>();
```

3. Use the repository:

```csharp
var repository = serviceProvider.GetRequiredService<IBaseGraphRepository<YourEntity>>();
var entities = await repository.GetAllAsync();
```

## Features

- Generic CRUD operations
- Relationship management
- Search and filtering
- Soft delete support
- Automatic relationship loading via GraphFieldAttribute

## Example

See the `example/` directory for a complete working example with a social network graph (Users, Posts, Likes, Dislikes).

## License

MIT License - see LICENSE file for details.

