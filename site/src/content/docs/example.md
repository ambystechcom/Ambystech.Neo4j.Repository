---
title: Example — Social graph
description: A walkthrough of the User / Post example project.
---

The `example/` folder in the repository ships a small console app that models a social graph with `User`, `Post`, and `LIKE` / `DISLIKE` relationships. It is the quickest way to see every piece of the library working together.

![Example graph](https://github.com/ambystechcom/Ambystech.Neo4j.Repository/blob/main/assets/user_graph.png?raw=true)

## Domain

`User` and `Post` both inherit from `BaseNode` and use `[GraphField]` to describe their relationships in each direction.

```csharp
public class User : BaseNode
{
    public string Name  { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [GraphField(relationshipTypes: new[] { RelationshipTypes.LIKE },
                targetNodeLabel: nameof(Post), direction: Direction.Outgoing)]
    public List<Post> LikedPosts { get; set; } = new();

    [GraphField(relationshipTypes: new[] { RelationshipTypes.DISLIKE },
                targetNodeLabel: nameof(Post), direction: Direction.Outgoing)]
    public List<Post> DislikedPosts { get; set; } = new();
}

public class Post : BaseNode
{
    public string Title   { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [GraphField(relationshipTypes: new[] { RelationshipTypes.LIKE },
                targetNodeLabel: nameof(User), direction: Direction.Incoming)]
    public List<User> LikedBy { get; set; } = new();

    [GraphField(relationshipTypes: new[] { RelationshipTypes.DISLIKE },
                targetNodeLabel: nameof(User), direction: Direction.Incoming)]
    public List<User> DislikedBy { get; set; } = new();
}
```

`RelationshipTypes` centralises the Cypher type names as constants.

## Repositories

Per-entity repositories stay minimal — they just subclass `BaseGraphRepository<T>`:

```csharp
public class UserRepository(
    IDriver driver,
    ILogger<UserRepository> logger,
    INodeConverter<User> converter)
    : BaseGraphRepository<User>(driver, logger, converter);

public class PostRepository(
    IDriver driver,
    ILogger<PostRepository> logger,
    INodeConverter<Post> converter)
    : BaseGraphRepository<Post>(driver, logger, converter);
```

## Converters

The converters extend `DefaultNodeConverter<T>` and override `ConvertFromRecord` to hydrate the relationship collections from the same record the repository already fetched — no extra round-trips.

```csharp
public class UserConverter(IServiceProvider sp) : DefaultNodeConverter<User>
{
    public override User ConvertFromRecord(IRecord record)
    {
        var user = base.ConvertFromRecord(record);

        var postConverter = sp.GetRequiredService<INodeConverter<Post>>();

        if (record.Values.TryGetValue("likedPosts", out var liked) && liked is IEnumerable<object> likes)
            user.LikedPosts = likes.OfType<INode>().Select(postConverter.ConvertFromNode).ToList();

        if (record.Values.TryGetValue("dislikedPosts", out var disliked) && disliked is IEnumerable<object> dislikes)
            user.DislikedPosts = dislikes.OfType<INode>().Select(postConverter.ConvertFromNode).ToList();

        return user;
    }
}
```

## Composition

`Program.cs` wires everything through the standard .NET host:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNeo4jRepository();

builder.Services.AddSingleton<INodeConverter<Post>, PostConverter>();
builder.Services.AddSingleton<INodeConverter<User>, UserConverter>();

builder.Services.AddScoped<IBaseGraphRepository<Post>, PostRepository>();
builder.Services.AddScoped<IBaseGraphRepository<User>, UserRepository>();

var host = builder.Build();
```

From there, resolving `IBaseGraphRepository<User>` gives you a fully-wired repository with relationship loading, soft delete, and pagination.
