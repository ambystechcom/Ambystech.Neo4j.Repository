using Ambystech.Neo4j.Repository.Contracts.Nodes;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Converters;

public interface INodeConverter<T> where T : BaseNode
{
    T ConvertFromNode(INode node);

    T ConvertFromRecord(IRecord record, string nodeAlias = "n");

    Dictionary<string, object> ConvertToProperties(T entity);
}

