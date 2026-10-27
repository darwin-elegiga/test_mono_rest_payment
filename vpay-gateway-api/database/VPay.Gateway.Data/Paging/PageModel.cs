namespace VPay.Gateway.Data.Paging;

public sealed class PageModel
{
    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalRows { get; set; }
}
