using Ambystech.Neo4j.Repository.Extensions;
using Ambystech.Neo4j.Repository.Example.Converters;
using Ambystech.Neo4j.Repository.Example.Models;
using Ambystech.Neo4j.Repository.Example.Repositories;
using Ambystech.Neo4j.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ambystech.Neo4j.Repository.Converters;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>();

builder.Services.AddNeo4jRepository();
builder.Services.AddScoped<INodeConverter<Post>, PostConverter>();
builder.Services.AddScoped<INodeConverter<User>, UserConverter>();
builder.Services.AddScoped<IBaseGraphRepository<User>, UserRepository>();
builder.Services.AddScoped<IBaseGraphRepository<Post>, PostRepository>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var userRepo = host.Services.GetRequiredService<IBaseGraphRepository<User>>();
var postRepo = host.Services.GetRequiredService<IBaseGraphRepository<Post>>();

logger.LogInformation("Example Neo4j Repository Application");

try
{
    var users = await userRepo.GetAllAsync();
    logger.LogInformation("Found {Count} users", users.TotalResults);
    
    foreach (var user in users.Results)
    {
        logger.LogInformation("User: {Name} - Liked {LikedCount} posts, Disliked {DislikedCount} posts", 
            user.Name, user.LikedPosts.Count, user.DislikedPosts.Count);
    }
    
    var posts = await postRepo.GetAllAsync();
    logger.LogInformation("Found {Count} posts", posts.TotalResults);
    
    foreach (var post in posts.Results)
    {
        logger.LogInformation("Post: {Title} - {LikedCount} likes, {DislikedCount} dislikes", 
            post.Title, post.LikedBy.Count, post.DislikedBy.Count);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Error running example");
}

await host.RunAsync();
