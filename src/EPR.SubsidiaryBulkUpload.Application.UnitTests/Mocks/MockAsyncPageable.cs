using Azure;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;

internal class MockAsyncPageable<T> : AsyncPageable<T>
{
    private readonly List<T> _items;

    public MockAsyncPageable(List<T> items) => _items = items;

    public override IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
    {
        var page = Page<T>.FromValues(_items, null, new Mock<Response>().Object);
        return new[] { page }.ToAsyncEnumerable();
    }
}
