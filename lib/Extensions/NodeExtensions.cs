using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Extensions;

public static class NodeExtensions
{
    public static T GetProperty<T>(this INode node, string propertyName, T defaultValue)
    {
        if (node.Properties.TryGetValue(propertyName, out var value) && value != null)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.ToString();
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value.ToString(), out var intValue))
                {
                    return (T)(object)intValue;
                }
            }
            else if (typeof(T) == typeof(long))
            {
                if (long.TryParse(value.ToString(), out var longValue))
                {
                    return (T)(object)longValue;
                }
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value.ToString(), out var doubleValue))
                {
                    return (T)(object)doubleValue;
                }
            }
            else if (typeof(T) == typeof(decimal))
            {
                if (decimal.TryParse(value.ToString(), out var decimalValue))
                {
                    return (T)(object)decimalValue;
                }
            }
            else if (typeof(T) == typeof(float))
            {
                if (float.TryParse(value.ToString(), out var floatValue))
                {
                    return (T)(object)floatValue;
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(value.ToString(), out var boolValue))
                {
                    return (T)(object)boolValue;
                }
            }
            else if (typeof(T) == typeof(DateTime))
            {
                if (DateTime.TryParse(value.ToString(), out var dateValue))
                {
                    return (T)(object)dateValue;
                }
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                if (DateTime.TryParse(value.ToString(), out var nullableDateValue))
                {
                    return (T)(object)nullableDateValue;
                }
            }
            else if (typeof(T) == typeof(Guid))
            {
                if (Guid.TryParse(value.ToString(), out var guidValue))
                {
                    return (T)(object)guidValue;
                }
            }
            else if (typeof(T) == typeof(Guid?))
            {
                if (Guid.TryParse(value.ToString(), out var nullableGuidValue))
                {
                    return (T)(object)nullableGuidValue;
                }
            }
            else if (typeof(T).IsEnum)
            {
                if (Enum.TryParse(typeof(T), value.ToString(), true, out var enumValue) && enumValue != null)
                {
                    return (T)enumValue;
                }
            }
            else
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        return defaultValue;
    }

    public static string GetSafeStringProperty(this INode node, string propertyName, string defaultValue = "")
    {
        return node.GetProperty(propertyName, defaultValue);
    }

    public static int GetSafeIntProperty(this INode node, string propertyName, int defaultValue = 0)
    {
        return node.GetProperty(propertyName, defaultValue);
    }

    public static bool GetSafeBoolProperty(this INode node, string propertyName, bool defaultValue = false)
    {
        return node.GetProperty(propertyName, defaultValue);
    }

    public static DateTime GetSafeDateTimeProperty(this INode node, string propertyName, DateTime defaultValue = default)
    {
        return node.GetProperty(propertyName, defaultValue);
    }

    public static DateTime? GetSafeNullableDateTimeProperty(this INode node, string propertyName, DateTime? defaultValue = null)
    {
        return node.GetProperty(propertyName, defaultValue);
    }

    public static List<T> GetSafeListProperty<T>(this INode node, string propertyName)
    {
        if (!node.Properties.TryGetValue(propertyName, out var value) || value == null)
        {
            return [];
        }

        if (value is not IList<object> list)
        {
            return [];
        }

        var result = new List<T>();
        foreach (var item in list)
        {
            if (item == null) continue;

            try
            {
                if (typeof(T) == typeof(string))
                {
                    result.Add((T)(object)item.ToString()!);
                }
                else if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(item.ToString(), out var intValue))
                    {
                        result.Add((T)(object)intValue);
                    }
                }
                else if (typeof(T) == typeof(long))
                {
                    if (long.TryParse(item.ToString(), out var longValue))
                    {
                        result.Add((T)(object)longValue);
                    }
                }
                else if (typeof(T) == typeof(double))
                {
                    if (double.TryParse(item.ToString(), out var doubleValue))
                    {
                        result.Add((T)(object)doubleValue);
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(item.ToString(), out var boolValue))
                    {
                        result.Add((T)(object)boolValue);
                    }
                }
                else
                {
                    result.Add((T)Convert.ChangeType(item, typeof(T)));
                }
            }
            catch
            {
                continue;
            }
        }

        return result;
    }
}

