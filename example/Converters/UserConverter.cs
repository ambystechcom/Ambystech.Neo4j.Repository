using Ambystech.Neo4j.Repository.Converters;
using Ambystech.Neo4j.Repository.Example.Models;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Example.Converters;

public class UserConverter(IServiceProvider serviceProvider) : DefaultNodeConverter<User>
{
    public override User ConvertFromRecord(IRecord record, string nodeAlias = "n")
    {
        var user = base.ConvertFromRecord(record, nodeAlias);
        
        INodeConverter<Post>? postConverter = null;
        
        if (record.Keys.Contains("likedposts_collection"))
        {
            var likedPostsValue = record["likedposts_collection"];
            if (likedPostsValue != null && !likedPostsValue.Equals(false))
            {
                postConverter ??= serviceProvider.GetRequiredService<INodeConverter<Post>>();
                var postNodes = likedPostsValue.As<List<INode>>();
                user.LikedPosts = [.. postNodes
                    .Where(n => n != null)
                    .Select(n => postConverter.ConvertFromNode(n))];
            }
        }
        
        if (record.Keys.Contains("dislikedposts_collection"))
        {
            var dislikedPostsValue = record["dislikedposts_collection"];
            if (dislikedPostsValue != null && !dislikedPostsValue.Equals(false))
            {
                postConverter ??= serviceProvider.GetRequiredService<INodeConverter<Post>>();
                var postNodes = dislikedPostsValue.As<List<INode>>();
                user.DislikedPosts = [.. postNodes
                    .Where(n => n != null)
                    .Select(n => postConverter.ConvertFromNode(n))];
            }
        }
        
        return user;
    }
}

