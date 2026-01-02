using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Ambystech.Neo4j.Repository.Contracts.Attributes;
using Ambystech.Neo4j.Repository.Contracts.Nodes;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Converters;

public class DefaultNodeConverter<T> : INodeConverter<T> where T : BaseNode, new()
{
    private static readonly ConcurrentDictionary<Type, List<PropertyMapping>> _propertyCache = new();

    public virtual T ConvertFromNode(INode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        var entity = new T
        {
            Id = node.ElementId
        };

        var mappings = GetPropertyMappings();

        foreach (var mapping in mappings)
        {
            if (node.Properties.TryGetValue(mapping.Neo4jPropertyName, out var value) && value != null)
            {
                try
                {
                    var convertedValue = ConvertValue(value, mapping.PropertyType);
                    mapping.Property.SetValue(entity, convertedValue);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        if (node.Properties.TryGetValue("created_at", out var createdAt))
        {
            entity.CreatedAt = ConvertToDateTime(createdAt);
        }

        if (node.Properties.TryGetValue("updated_at", out var updatedAt))
        {
            entity.UpdatedAt = ConvertToDateTime(updatedAt);
        }

        return entity;
    }

    public virtual T ConvertFromRecord(IRecord record, string nodeAlias = "n")
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!record.Keys.Contains(nodeAlias))
            throw new ArgumentException($"Record does not contain node with alias '{nodeAlias}'", nameof(nodeAlias));

        var node = record[nodeAlias].As<INode>();
        var entity = ConvertFromNode(node);

        var relationshipProps = GetRelationshipProperties();
        foreach (var relProp in relationshipProps.Where(r => r.IsCountOnly))
        {
            var alias = relProp.PropertyName.ToLower() + "_collection";
            if (record.Keys.Contains(alias))
            {
                var countValue = record[alias].As<long>();
                relProp.Property.SetValue(entity, (int)countValue);
            }
        }

        return entity;
    }

    public virtual Dictionary<string, object> ConvertToProperties(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = new Dictionary<string, object>();
        var mappings = GetPropertyMappings();

        foreach (var mapping in mappings)
        {
            var value = mapping.Property.GetValue(entity);

            if (value == null || IsDefaultValue(value, mapping.PropertyType))
                continue;

            if (value is string str && string.IsNullOrWhiteSpace(str))
                continue;

            properties[mapping.Neo4jPropertyName] = value;
        }

        return properties;
    }

    protected List<PropertyMapping> GetPropertyMappings()
    {
        return _propertyCache.GetOrAdd(typeof(T), type =>
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.Name != nameof(BaseNode.Id))
                .Where(p => p.Name != nameof(BaseNode.CreatedAt))
                .Where(p => p.Name != nameof(BaseNode.UpdatedAt))
                .Where(p => !IsRelationshipProperty(p))
                .Select(p => new PropertyMapping
                {
                    Property = p,
                    PropertyType = p.PropertyType,
                    Neo4jPropertyName = GetNeo4jPropertyName(p)
                })
                .ToList();
        });
    }

    protected virtual bool IsRelationshipProperty(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<GraphFieldAttribute>();
        return attr?.IsRelationship == true;
    }

    protected virtual List<RelationshipPropertyInfo> GetRelationshipProperties()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                IsCountOnly = x.Attribute!.IsCountOnly
            })
            .ToList();
    }

    protected class RelationshipPropertyInfo
    {
        public PropertyInfo Property { get; set; } = null!;
        public string PropertyName { get; set; } = null!;
        public GraphFieldAttribute Attribute { get; set; } = null!;
        public bool IsCountOnly { get; set; }
    }

    protected virtual string GetNeo4jPropertyName(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<GraphFieldAttribute>();
        if (attr != null && !string.IsNullOrEmpty(attr.FieldName))
            return attr.FieldName;

        return ToSnakeCase(property.Name);
    }

    protected virtual object? ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(DateTime))
        {
            return ConvertToDateTime(value);
        }

        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, value.ToString()!, ignoreCase: true);
        }

        if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
        {
            return ConvertToList(value, underlyingType);
        }

        if (underlyingType.IsPrimitive || underlyingType == typeof(string) || underlyingType == typeof(decimal))
        {
            return Convert.ChangeType(value, underlyingType);
        }

        return Convert.ChangeType(value, underlyingType);
    }

    protected virtual DateTime? ConvertToDateTime(object value)
    {
        if (value == null)
            return null;

        if (value is ZonedDateTime zonedDateTime)
        {
            return zonedDateTime.ToDateTimeOffset().DateTime;
        }

        if (DateTime.TryParse(value.ToString(), out var dateTime))
        {
            return dateTime;
        }

        return null;
    }

    protected virtual object? ConvertToList(object value, Type targetType)
    {
        if (value is not IList<object> sourceList)
            return null;

        var elementType = targetType.IsGenericType
            ? targetType.GetGenericArguments()[0]
            : typeof(object);

        var listType = typeof(List<>).MakeGenericType(elementType);
        var targetList = (IList?)Activator.CreateInstance(listType);

        if (targetList == null)
            return null;

        foreach (var item in sourceList)
        {
            if (item == null)
                continue;

            try
            {
                var convertedItem = ConvertValue(item, elementType);
                targetList.Add(convertedItem);
            }
            catch
            {
                continue;
            }
        }

        return targetList;
    }

    protected virtual bool IsDefaultValue(object value, Type type)
    {
        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }
        return false;
    }

    protected virtual string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }

    protected class PropertyMapping
    {
        public PropertyInfo Property { get; set; } = null!;
        public Type PropertyType { get; set; } = null!;
        public string Neo4jPropertyName { get; set; } = null!;
    }
}

