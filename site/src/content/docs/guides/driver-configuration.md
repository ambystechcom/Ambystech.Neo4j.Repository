---
title: Configuring the Neo4j Driver
description: Override connection pool, timeouts, encryption, and logging on the underlying IDriver.
---

`AddNeo4jRepository()` has two overloads. Pick the one that matches how much control you need — or register `IDriver` yourself if you need more.

## 1. Zero-config (default)

The no-argument overload reads three keys from `IConfiguration` and registers `IDriver` as a singleton. Basic auth is built from `Neo4j-User` / `Neo4j-Password`.

```csharp
builder.Services.AddNeo4jRepository();
```

```json
{
  "Neo4j-Uri": "bolt://localhost:7687",
  "Neo4j-User": "neo4j",
  "Neo4j-Password": "password"
}
```

Use this in development and anywhere the stock driver defaults are fine.

## 2. `ConfigBuilder` overload

Use this when you need to tune the driver — connection pool, timeouts, encryption, logging.

```csharp
using Neo4j.Driver;

builder.Services.AddNeo4jRepository(config =>
{
    config
        .WithMaxConnectionPoolSize(100)
        .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(60))
        .WithConnectionTimeout(TimeSpan.FromSeconds(30))
        .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(30))
        .WithEncryptionLevel(EncryptionLevel.Encrypted)
        .WithLogger(new ConsoleLogger());
});
```

:::caution[Auth is not read from configuration]
This overload reads only `Neo4j-Uri` from configuration — it calls `GraphDatabase.Driver(uri, configBuilder)` without an `AuthToken`. Use it when:
- The URI already embeds credentials (`neo4j+s://user:pass@host`), **or**
- The server accepts unauthenticated connections, **or**
- You need full control — in which case prefer option 3 below.
:::

### Common `ConfigBuilder` knobs

| Method                              | What it controls                                      |
|-------------------------------------|-------------------------------------------------------|
| `WithMaxConnectionPoolSize(int)`    | Cap on concurrent connections per driver instance     |
| `WithConnectionAcquisitionTimeout`  | How long a caller waits for a pooled connection       |
| `WithConnectionTimeout`             | Socket-level connect timeout                          |
| `WithMaxTransactionRetryTime`       | How long managed transactions keep retrying           |
| `WithEncryptionLevel`               | `Encrypted` / `None` (only for `bolt://` scheme)      |
| `WithTrustManager`                  | Custom TLS trust policy                               |
| `WithLogger(ILogger)`               | Plug the driver into your logging pipeline            |
| `WithFetchSize(long)`               | Records fetched per network round-trip                |
| `WithDefaultReadBufferSize(int)`    | Per-connection read buffer                            |

See the [Neo4j .NET driver manual](https://neo4j.com/docs/dotnet-manual/current/) for the full surface.

## 3. Register `IDriver` yourself

For anything the overloads don't cover — custom `AuthToken`, routing context, Aura certificates, multiple databases — skip `AddNeo4jRepository()` and register the driver directly. The repository only needs an `IDriver` in the container.

```csharp
using Neo4j.Driver;

builder.Services.AddSingleton<IDriver>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    return GraphDatabase.Driver(
        config["Neo4j-Uri"],
        AuthTokens.Basic(config["Neo4j-User"], config["Neo4j-Password"]),
        o => o
            .WithMaxConnectionPoolSize(200)
            .WithEncryptionLevel(EncryptionLevel.Encrypted)
            .WithLogger(sp.GetRequiredService<ILogger<IDriver>>().AsDriverLogger()));
});
```

Then register your converters and repositories as usual:

```csharp
builder.Services.AddSingleton<INodeConverter<User>, UserConverter>();
builder.Services.AddScoped<IBaseGraphRepository<User>, UserRepository>();
```

## Lifetime and disposal

`IDriver` is registered as a **singleton** — the driver owns a connection pool and is designed to live for the lifetime of the application. Let the DI container dispose it on shutdown; do not wrap it in `using` blocks per request.

## Production checklist

- Encrypt the channel: `neo4j+s://` / `bolt+s://` URIs or `WithEncryptionLevel(EncryptionLevel.Encrypted)`.
- Size the pool to your workload. `MaxConnectionPoolSize` defaults to 100 — raise it for high-throughput services, lower it to match a PaaS tier.
- Set a sensible `ConnectionAcquisitionTimeout` so callers fail fast when the pool is saturated.
- Wire `WithLogger` to your `ILogger` so driver diagnostics land in the same sink as the rest of the app.
- Keep credentials out of source. Use user secrets locally, environment variables or a secret store in production.

## Further reading

- [Neo4j .NET driver manual](https://neo4j.com/docs/dotnet-manual/current/) — official documentation covering sessions, transactions, routing, and the full configuration surface.
- [`neo4j/neo4j-dotnet-driver` on GitHub](https://github.com/neo4j/neo4j-dotnet-driver) — source, release notes, and issue tracker for the `Neo4j.Driver` package this library builds on.
