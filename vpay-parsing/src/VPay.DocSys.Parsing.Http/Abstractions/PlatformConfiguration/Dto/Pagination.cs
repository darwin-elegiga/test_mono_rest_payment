using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class Pagination<T>
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRows { get; set; }

        public int TotalPages { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<T> Items { get; } = new List<T>();
        public Pagination() { }
        public Pagination(IEnumerable<T> items) => Items = items.ToList();
        public Pagination(params T[] items) => Items = items.ToList();
    }
}
