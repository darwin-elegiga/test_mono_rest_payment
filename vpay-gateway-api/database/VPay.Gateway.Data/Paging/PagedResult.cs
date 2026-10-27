using static VPay.Gateway.Data.Paging.Defaults;

namespace VPay.Gateway.Data.Paging;

public sealed class PagedResult<TItem>
{
    public IEnumerable<TItem> Items { get; }

    public int CurrentPage { get; }

    public int PageSize { get; }

    public int TotalRows { get; }

    public int TotalPages { get; }

    public PagedResult(IEnumerable<TItem> items, int currentPage, int pageSize, int totalRows)
    {
        if (currentPage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentPage), currentPage, nameof(currentPage) + " must be greater than zero");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, nameof(pageSize) + " must be greater than zero");
        }

        if (totalRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRows), totalRows, nameof(totalRows) + " must be greater than or equal to zero");
        }

        Items = items ?? throw new ArgumentNullException(nameof(items));
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalRows = totalRows;
        TotalPages = (int)Math.Ceiling(TotalRows / (double)PageSize);
    }

    public PagedResult(IEnumerable<TItem> items, PageModel pageModel) :
        this(items, pageModel?.CurrentPage ?? throw new ArgumentNullException(nameof(pageModel)), pageModel.PageSize, pageModel.TotalRows)
    { }

     public static PagedResult<TItem> Empty() =>
        new(Enumerable.Empty<TItem>(), DefaultPage, DefaultPageSize, 0);
}
