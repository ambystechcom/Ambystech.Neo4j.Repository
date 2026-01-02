using Ambystech.Neo4j.Repository;
using Ambystech.Neo4j.Repository.Converters;
using Ambystech.Neo4j.Repository.Example.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Example.Repositories;

public class UserRepository(
    IDriver driver,
    ILogger<UserRepository> logger,
    INodeConverter<User> nodeConverter) : BaseGraphRepository<User>(driver, logger, nodeConverter)
{
}

