using Newtonsoft.Json;

namespace Vpay.Gateway.Common.Vpay.Models;

public class Correspondence
{
    //
    // Summary:
    //     The correspondence identifier.
    [JsonIgnore]
    public long Id { get; set; }

    //
    // Summary:
    //     The sender name.
    public string? SenderName { get; set; }

    //
    // Summary:
    //     The first sender address.
    public string? SenderAddress1 { get; set; }

    //
    // Summary:
    //     The second sender address.
    public string? SenderAddress2 { get; set; }

    //
    // Summary:
    //     The sender city.
    public string? SenderCity { get; set; }

    //
    // Summary:
    //     The sender state.
    public string? SenderState { get; set; }

    //
    // Summary:
    //     The sender zip code.
    public string? SenderZip { get; set; }

    //
    // Summary:
    //     The sender fax number.
    public string? SenderFax { get; set; }

    //
    // Summary:
    //     The receiver name.
    public string? ReceiverName { get; set; }

    //
    // Summary:
    //     The receiver first address.
    public string? ReceiverAddress1 { get; set; }

    //
    // Summary:
    //     The receiver second address.
    public string? ReceiverAddress2 { get; set; }

    //
    // Summary:
    //     The receiver city.
    public string? ReceiverCity { get; set; }

    //
    // Summary:
    //     The receiver state.
    public string? ReceiverState { get; set; }

    //
    // Summary:
    //     The receiver zip code.
    public string? ReceiverZip { get; set; }

    //
    // Summary:
    //     The receiver fax number.
    public string? ReceiverFax { get; set; }

    //
    // Summary:
    //     The receiver phone number.
    public string? ReceiverPhone { get; set; }

    //
    // Summary:
    //     The receiver email.
    public string? ReceiverEmail { get; set; }

    //
    // Summary:
    //     The request date.
    public DateTime RequestDate { get; set; }

    //
    // Summary:
    //     The status date.
    public DateTime StatusDate { get; set; }

    //
    // Summary:
    //     The correspondence type.
    public string? CorrType { get; set; }

    //
    // Summary:
    //     The correspondence status.
    public string? Status { get; set; }

    //
    // Summary:
    //     The correspondence direction Ex. ongoing, incoming.
    public string? Direction { get; set; }

    ////
    //// Summary:
    ////     The list of correspondence jobs.
    //public ICollection<CorrJob> Jobs { get; set; } = new List<CorrJob>();

}
