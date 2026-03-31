namespace StockFlow.Web.Common
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class PagedQuery
    {
        private int _page = 1;
        private int _pageSize = 20;
        private const int MAX_PAGE_SIZE = 100;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MAX_PAGE_SIZE ? MAX_PAGE_SIZE : value < 1 ? 1 : value;
        }

        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    public static class PagedResultExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PagedQuery paged,
            CancellationToken ct = default)
        {
            var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(query, ct);

            var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(
                    query
                        .Skip((paged.Page - 1) * paged.PageSize)
                        .Take(paged.PageSize),
                    ct);

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }
    }
}