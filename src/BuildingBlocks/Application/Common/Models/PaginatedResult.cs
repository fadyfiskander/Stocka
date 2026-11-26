namespace BuildingBlocks.Application.Common.Models;

public record PaginatedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class PaginationExtensions
{
    public static PaginatedResult<T> ToPaginated<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var list = source.ToList();
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedResult<T>(items, total, page, pageSize);
    }
}
