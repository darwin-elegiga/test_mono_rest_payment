using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Green Arrow Api Client
    /// </summary>
    public interface IGreenArrowApiClient
    {
        /// <summary>
        /// Send Email
        /// </summary>
        /// <param name="emailRequest"></param>
        /// <returns></returns>
        Task SendEmail(IEmailRequest emailRequest);
    }
}
