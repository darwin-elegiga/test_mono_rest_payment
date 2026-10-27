namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Configuration values used for fax job abandon after calculation
    /// </summary>
    public class FaxJobSettings
    {
        /// <summary>
        /// value used for scaling the wait time
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// seconds added to account for dialing and connecting
        /// </summary>
        public int PaddingInSeconds { get; set; }

        /// <summary>
        /// Maximum hours to cap the abandon after calculation
        /// </summary>
        public int MaxHoursCap { get; set; }
    }
}
