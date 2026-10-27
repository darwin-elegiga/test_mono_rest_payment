namespace VPay.DocSys.Parsing
{
    public class OtbDataServiceSettings
    {
        /// <summary>
        /// The flag to determine if the OTB data should be extracted from the OTB API.
        /// </summary>
        public bool UseApi { get; set; } = false;

        /// <summary>
        /// The flag to determine if the OTB data should be extracted from the OTB File.
        /// </summary>
        public bool UseFile { get; set; } = true;
    }
}
