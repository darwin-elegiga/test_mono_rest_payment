namespace Vpay.Gateway.Common.Vpay.Models;
/// <summary>
/// Represents a request to search transactions within Elastic.
/// </summary>
public class TransactionSearchRequest
{
    /// <summary>
    /// A comma delimited string of terms to search on.
    /// </summary>
    public string Term { get; set; }

    /// <summary>
    /// The start of the date range used to search.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end of the date range for the search.
    /// </summary>
    public DateTime EndDate { get; set; } = DateTime.Today.AddTicks(-1).ToUniversalTime();

    /// <summary>
    /// The initial record index for returning the result size.
    /// </summary>
    public int From { get; set; }

    /// <summary>.
    /// The maximum number of records to return.
    /// </summary>
    public int Size { get; set; } = 100;

    /// <summary>
    /// The sort used for returning transactions
    /// </summary>
    public TransactionSort Sort { get; set; } = new TransactionSort();

    /// <summary>
    /// Statuses to filter by
    /// </summary>
    public List<string> Statuses { get; set; } = new List<string>();

    /// <summary>
    /// Fields to have totals for
    /// </summary>
    public List<string> FieldsToTotal { get; set; } = new List<string>();
}
