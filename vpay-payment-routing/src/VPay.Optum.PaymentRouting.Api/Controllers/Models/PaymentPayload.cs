using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.MassTransit.Serialization;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class PaymentPayload
    {
        public string BatchID { get; set; }
        public int? FileProcID { get; set; }
        public string PayerAlias { get; set; }
        public string TransactionType { get; set; }
        public string ProviderTIN { get; set; }
        public string PayerID { get; set; }
        public string SubpayerID { get; set; }
        public string ZipCode { get; set; }
        public string UnconsolPaymentNumber { get; set; }
        public decimal? UnconsolPaymentAmount { get; set; }
        public string BlobStorageURI { get; set; }
        public string PaymentClob { get; set; }

        public VpayPayment ToAvro()
        {
            return new VpayPayment
            {
                payer835ID = PayerID,
                batchID = BatchID,
                fileProcID = FileProcID,
                payerAlias = PayerAlias,
                provTIN = ProviderTIN,
                transactionType = TransactionType,
                payerSubpayerID = SubpayerID,
                zipCode = ZipCode,
                unconsolPaymentNb = UnconsolPaymentNumber,
                uconslPaymentAmount = UnconsolPaymentAmount.ToBytes(),
                blobStorageURI = BlobStorageURI,
                paymentClob = PaymentClob
            };
        }

        public static PaymentPayload Create(VpayPayment avro)
        {
            return new PaymentPayload
            {
                PayerID = avro.payer835ID,
                BatchID = avro.batchID,
                FileProcID = avro.fileProcID,
                PayerAlias = avro.payerAlias,
                ProviderTIN = avro.provTIN,
                TransactionType = avro.transactionType,
                SubpayerID = avro.payerSubpayerID,
                ZipCode = avro.zipCode,
                UnconsolPaymentNumber = avro.unconsolPaymentNb,
                UnconsolPaymentAmount = avro.uconslPaymentAmount.FromBytes(),
                BlobStorageURI = avro.blobStorageURI,
                PaymentClob = avro.paymentClob
            };
        }
    }
}
