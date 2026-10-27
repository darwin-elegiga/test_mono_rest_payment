using Newtonsoft.Json;

namespace VPay.Distribution.Http.Abstractions
{
    public class Pagination<T>
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRows { get; set; }

        public int TotalPages { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<T> Items { get; } = [];

        public Pagination() { }

        public Pagination(IEnumerable<T> items) => Items = [.. items];

        public Pagination(params T[] items) => Items = [.. items];
    }
}
