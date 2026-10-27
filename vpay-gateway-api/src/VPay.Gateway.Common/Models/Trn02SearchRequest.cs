using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VPay.Gateway.Common.Models;
[JsonSerializable(typeof(Trn02SearchRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    GenerationMode = JsonSourceGenerationMode.Serialization)]
public sealed class Trn02SearchRequest
{
    /// <summary>
    /// The date range begin date.
    /// Required.
    /// </summary>
    [Required]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The date range end date.
    /// Required.
    /// </summary>
    [Required]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Payment Id/Number.
    /// Required.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string? Trn02 { get; set; }

    /// <summary>
    /// Provider TIN.
    /// Required.
    /// </summary>
    /// [MaxLength(9)]
    [Required]
    [MaxLength(9)]
    public string? ProviderTin { get; set; }

    /// <summary>
    /// Index of the page to retrieve. Page index starts at 1.
    /// Optional, defaults to 1 when null or less than 1.
    /// </summary>
    [Range(0, 10000)]
    public int? Page { get; set; }

    /// <summary>
    /// The number of results per page. Can be any value greater than 0.
    /// Optional, defaults to 50 when null or less than 1.
    /// </summary>
    [Range(0, 1000)]
    public int? PageSize { get; set; }
}
