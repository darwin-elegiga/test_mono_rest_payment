namespace VPay.Gateway.Common.PlatformConfig;

public class PaginationOrArrayNewtonsoftJsonConverter<T> : Newtonsoft.Json.JsonConverter<Pagination<T>>
{
    public override Pagination<T>? ReadJson(
        Newtonsoft.Json.JsonReader reader,
        Type objectType,
        Pagination<T>? existingValue,
        bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => reader.TokenType switch
        {
            Newtonsoft.Json.JsonToken.StartArray => serializer.Deserialize<T[]>(reader).ToPagination(),
            Newtonsoft.Json.JsonToken.StartObject => serializer.Deserialize<PaginationMapping>(reader)?.ToPagination(),
            _ => throw new Newtonsoft.Json.JsonException()
        };

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, Pagination<T>? value, Newtonsoft.Json.JsonSerializer serializer)
        => serializer.Serialize(writer, PaginationMapping.FromPagination(value));

    /// <summary>
    /// Used to avoid infinite loops / stack overflow errors by serializing a type
    /// that does not have any related JsonConverters
    /// </summary>
    private class PaginationMapping
    {
        public static PaginationMapping? FromPagination(Pagination<T>? pagination)
            => pagination == null ? null : new()
            {
                Items = pagination.Items,
                CurrentPage = pagination.CurrentPage,
                PageSize = pagination.PageSize,
                TotalPages = pagination.TotalPages,
                TotalRows = pagination.TotalRows
            };
        public Pagination<T> ToPagination()
            => Items.ToPagination(CurrentPage, PageSize, TotalPages, TotalRows);
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRows { get; set; }

        public int TotalPages { get; set; }

        [Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public List<T> Items { get; set; } = new List<T>();
    }
}
