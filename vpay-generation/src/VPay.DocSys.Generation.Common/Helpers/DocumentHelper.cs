namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class DocumentHelper
    {
        /// <summary>
        /// Create the revision string from revision number
        /// </summary>
        /// <param name="revision">revision value</param>
        /// <param name="prepend">string to prepend</param>
        /// <returns></returns>
        public static string PrependToRevisionString(this int revision, string prepend)
        {
            return revision <= 1 ? string.Empty : $"{prepend}{revision:D2}";
        }
    }
}
