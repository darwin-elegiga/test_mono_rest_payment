namespace VPay.DocSys.Generation.Common
{
    public enum ReportRequestStatus
    {
        /// <summary>
        /// Unknown State
        /// Is considered complete? NO
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Request Made
        /// Is considered complete? NO
        /// </summary>
        RequestMade = 1,

        /// <summary>
        /// Report Starting
        /// Is considered complete? NO
        /// </summary>
        Starting = 2,

        /// <summary>
        /// Fetching Source
        /// Is considered complete? NO
        /// </summary>
        FetchingSource = 3,

        /// <summary>
        /// Comparing Documents
        /// Is considered complete? NO
        /// </summary>
        Comparing = 4,

        /// <summary>
        /// Successfully Completed
        /// Is considered complete? YES
        /// </summary>
        Success = 5,

        /// <summary>
        /// Completed with Error(s)
        /// Is considered complete? YES
        /// </summary>
        Error = 99
    }
}
