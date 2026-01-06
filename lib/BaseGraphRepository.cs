using Ambystech.Neo4j.Repository.Contracts.Attributes;
using Ambystech.Neo4j.Repository.Contracts.Nodes;
using Ambystech.Neo4j.Repository.Contracts.Search;
using Ambystech.Neo4j.Repository.Converters;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace Ambystech.Neo4j.Repository;

public abstract class BaseGraphRepository<T>(IDriver driver, MsLogger logger, INodeConverter<T> nodeConverter) : IBaseGraphRepository<T> where T : BaseNode
{
    protected readonly IDriver _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    protected readonly MsLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly INodeConverter<T> _nodeConverter = nodeConverter ?? throw new ArgumentNullException(nameof(nodeConverter));
    protected readonly string _nodeLabel = typeof(T).Name;

    private static readonly ConcurrentDictionary<Type, List<RelationshipPropertyInfo>> _relationshipCache = new();

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var properties = _nodeConverter.ConvertToProperties(entity);
            properties.Add("created_at", DateTime.UtcNow);

            var query = $"CREATE (n:{_nodeLabel} $properties) RETURN n";

            _logger.LogDebug("Executing create query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["properties"] = properties });
            var record = await result.SingleAsync();

            return _nodeConverter.ConvertFromNode(record["n"].As<INode>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity of type {EntityType}", typeof(T).Name);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = $"MATCH (n:{_nodeLabel}) WHERE elementId(n) = $id AND (n.deleted_at IS NULL) RETURN n";

            _logger.LogDebug("Executing get by id query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["id"] = id });
            var records = await result.ToListAsync();
            var record = records.FirstOrDefault();

            return record != null ? _nodeConverter.ConvertFromNode(record["n"].As<INode>()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity by id: {Id}", id);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<T?> GetByFieldAsync(string fieldName, string fieldValue, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = $"MATCH (n:{_nodeLabel}) WHERE n.{fieldName} = $fieldValue AND (n.deleted_at IS NULL) RETURN n";

            _logger.LogDebug("Executing get by field query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["fieldValue"] = fieldValue });
            var records = await result.ToListAsync();
            var record = records.FirstOrDefault();

            return record != null ? _nodeConverter.ConvertFromNode(record["n"].As<INode>()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity by field: {FieldName} = {FieldValue}", fieldName, fieldValue);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<SearchResult<T>> GetAllAsync(int? skip = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(new BaseSearchModel { Page = skip ?? 0, PageSize = limit ?? 50 }, cancellationToken);
    }

    public virtual async Task<SearchResult<T>> GetAllAsync(BaseSearchModel searchModel, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        SearchResult<T> searchResult = new();
        try
        {
            var relationshipProps = GetRelationshipProperties();
            var hasRelationships = relationshipProps.Any();

            string query;
            Dictionary<string, object> parameters;

            if (hasRelationships)
            {
                (query, parameters) = BuildSearchQueryWithRelationships(searchModel, relationshipProps);
            }
            else
            {
                (query, parameters) = BuildSearchQuery(searchModel);
            }

            _logger.LogDebug("Executing search query: {Query} with parameters: {@Parameters}", query, parameters);

            var result = await session.RunAsync(query, parameters);
            var records = await result.ToListAsync(cancellationToken: cancellationToken);
            await session.CloseAsync();

            var count = await CountAsync(cancellationToken);

            var entities = hasRelationships
                ? records.Select(record => _nodeConverter.ConvertFromRecord(record))
                : records.Select(record => _nodeConverter.ConvertFromNode(record["n"].As<INode>()));

            searchResult = new SearchResult<T>
            {
                Results = entities,
                TotalResults = count
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities with search model: {@SearchModel}", searchModel);
            throw;
        }

        return searchResult;
    }

    public virtual async Task<T?> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var properties = _nodeConverter.ConvertToProperties(entity);
            properties = properties.Where(kv => kv.Value != null && !(kv.Value is string str && string.IsNullOrWhiteSpace(str)))
                                   .ToDictionary(kv => kv.Key, kv => kv.Value);

            var query = $"MATCH (n:{_nodeLabel}) WHERE elementId(n) = $id SET n += $properties, n.updated_at = datetime() RETURN n";

            _logger.LogDebug("Executing update query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["id"] = id, ["properties"] = properties });
            var records = await result.ToListAsync();
            var record = records.FirstOrDefault();

            return record != null ? _nodeConverter.ConvertFromNode(record["n"].As<INode>()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity with id: {Id}", id);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = $"MATCH (n:{_nodeLabel}) WHERE elementId(n) = $id SET n.deleted_at = datetime() RETURN n";

            _logger.LogDebug("Executing soft delete query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["id"] = id });
            var records = await result.ToListAsync();

            return records.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting entity with id: {Id}", id);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<bool> DetachDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = $"MATCH (n:{_nodeLabel}) WHERE elementId(n) = $id DETACH DELETE n RETURN count(n) as deletedCount";

            _logger.LogDebug("Executing detach delete query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["id"] = id });
            var record = await result.SingleAsync();

            return record["deletedCount"].As<int>() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detach deleting entity with id: {Id}", id);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<IEnumerable<T>> SearchAsync(string query, int? skip = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var cypherQuery = BuildSearchQuery(query, skip, limit);

            _logger.LogDebug("Executing search query: {Query}", cypherQuery);

            var result = await session.RunAsync(cypherQuery, new Dictionary<string, object> { ["searchQuery"] = query.ToLower() });
            var records = await result.ToListAsync();

            return records.Select(record => _nodeConverter.ConvertFromNode(record["n"].As<INode>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities with query: {Query}", query);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = $"MATCH (n:{_nodeLabel}) WHERE (n.deleted_at IS NULL) RETURN count(n) as totalCount";

            _logger.LogDebug("Executing count query: {Query}", query);

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();

            return record["totalCount"].As<long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities");
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<IEnumerable<IRecord>> ExecuteQueryAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            _logger.LogDebug("Executing custom query: {Query}", query);
            
            var result = await session.RunAsync(query, parameters as Dictionary<string, object> ?? []);
            return await result.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing custom query: {Query}", query);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<IEnumerable<T>> ExecuteQueryAsync<TEntity>(string query, object? parameters = null, CancellationToken cancellationToken = default) where TEntity : class
    {
        var session = _driver.AsyncSession();
        try
        {
            _logger.LogDebug("Executing custom query with mapping: {Query}", query);

            var result = await session.RunAsync(query, parameters as Dictionary<string, object> ?? new Dictionary<string, object>());
            var records = await result.ToListAsync();

            return records.Select(record => _nodeConverter.ConvertFromNode(record["n"].As<INode>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing custom query with mapping: {Query}", query);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<IEnumerable<T>> GetRelatedEntitiesAsync(string nodeId, string relationshipType, RelationshipDirection direction = RelationshipDirection.Outgoing, string? targetNodeLabel = null, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var directionPattern = direction switch
            {
                RelationshipDirection.Outgoing => $"-[:{relationshipType}]->",
                RelationshipDirection.Incoming => $"<-[:{relationshipType}]-",
                RelationshipDirection.Both => $"-[:{relationshipType}]-",
                _ => $"-[:{relationshipType}]->"
            };

            var targetNodePattern = !string.IsNullOrEmpty(targetNodeLabel) ? $"(target:{targetNodeLabel})" : "(target)";
            
            var query = $"MATCH (source:{_nodeLabel}) WHERE elementId(source) = $nodeId " +
                       $"MATCH (source){directionPattern}{targetNodePattern} " +
                       $"RETURN target ORDER BY target.createdAt DESC";

            _logger.LogDebug("Executing get related entities query: {Query}", query);

            var result = await session.RunAsync(query, new Dictionary<string, object> { ["nodeId"] = nodeId });
            var records = await result.ToListAsync(cancellationToken: cancellationToken);

            return records.Select(record => _nodeConverter.ConvertFromNode(record["target"].As<INode>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related entities for nodeId: {NodeId}, relationship: {RelationshipType}, direction: {Direction}", 
                nodeId, relationshipType, direction);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<IEnumerable<IRecord>> GetRelationshipsAsync(string nodeId, string relationshipType, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var directionPattern = direction switch
            {
                RelationshipDirection.Outgoing => $"-[r:{relationshipType}]->",
                RelationshipDirection.Incoming => $"<-[r:{relationshipType}]-",
                RelationshipDirection.Both => $"-[r:{relationshipType}]-",
                _ => $"-[r:{relationshipType}]->"
            };

            var query = $"MATCH (source:{_nodeLabel}) WHERE source.id = $nodeId " +
                       $"MATCH (source){directionPattern}(target) " +
                       $"RETURN r, source, target ORDER BY r.createdAt DESC";
            
            _logger.LogDebug("Executing get relationships query: {Query}", query);
            
            var result = await session.RunAsync(query, new Dictionary<string, object> { ["nodeId"] = nodeId });
            return await result.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relationships for nodeId: {NodeId}, relationship: {RelationshipType}, direction: {Direction}", 
                nodeId, relationshipType, direction);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    protected virtual (string Query, Dictionary<string, object> Parameters) BuildSearchQuery(BaseSearchModel searchModel)
    {
        var queryBuilder = new StringBuilder($"MATCH (n:{_nodeLabel})");
        var parameters = new Dictionary<string, object>();

        var relationshipMatches = BuildRelationshipMatches(searchModel, parameters);
        if (!string.IsNullOrEmpty(relationshipMatches))
        {
            queryBuilder.Append($" {relationshipMatches}");
        }

        var whereClause = BuildWhereClause(searchModel, parameters);

        if (!string.IsNullOrEmpty(whereClause))
        {
            queryBuilder.Append($" WHERE {whereClause}");
        }

        queryBuilder.Append(" RETURN DISTINCT n");

        if (!string.IsNullOrWhiteSpace(searchModel.OrderByField))
        {
            var orderDirection = searchModel.Descending ? "DESC" : "ASC";
            queryBuilder.Append($" ORDER BY n.{searchModel.OrderByField} {orderDirection}");
        }

        if (searchModel.Skip > 0)
        {
            queryBuilder.Append($" SKIP {searchModel.Skip}");
        }

        if (searchModel.PageSize > 0)
        {
            queryBuilder.Append($" LIMIT {searchModel.PageSize}");
        }

        return (queryBuilder.ToString(), parameters);
    }

    protected virtual (string Query, Dictionary<string, object> Parameters) BuildCountQuery(BaseSearchModel searchModel)
    {
        var queryBuilder = new StringBuilder($"MATCH (n:{_nodeLabel})");
        var parameters = new Dictionary<string, object>();
        var whereClause = BuildWhereClause(searchModel, parameters);

        if (!string.IsNullOrEmpty(whereClause))
        {
            queryBuilder.Append($" WHERE {whereClause}");
        }

        queryBuilder.Append(" RETURN count(n) as totalCount");

        return (queryBuilder.ToString(), parameters);
    }

    protected virtual string BuildWhereClause(BaseSearchModel searchModel, Dictionary<string, object> parameters)
    {
        var conditions = new List<string>();

        if (!searchModel.IncludeDeleted)
        {
            conditions.Add("(n.deleted_at IS NULL)");
        }

        if (!string.IsNullOrWhiteSpace(searchModel.TextSearch))
        {
            var searchableFields = GetSearchableFields(searchModel.GetType());

            if (searchableFields.Any())
            {
                var searchConditions = searchableFields.Select(field =>
                    $"toLower(toString(n.{field})) CONTAINS $textSearch"
                ).ToList();

                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters["textSearch"] = searchModel.TextSearch.ToLower();
            }
        }

        var searchModelType = searchModel.GetType();
        var properties = searchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                       p.Name != nameof(BaseSearchModel.Page) &&
                       p.Name != nameof(BaseSearchModel.PageSize) &&
                       p.Name != nameof(BaseSearchModel.Skip) &&
                       p.Name != nameof(BaseSearchModel.TextSearch) &&
                       p.Name != nameof(BaseSearchModel.IncludeDeleted) &&
                       p.Name != nameof(BaseSearchModel.OrderByField) &&
                       p.Name != nameof(BaseSearchModel.Descending))
            .ToList();

        foreach (var property in properties)
        {
            var value = property.GetValue(searchModel);
            
            if (value == null || IsDefaultValue(value, property.PropertyType))
                continue;

            var parameterName = property.Name.ToLower();
            var nodePropertyName = GetNodePropertyName(property);

            if (property.PropertyType == typeof(string))
            {
                var stringValue = value.ToString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    conditions.Add($"toLower(n.{nodePropertyName}) CONTAINS ${parameterName}");
                    parameters[parameterName] = stringValue.ToLower();
                }
            }
            else if (property.PropertyType.IsEnum)
            {
                conditions.Add($"n.{nodePropertyName} = ${parameterName}");
                parameters[parameterName] = value.ToString();
            }
            else if (IsNumericType(property.PropertyType))
            {
                conditions.Add($"n.{nodePropertyName} = ${parameterName}");
                parameters[parameterName] = value;
            }
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                conditions.Add($"n.{nodePropertyName} = ${parameterName}");
                parameters[parameterName] = value;
            }
            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                conditions.Add($"n.{nodePropertyName} = ${parameterName}");
                parameters[parameterName] = value;
            }
            else if (property.PropertyType == typeof(List<string>))
            {
                var ids = value as List<string>;

                if (ids == null || ids.Count == 0 || ids.Any(string.IsNullOrWhiteSpace)) continue;

                var graphFieldAttr = property.GetCustomAttribute<GraphFieldAttribute>();
                if (graphFieldAttr != null && graphFieldAttr.IsRelationship)
                {
                    conditions.Add($"toLower(target_{parameterName}.{graphFieldAttr.TargetFieldName}) IN ${parameterName}");
                    parameters[parameterName] = ids.Select(id => id.ToLower()).ToList();
                }
                else
                {
                    conditions.Add($"n.{nodePropertyName} IN ${parameterName}");
                    parameters[parameterName] = ids;
                }
            }
        }

        return string.Join(" AND ", conditions);
    }

    protected virtual string BuildRelationshipMatches(BaseSearchModel searchModel, Dictionary<string, object> parameters)
    {
        var matchClauses = new List<string>();

        var searchModelType = searchModel.GetType();
        var properties = searchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var graphFieldAttr = property.GetCustomAttribute<GraphFieldAttribute>();
            if (graphFieldAttr != null && graphFieldAttr.IsRelationship)
            {
                var value = property.GetValue(searchModel);
                if (value != null && value is List<string> ids && ids.Count > 0 && !ids.Any(string.IsNullOrWhiteSpace))
                {
                    var parameterName = property.Name.ToLower();
                    var relationshipPatterns = new List<string>();

                    foreach (var relType in graphFieldAttr.RelationshipTypes!)
                    {
                        relationshipPatterns.Add($"-[:{relType}]->");
                    }

                    var relationshipPattern = relationshipPatterns.Count == 1
                        ? relationshipPatterns[0]
                        : $"-[:{string.Join("|", graphFieldAttr.RelationshipTypes!)}]->";

                    matchClauses.Add($"MATCH (n){relationshipPattern}(target_{parameterName}:{graphFieldAttr.TargetNodeLabel})");
                }
            }
        }

        return string.Join(" ", matchClauses);
    }

    protected virtual string GetNodePropertyName(string propertyName)
    {
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    protected virtual string GetNodePropertyName(PropertyInfo property)
    {
        var graphFieldAttr = property.GetCustomAttribute<GraphFieldAttribute>();
        if (graphFieldAttr != null && !string.IsNullOrEmpty(graphFieldAttr.FieldName))
        {
            return graphFieldAttr.FieldName;
        }

        return GetNodePropertyName(property.Name);
    }

    protected virtual List<string> GetSearchableFields(Type searchModelType)
    {
        var searchableFields = new List<string>();
        var properties = searchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var graphFieldAttr = property.GetCustomAttribute<GraphFieldAttribute>();
            if (graphFieldAttr != null && graphFieldAttr.IsSearchable && !string.IsNullOrEmpty(graphFieldAttr.FieldName))
            {
                searchableFields.Add(graphFieldAttr.FieldName);
            }
        }

        return searchableFields;
    }

    protected virtual bool IsDefaultValue(object value, Type type)
    {
        if (type.IsValueType)
        {
            return value.Equals(Activator.CreateInstance(type));
        }
        return value == null;
    }

    protected virtual bool IsNumericType(Type type)
    {
        var numericTypes = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal),
            typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?),
            typeof(int?), typeof(uint?), typeof(long?), typeof(ulong?),
            typeof(float?), typeof(double?), typeof(decimal?)
        };

        return numericTypes.Contains(type);
    }

    public virtual void Dispose()
    {
        _driver?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual string BuildSearchQuery(string searchTerm, int? skip, int? limit)
    {
        var query = $"MATCH (n:{_nodeLabel}) WHERE toLower(toString(n)) CONTAINS $searchQuery RETURN n ORDER BY n.createdAt DESC";
        if (skip.HasValue) query += $" SKIP {skip.Value}";
        if (limit.HasValue) query += $" LIMIT {limit.Value}";
        return query;
    }

    protected List<RelationshipPropertyInfo> GetRelationshipProperties()
    {
        return _relationshipCache.GetOrAdd(typeof(T), type =>
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<GraphFieldAttribute>()
                })
                .Where(x => x.Attribute?.IsRelationship == true)
                .Select(x => new RelationshipPropertyInfo
                {
                    Property = x.Property,
                    PropertyName = x.Property.Name,
                    Attribute = x.Attribute!,
                    IsCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(x.Property.PropertyType)
                                  && x.Property.PropertyType != typeof(string),
                    IsCountOnly = x.Attribute!.IsCountOnly,
                    RecordAlias = x.Property.Name.ToLower()
                })
                .ToList();
        });
    }

    protected virtual (string Query, Dictionary<string, object> Parameters) BuildSearchQueryWithRelationships(
        BaseSearchModel searchModel,
        List<RelationshipPropertyInfo> relationships)
    {
        var (baseQuery, parameters) = BuildSearchQuery(searchModel);

        var optionalMatches = new StringBuilder();
        var withItems = new List<string> { "n" };
        var returnItems = new List<string> { "n" };

        foreach (var rel in relationships)
        {
            var relType = rel.Attribute.RelationshipTypes![0];
            var directionEnum = ParseDirection(rel.Attribute.Direction);
            var direction = GetDirectionPattern(directionEnum, relType);
            var targetLabel = rel.Attribute.TargetNodeLabel;
            var alias = rel.RecordAlias;

            optionalMatches.AppendLine($"OPTIONAL MATCH (n){direction}(rel_{alias}:{targetLabel})");

            if (rel.IsCountOnly)
            {
                withItems.Add($"count(DISTINCT rel_{alias}) as {alias}_collection");
                returnItems.Add($"{alias}_collection");
            }
            else if (rel.IsCollection)
            {
                withItems.Add($"collect(DISTINCT rel_{alias}) as {alias}_collection");
                returnItems.Add($"{alias}_collection");
            }
            else
            {
                withItems.Add($"rel_{alias}");
                returnItems.Add($"rel_{alias}");
            }
        }

        var returnIndex = baseQuery.IndexOf("RETURN DISTINCT n");
        if (returnIndex == -1)
            return (baseQuery, parameters);

        var beforeReturn = baseQuery[..returnIndex];
        var afterReturn = baseQuery[(returnIndex + "RETURN DISTINCT n".Length)..];

        var enhancedQuery =
            $"{beforeReturn}" +
            $"{optionalMatches}" +
            $"WITH {string.Join(", ", withItems)}\n" +
            $"RETURN DISTINCT {string.Join(", ", returnItems)}" +
            $"{afterReturn}";

        return (enhancedQuery, parameters);
    }

    protected virtual RelationshipDirection ParseDirection(string direction)
    {
        return direction?.ToLower() switch
        {
            "outgoing" => RelationshipDirection.Outgoing,
            "incoming" => RelationshipDirection.Incoming,
            "both" => RelationshipDirection.Both,
            _ => RelationshipDirection.Outgoing
        };
    }

    protected virtual string GetDirectionPattern(RelationshipDirection direction, string relType)
    {
        return direction switch
        {
            RelationshipDirection.Outgoing => $"-[:{relType}]->",
            RelationshipDirection.Incoming => $"<-[:{relType}]-",
            RelationshipDirection.Both => $"-[:{relType}]-",
            _ => $"-[:{relType}]->"
        };
    }

    protected class RelationshipPropertyInfo
    {
        public PropertyInfo Property { get; set; } = null!;
        public string PropertyName { get; set; } = null!;
        public GraphFieldAttribute Attribute { get; set; } = null!;
        public bool IsCollection { get; set; }
        public bool IsCountOnly { get; set; }
        public string RecordAlias { get; set; } = null!;
    }

    public virtual async Task<bool> CreateRelationshipAsync(string sourceElementId, string relationshipType, string targetElementId, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var query = direction switch
            {
                RelationshipDirection.Outgoing => $@"
                    MATCH (source) WHERE elementId(source) = $sourceElementId
                    MATCH (target) WHERE elementId(target) = $targetElementId
                    MERGE (source)-[r:{relationshipType}]->(target)
                    RETURN r",
                RelationshipDirection.Incoming => $@"
                    MATCH (source) WHERE elementId(source) = $sourceElementId
                    MATCH (target) WHERE elementId(target) = $targetElementId
                    MERGE (source)<-[r:{relationshipType}]-(target)
                    RETURN r",
                RelationshipDirection.Both => $@"
                    MATCH (source) WHERE elementId(source) = $sourceElementId
                    MATCH (target) WHERE elementId(target) = $targetElementId
                    MERGE (source)-[r:{relationshipType}]-(target)
                    RETURN r",
                _ => $@"
                    MATCH (source) WHERE elementId(source) = $sourceElementId
                    MATCH (target) WHERE elementId(target) = $targetElementId
                    MERGE (source)-[r:{relationshipType}]->(target)
                    RETURN r"
            };

            _logger.LogDebug("Executing create relationship query: {Query}", query);

            var parameters = new Dictionary<string, object>
            {
                ["sourceElementId"] = sourceElementId,
                ["targetElementId"] = targetElementId
            };

            var result = await session.RunAsync(query, parameters);
            var record = await result.SingleAsync();

            return record != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating relationship of type {RelationshipType} between {SourceElementId} and {TargetElementId} with direction {Direction}",
                relationshipType, sourceElementId, targetElementId, direction);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public virtual async Task<bool> SyncRelationshipsAsync(string sourceElementId, string relationshipType, IEnumerable<string> targetElementIds, RelationshipDirection direction = RelationshipDirection.Outgoing, CancellationToken cancellationToken = default)
    {
        var session = _driver.AsyncSession();
        try
        {
            var targetIdsList = targetElementIds?.ToList() ?? new List<string>();

            var (deletePattern, mergePattern) = direction switch
            {
                RelationshipDirection.Outgoing => ($"-[oldRel:{relationshipType}]->", $"-[r:{relationshipType}]->"),
                RelationshipDirection.Incoming => ($"<-[oldRel:{relationshipType}]-", $"<-[r:{relationshipType}]-"),
                RelationshipDirection.Both => ($"-[oldRel:{relationshipType}]-", $"-[r:{relationshipType}]-"),
                _ => ($"-[oldRel:{relationshipType}]->", $"-[r:{relationshipType}]->")
            };

            var query = $@"
                MATCH (source) WHERE elementId(source) = $sourceElementId
                OPTIONAL MATCH (source){deletePattern}(oldTarget)
                WHERE NOT elementId(oldTarget) IN $targetElementIds
                DELETE oldRel
                WITH source
                UNWIND $targetElementIds AS targetId
                MATCH (target) WHERE elementId(target) = targetId
                MERGE (source){mergePattern}(target)
                RETURN count(target) as syncedCount";

            _logger.LogDebug("Executing sync relationships query: {Query}", query);

            var parameters = new Dictionary<string, object>
            {
                ["sourceElementId"] = sourceElementId,
                ["targetElementIds"] = targetIdsList
            };

            var result = await session.RunAsync(query, parameters);
            var record = await result.SingleAsync();

            return record != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing relationships of type {RelationshipType} from {SourceElementId} to targets with direction {Direction}",
                relationshipType, sourceElementId, direction);
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}

