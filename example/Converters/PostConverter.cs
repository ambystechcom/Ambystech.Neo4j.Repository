using Ambystech.Neo4j.Repository.Converters;
using Ambystech.Neo4j.Repository.Example.Models;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Example.Converters;

public class PostConverter(IServiceProvider serviceProvider) : DefaultNodeConverter<Post>
{
    public override Post ConvertFromRecord(IRecord record, string nodeAlias = "n")
    {
        var post = base.ConvertFromRecord(record, nodeAlias);
        
        INodeConverter<User>? userConverter = null;
        
        if (record.Keys.Contains("likedby_collection"))
        {
            var likedByValue = record["likedby_collection"];
            if (likedByValue != null && !likedByValue.Equals(false))
            {
                userConverter ??= serviceProvider.GetRequiredService<INodeConverter<User>>();
                var userNodes = likedByValue.As<List<INode>>();
                post.LikedBy = [.. userNodes
                    .Where(n => n != null)
                    .Select(n => userConverter.ConvertFromNode(n))];
            }
        }
        
        if (record.Keys.Contains("dislikedby_collection"))
        {
            var dislikedByValue = record["dislikedby_collection"];
            if (dislikedByValue != null && !dislikedByValue.Equals(false))
            {
                userConverter ??= serviceProvider.GetRequiredService<INodeConverter<User>>();
                var userNodes = dislikedByValue.As<List<INode>>();
                post.DislikedBy = [.. userNodes
                    .Where(n => n != null)
                    .Select(n => userConverter.ConvertFromNode(n))];
            }
        }
        
        return post;
    }
}

