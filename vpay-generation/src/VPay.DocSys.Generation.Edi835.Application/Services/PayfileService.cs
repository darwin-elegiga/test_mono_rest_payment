using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Data.Db2.Abstractions.NonFundedPayment;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application.Models;
using VPay.DocSys.Generation.Edi835.Application.PayFile;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public class PayfileService : IPayfileService
    {
        private readonly INonFundedPaymentDetailRepository _repository;
        private readonly IReconReportsApi _api;
        private readonly ILoafApi _loafApi;
        private readonly ILogger<PayfileService> _logger;

        private const char _fieldDelimiter = '|';
        private const char _lineTerminator = '\n';

        public PayfileService(INonFundedPaymentDetailRepository repository, IReconReportsApi api, ILoafApi loafApi, ILogger<PayfileService> logger)
        {
            _repository = repository;
            _api = api;
            _loafApi = loafApi;
            _logger = logger;
        }

        public async Task<string> GeneratePayFileDocument(DS.Edi835 message, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            string paymentTypeCode = GetPaymentTypeCode(message.PaymentTypeCode);

            var payDetail = new PayfileDetail()
            {
                TransactionId = message.TransactionId.ToString(),
                OriginCode = message.OriginCode,
                ClientCode = message.ClientCode,
                BillingEntityCode = message.BillingEntityCode,
                ClaimId = message.ClaimId,
                CheckNumber = "",
                CheckDate = message.ClaimDate.ToString("yyyyMMdd"),
                UserKey = message.UserKey,
                PaymentTypeCode = paymentTypeCode,
                Amount = (message.PaymentAmount * 100).ToString("F2").PadLeft(20, '0'), /* Legacy has been off all along */
                Currency = "",
                SesPayDate = message.LoadDate.ToString("yyyyMMdd"),
                PortUser = "PORTUSER",
                TaxIdSuffix = "",
                NPPID = "",
                Name1 = message.PayeeName,
                Name2 = "",
                Address1 = "",
                Address2 = "",
                Address3 = "",
                Address4 = "",
                City = "",
                State = "",
                PostalCode = "",
                Country = "",
                EftFromRoute = "",
                EftFromAccount = "",
                EftToAccount = "",
                EftToRoute = "",
            };

            var detail = await _repository.GetPayfileInfoById(message.NonFundedPaymentDetailId, new CancellationToken()).ConfigureAwait(false);

            if (detail != null)
            {
                payDetail.CheckNumber = detail.UserDisplay3;
                payDetail.TaxId = detail.TaxId;
                payDetail.Address1 = detail.PayeeAddress1;
                payDetail.Address2 = detail.PayeeAddress2;
                payDetail.Address3 = detail.PayeeAddress3;
                payDetail.City = detail.PayeeCity;
                payDetail.State = detail.PayeeState;
                payDetail.PostalCode = detail.PayeeZipCode;
            }

            // if the original payment type on the message is OPACH, we don't want to override the EFT information
            if (!message.PaymentTypeCode.Equals(PayType.OPACH, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(payDetail.PaymentTypeCode, PayType.CLEFT) || string.Equals(payDetail.PaymentTypeCode, PayType.CLSDA))
                {
                    var mainAccount = await _api.GetMain835Account(cancellationToken).ConfigureAwait(false); //should potentially cache this for a while. It doesn't change that often
                    var transactionAccount = await _api.GetBankAccountForTransactionId(message.TransactionId, cancellationToken).ConfigureAwait(false);

                    if (mainAccount != null)
                    {
                        payDetail.EftFromAccount = mainAccount.AccountNumber;
                        payDetail.EftFromRoute = mainAccount.RouteNumber;
                    }

                    if (transactionAccount != null)
                    {
                        payDetail.EftToAccount = transactionAccount.AccountNumber;
                        payDetail.EftToRoute = transactionAccount.RouteNumber;
                    }
                }
            }
            else
            {
                LoafTransactionBankInfoDto optumBankInfo = await _loafApi.GetOptumBankInfo(message.TransactionId.ToString(), new CancellationToken()).ConfigureAwait(false);
                if (optumBankInfo == null)
                {
                    _logger.LogInformation("No Optum information found in LOAF for {TransactionId}", message.TransactionId);
                    throw new OpachBankingDetailsMissingException($"Cannot generate payfile.  No Optum information found in LOAF for Transaction '{message.TransactionId}'", message.TransactionId);
                }

                payDetail.PaymentTypeCode = PayType.CLEFT;

                // if the routing and account numbers don't have a value, switch to "check"
                if (string.IsNullOrWhiteSpace(optumBankInfo.FromRoutingNumber)
                    || string.IsNullOrWhiteSpace(optumBankInfo.ToRoutingNumber)
                    || string.IsNullOrWhiteSpace(optumBankInfo.FromAccountNumber)
                    || string.IsNullOrWhiteSpace(optumBankInfo.ToAccountNumber))
                {
                    payDetail.PaymentTypeCode = PayType.CLCHK;
                }
                else
                {
                    payDetail.EftFromRoute = optumBankInfo.FromRoutingNumber ?? "";
                    payDetail.EftFromAccount = optumBankInfo.FromAccountNumber ?? "";
                    payDetail.EftToAccount = optumBankInfo.ToAccountNumber ?? "";
                    payDetail.EftToRoute = optumBankInfo.ToRoutingNumber ?? "";
                }
            }

            return BuildPayFileAsString(payDetail);
        }

        private static string BuildPayFileAsString(PayfileDetail detail)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(FormatZeroPaddedString(detail.TransactionId, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.OriginCode, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.ClientCode, 10)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.BillingEntityCode, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.ClaimId, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.CheckNumber, 10)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.CheckDate, 8)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.UserKey, 50)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.PaymentTypeCode, 5)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Amount, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Currency, 3)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.SesPayDate, 8)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.PortUser, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.TaxId, 20)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.TaxIdSuffix, 3)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.NPPID, 10)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Name1, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Name2, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Address1, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Address2, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Address3, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Address4, 80)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.City, 30)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.State, 30)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.PostalCode, 30)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.Country, 50)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.EftFromRoute, 9)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.EftFromAccount, 17)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.EftToRoute, 9)).Append(_fieldDelimiter);
            builder.Append(FormatFixedString(detail.EftToAccount, 17)).Append(_fieldDelimiter);
            builder.Append(_lineTerminator);

            return builder.ToString();
        }

        public static string FormatFixedString(string value, int fixedLength)
        {
            if (value == null)
            {
                return new string(' ', fixedLength);
            }

            if (value.Length > fixedLength)
            {
                return value.Substring(0, fixedLength);
            }

            return value.PadRight(fixedLength, ' ');
        }

        public static string FormatZeroPaddedString(string value, int fixedLength)
        {
            if (value == null)
            {
                return new string('0', fixedLength);
            }

            if (value.Length > fixedLength)
            {
                return value.Substring(0, fixedLength);
            }

            return value.PadLeft(fixedLength, '0');
        }

        /// <summary>
        /// Translate the PayType, since SDS does not recognize all of the PayTypes we use internally.
        /// </summary>
        private string GetPaymentTypeCode(string paymentTypeCode)
        {
            switch (paymentTypeCode.ToUpper())
            {
                case PayType.CLEFT:
                case PayType.OPACH:
                case PayType.CLSDA:
                    return PayType.CLEFT;

                default:
                    return paymentTypeCode.ToUpper();
            }
        }
    }
}
