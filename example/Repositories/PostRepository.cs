using Ambystech.Neo4j.Repository.Converters;
using Ambystech.Neo4j.Repository.Example.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Example.Repositories;

public class PostRepository(
    IDriver driver,
    ILogger<PostRepository> logger,
    INodeConverter<Post> nodeConverter) : BaseGraphRepository<Post>(driver, logger, nodeConverter)
{
}

