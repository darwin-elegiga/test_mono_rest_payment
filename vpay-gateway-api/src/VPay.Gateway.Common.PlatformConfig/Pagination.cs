namespace VPay.Gateway.Common.PlatformConfig;

public static class Pagination
{
    public static Pagination<T> ToPagination<T>(this IList<T>? items)
        => new Pagination<T>
        {
            CurrentPage = 1,
            PageSize = items?.Count ?? 0,
            TotalPages = 1,
            TotalRows = items?.Count ?? 0,
            Items = items?.ToList() ?? new List<T>(),
        };

    public static Pagination<T> ToPagination<T>(this IList<T>? items, int currentPage, int pageSize, int totalPages, int totalRows)
        => new Pagination<T>
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalRows = totalRows,
            Items = items?.ToList() ?? new List<T>(),
        };
}

public class Pagination<T>
{
    public Pagination()
    {
        Items = new List<T>();
    }
    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalRows { get; init; }

    public int TotalPages { get; init; }

    [Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.Always)]
    public List<T> Items { get; init; }
}
