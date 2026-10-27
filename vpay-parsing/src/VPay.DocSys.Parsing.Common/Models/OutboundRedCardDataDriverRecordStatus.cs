using System.ComponentModel;

namespace VPay.DocSys.Parsing.Common
{
    public enum OutboundRedCardDataDriverRecordStatus
    {
        [Description("Unknown")]
        Unknown, // when a record is first created

        [Description("No container or database entry for GUID")]
        DataNotFound,

        [Description("Cover sheet missing from container")]
        CoverSheetMissing,

        [Description("Writing tar member failed")]
        ArchiveUpdateFailed,

        [Description("Successful")]
        Validated,
    }
}
