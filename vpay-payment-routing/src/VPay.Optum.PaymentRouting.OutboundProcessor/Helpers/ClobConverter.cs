using System.IO;
using System.Text;
using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;
using VPay.EdiFabric;
using VPay.EdiFabric.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Helpers
{
    public class ClobConverter : IClobConverter
    {
        public async Task<TS835> ClobTo835(string paymentClob)
        {
            await using var paymentStream = paymentClob.ToStream();
            using var ediPaymentReader = new VPay835Reader(paymentStream, false, true);
            TS835 ts835 = await ediPaymentReader.Read835Async().ConfigureAwait(false);
            return ts835;
        }

        public async Task<string> Ts835ToClob(TS835 ts835)
        {
            string outboundPaymentClob;
            await using (var hipaaStream = new MemoryStream())
            {
                using (var hipaa835Writer = new VPay835Writer(hipaaStream, encoding: new UTF8Encoding(false)))
                {
                    await hipaa835Writer.Write835TransactionAsync(ts835).ConfigureAwait(false);
                }

                hipaaStream.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(hipaaStream);
                outboundPaymentClob = await reader.ReadToEndAsync();
            }

            return outboundPaymentClob;
        }
    }
}
