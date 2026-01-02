using Ambystech.Neo4j.Repository.Contracts.Nodes;
using Ambystech.Neo4j.Repository.Converters;
using FluentAssertions;
using Moq;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Tests.Converters;

[TestClass]
public class DefaultNodeConverterTests
{
    [TestMethod]
    public void ConvertFromNode_ShouldMapProperties()
    {
        var converter = new DefaultNodeConverter<TestNode>();
        var nodeMock = new Mock<INode>();
        
        nodeMock.Setup(n => n.ElementId).Returns("test-id");
        nodeMock.Setup(n => n.Properties).Returns(new Dictionary<string, object>
        {
            { "name", "Test Name" },
            { "created_at", DateTime.UtcNow }
        });

        var result = converter.ConvertFromNode(nodeMock.Object);

        result.Should().NotBeNull();
        result.Id.Should().Be("test-id");
    }

    [TestMethod]
    public void ConvertToProperties_ShouldExcludeNullValues()
    {
        var converter = new DefaultNodeConverter<TestNode>();
        var node = new TestNode
        {
            Id = "test-id",
            Name = "Test"
        };

        var properties = converter.ConvertToProperties(node);

        properties.Should().NotBeNull();
        properties.Should().ContainKey("name");
        properties["name"].Should().Be("Test");
    }
}

public class TestNode : BaseNode
{
    public string Name { get; set; } = string.Empty;
}

