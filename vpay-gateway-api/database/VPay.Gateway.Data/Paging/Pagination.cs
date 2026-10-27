namespace VPay.Gateway.Data.Paging;

public class Pagination<T>
{
    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalRows { get; set; }

    public int TotalPages { get; set; }

    public List<T> Items { get; } = new List<T>();
}
