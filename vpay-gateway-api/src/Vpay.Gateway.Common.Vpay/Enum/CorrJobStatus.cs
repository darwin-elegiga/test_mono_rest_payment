using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Vpay.Gateway.Common.Vpay.Enum;

public enum CorrJobStatus
{
    Undefined,
    //
    // Summary:
    //     Abandoned
    [Display(Name = "Abandoned")]
    [Description("ABANDONED")]
    Abandoned,
    //
    // Summary:
    //     Bad Number
    [Display(Name = "Bad Number")]
    [Description("BADNUMBER")]
    BadNumber,
    //
    // Summary:
    //     Cancelled
    [Display(Name = "Cancelled")]
    [Description("CANCELLED")]
    Cancelled,
    //
    // Summary:
    //     Done
    [Display(Name = "Done")]
    [Description("DONE")]
    Done,
    //
    // Summary:
    //     Drop
    [Display(Name = "Drop")]
    [Description("DROPTOPRNT")]
    DropToPrint,
    //
    // Summary:
    //     Duplicate Number
    [Display(Name = "Duplicate Number")]
    [Description("DUPENUMBER")]
    DupeNumber,
    //
    // Summary:
    //     Failed
    [Display(Name = "Failed")]
    [Description("FAILED")]
    Failed,
    //
    // Summary:
    //     Hold
    [Display(Name = "Hold")]
    [Description("HOLD")]
    Hold,
    //
    // Summary:
    //     Killed
    [Display(Name = "Killed")]
    [Description("KILLED")]
    Killed,
    //
    // Summary:
    //     Mailed
    [Display(Name = "Mailed")]
    [Description("MAILED")]
    Mailed,
    //
    // Summary:
    //     Mailedfax
    [Display(Name = "Mailedfax")]
    [Description("MAILEDFAX")]
    Mailedfax,
    //
    // Summary:
    //     Pending
    [Display(Name = "Pending")]
    [Description("PENDING")]
    Pending,
    //
    // Summary:
    //     Pndprintm
    [Display(Name = "Pndprintm")]
    [Description("PNDPRINTM")]
    Pndprintm,
    //
    // Summary:
    //     Requested
    [Display(Name = "Requested")]
    [Description("REQUESTED")]
    Requested,
    //
    // Summary:
    //     Queued
    [Display(Name = "Queued")]
    [Description("QUEUED")]
    Queued,
    //
    // Summary:
    //     Retry
    [Display(Name = "Retry")]
    [Description("RETRY")]
    Retry,
    //
    // Summary:
    //     Retry Exceeded
    [Display(Name = "Retry Exceeded")]
    [Description("RTEXCEEDED")]
    RetryExceeded,
    //
    // Summary:
    //     Sent
    [Display(Name = "Sent")]
    [Description("SENT")]
    Sent,
    //
    // Summary:
    //     Stop
    [Display(Name = "Stop")]
    [Description("STOP")]
    Stop,
    //
    // Summary:
    //     Waiting
    [Display(Name = "Waiting")]
    [Description("WAITING")]
    Waiting,
    [Display(Name = "NODOCS")]
    [Description("NODOCS")]
    NoDocs,
    [Display(Name = "Generated")]
    [Description("GENERATED")]
    Generated,
    [Display(Name = "Paused")]
    [Description("PAUSED")]
    Paused
}
