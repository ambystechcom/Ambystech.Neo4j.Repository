using Ambystech.Neo4j.Repository.Contracts.Nodes;
using Ambystech.Neo4j.Repository.Converters;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver;

namespace Ambystech.Neo4j.Repository.Tests.Repositories;

[TestClass]
public class BaseGraphRepositoryTests
{
    private IFixture _fixture = null!;
    private Mock<IDriver> _driverMock = null!;
    private Mock<ILogger<TestRepository>> _loggerMock = null!;
    private Mock<INodeConverter<TestNode>> _converterMock = null!;
    private TestRepository _repository = null!;
    private TestAsyncSession _testSession = null!;

    [TestInitialize]
    public void Setup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        
        _driverMock = _fixture.Freeze<Mock<IDriver>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<TestRepository>>>();
        _converterMock = _fixture.Freeze<Mock<INodeConverter<TestNode>>>();

        _testSession = new TestAsyncSession();
        _driverMock.Setup(d => d.AsyncSession()).Returns(_testSession);

        _repository = new TestRepository(_driverMock.Object, _loggerMock.Object, _converterMock.Object);
    }

    [TestMethod]
    public async Task CountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        var countRecordMock = new Mock<IRecord>();
        countRecordMock.Setup(r => r["totalCount"]).Returns(10L);
        
        var countResultCursor = new TestResultCursor(new[] { countRecordMock.Object });
        _testSession.SetRunAsyncResult(countResultCursor);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(10L);
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnSearchResult()
    {
        // Arrange
        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ElementId).Returns("node1");
        nodeMock.Setup(n => n.Properties).Returns(new Dictionary<string, object>());

        var recordMock = new Mock<IRecord>();
        recordMock.Setup(r => r["n"]).Returns(nodeMock.Object);

        var searchResultCursor = new TestResultCursor(new[] { recordMock.Object });
        var countRecordMock = new Mock<IRecord>();
        countRecordMock.Setup(r => r["totalCount"]).Returns(1L);
        var countResultCursor = new TestResultCursor(new[] { countRecordMock.Object });

        _testSession.SetRunAsyncResults(searchResultCursor, countResultCursor);

        var testNode = new TestNode { Id = "node1" };
        _converterMock.Setup(c => c.ConvertFromNode(It.IsAny<INode>())).Returns(testNode);

        // Act
        var searchResult = await _repository.GetAllAsync();

        // Assert
        searchResult.Should().NotBeNull();
        searchResult.Results.Should().NotBeNull();
        searchResult.TotalResults.Should().Be(1L);
    }

    private class TestAsyncSession : IAsyncSession
    {
        private readonly Queue<IResultCursor> _runResults = new();
        private IResultCursor? _singleResult;
        private readonly Mock<Bookmarks> _bookmarksMock;

        public TestAsyncSession()
        {
            _bookmarksMock = new Mock<Bookmarks>();
        }

        public void SetRunAsyncResult(IResultCursor result)
        {
            _singleResult = result;
        }

        public void SetRunAsyncResults(params IResultCursor[] results)
        {
            foreach (var result in results)
            {
                _runResults.Enqueue(result);
            }
        }

        public Bookmark LastBookmark => throw new NotSupportedException("Bookmark is obsolete, use LastBookmarks");
        public Bookmarks LastBookmarks => _bookmarksMock.Object;
        public SessionConfig SessionConfig => throw new NotImplementedException();

        public IResultCursor Run(string query) => throw new NotImplementedException();
        public IResultCursor Run(string query, object parameters) => throw new NotImplementedException();
        public Task<IResultCursor> RunAsync(string query) => Task.FromResult(_singleResult ?? _runResults.Dequeue());
        public Task<IResultCursor> RunAsync(string query, object parameters) => Task.FromResult(_runResults.Dequeue());
        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters) => Task.FromResult(_runResults.Dequeue());
        public Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action) => Task.FromResult(_singleResult ?? _runResults.Dequeue());
        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters, Action<TransactionConfigBuilder> action) => Task.FromResult(_runResults.Dequeue());
        public Task<IResultCursor> RunAsync(Query query) => throw new NotImplementedException();
        public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task<T> ExecuteReadAsync<T>(Func<IAsyncTransaction, Task<T>> work) => throw new NotImplementedException();
        public Task<T> ExecuteWriteAsync<T>(Func<IAsyncTransaction, Task<T>> work) => throw new NotImplementedException();
        public Task<T> ExecuteReadAsync<T>(Func<IAsyncTransaction, Task<T>> work, TransactionConfig transactionConfig) => throw new NotImplementedException();
        public Task<T> ExecuteWriteAsync<T>(Func<IAsyncTransaction, Task<T>> work, TransactionConfig transactionConfig) => throw new NotImplementedException();
        public Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task ExecuteReadAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task CloseAsync() => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<IAsyncTransaction> BeginTransactionAsync() => throw new NotImplementedException();
        public Task<IAsyncTransaction> BeginTransactionAsync(TransactionConfig transactionConfig) => throw new NotImplementedException();
        public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode) => throw new NotImplementedException();
        public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work) => throw new NotImplementedException();
        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work) => throw new NotImplementedException();
        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work) => throw new NotImplementedException();
        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work) => throw new NotImplementedException();
        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action) => throw new NotImplementedException();
    }

    private class TestResultCursor : IResultCursor
    {
        private readonly IRecord[] _records;

        public TestResultCursor(IRecord[] records)
        {
            _records = records;
        }

        public IReadOnlyList<string> Keys => _records.FirstOrDefault()?.Keys ?? new List<string>();
        public IRecord Current => _records.FirstOrDefault() ?? throw new InvalidOperationException();
        public bool IsOpen => true;

        public Task<string[]> KeysAsync() => Task.FromResult(Keys.ToArray());
        public Task<string[]> KeysAsync(CancellationToken cancellationToken) => Task.FromResult(Keys.ToArray());
        public Task<bool> FetchAsync() => Task.FromResult(false);
        public Task<bool> FetchAsync(CancellationToken cancellationToken) => Task.FromResult(false);
        public IResultSummary Consume() => throw new NotImplementedException();
        public Task<IResultSummary> ConsumeAsync() => throw new NotImplementedException();
        public Task<IResultSummary> ConsumeAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public IRecord Peek() => _records.FirstOrDefault()!;
        public Task<IRecord?> PeekAsync() => Task.FromResult<IRecord?>(_records.FirstOrDefault());
        public Task<IRecord?> PeekAsync(CancellationToken cancellationToken) => Task.FromResult<IRecord?>(_records.FirstOrDefault());
        public IRecord Single() => _records.Single();
        public Task<IRecord> SingleAsync() => Task.FromResult(_records.Single());
        public Task<IRecord> SingleAsync(CancellationToken cancellationToken) => Task.FromResult(_records.Single());
        public List<IRecord> ToList() => _records.ToList();
        public Task<List<IRecord>> ToListAsync() => Task.FromResult(_records.ToList());
        public Task<List<IRecord>> ToListAsync(CancellationToken cancellationToken) => Task.FromResult(_records.ToList());
        public IAsyncEnumerator<IRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncRecordEnumerator(_records);
        }

        private class AsyncRecordEnumerator : IAsyncEnumerator<IRecord>
        {
            private readonly IRecord[] _records;
            private int _index = -1;

            public AsyncRecordEnumerator(IRecord[] records)
            {
                _records = records;
            }

            public IRecord Current => _records[_index];
            public ValueTask<bool> MoveNextAsync() => new(++_index < _records.Length);
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}

public class TestNode : BaseNode
{
    public string Name { get; set; } = string.Empty;
}

public class TestRepository : BaseGraphRepository<TestNode>
{
    public TestRepository(IDriver driver, ILogger<TestRepository> logger, INodeConverter<TestNode> nodeConverter)
        : base(driver, logger, nodeConverter)
    {
    }
}
