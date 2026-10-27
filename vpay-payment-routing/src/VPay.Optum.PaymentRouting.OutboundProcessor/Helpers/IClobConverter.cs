using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Helpers
{
    public interface IClobConverter
    {
        public Task<TS835> ClobTo835(string paymentClob);
        public Task<string> Ts835ToClob(TS835 ts835);
    }
}
