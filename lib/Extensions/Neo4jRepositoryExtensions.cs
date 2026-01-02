using Ambystech.Neo4j.Repository.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Extensions;

public static class Neo4jRepositoryExtensions
{
    public static IServiceCollection AddNeo4jRepository(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var uri = config["Neo4j-Uri"] ?? throw new InvalidOperationException("Neo4j server uri is not configured.");
            var user = config["Neo4j-User"] ?? throw new InvalidOperationException("Neo4j user is not configured.");
            var password = config["Neo4j-Password"] ?? throw new InvalidOperationException("Neo4j password is not configured.");

            return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        });

        return services;
    }

    public static IServiceCollection AddNeo4jRepository(this IServiceCollection services, Action<ConfigBuilder> configBuilder = null)
    {
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var uri = config["Neo4j-Uri"] ?? throw new InvalidOperationException("Neo4j server uri is not configured.");
            return GraphDatabase.Driver(uri, configBuilder);
        });

        return services;
    }
}

