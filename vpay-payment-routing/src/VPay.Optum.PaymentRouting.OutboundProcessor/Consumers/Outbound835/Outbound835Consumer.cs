using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.EdiFabric.Helpers;
using VPay.EdiFabric.Helpers.Models;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ConfigApi.Queries;
using VPay.Optum.Http.ReconReportsApi.Queries;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Helpers;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Commands;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Outbound835
{
    public class Outbound835Consumer : IConsumer<BuildOutbound835>
    {
        private const string DefaultRevEv = "411858498";
        private readonly IClobConverter _clobConverter;
        private readonly ILogger<Outbound835Consumer> _logger;
        private readonly IMediator _mediator;

        private readonly string _modifiedBy;

        public Outbound835Consumer(ILogger<Outbound835Consumer> logger, IMediator mediator,
            IClobConverter clobConverter, IUserInfo userInfo)
        {
            _logger = logger;
            _mediator = mediator;
            _clobConverter = clobConverter;
            _modifiedBy = userInfo.GetCurrentUserName();
        }

        /// <summary>
        /// Handles the incoming <see cref="BuildOutbound835"/> message
        /// </summary>
        /// <param name="inboundPayment">The inbound payment info containing ApMessageTrackingId and ClientCode</param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<BuildOutbound835> inboundPayment)
        {
            ArgumentHelpers.ThrowIf.IsNull(inboundPayment, nameof(inboundPayment));
            await ConsumePayment(inboundPayment.Message, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// This method uses the <see cref="BuildOutbound835"/> message to start building the outbound835 for payments that already have their 835s processed
        /// </summary>
        /// <remarks>
        /// If successful, updates the PaymentStatus to <see cref="PaymentStatus.Out835Built"/> and updates the ApMessageTrackingStatus to <see cref="ApMessageTrackingStatus.Completed"/>
        /// </remarks>
        /// <param name="payment">The inbound payment info containing ApMessageTrackingId and ClientCode</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns></returns>
        public async Task ConsumePayment(BuildOutbound835 payment, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNull(payment, nameof(payment));

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ApMessageTrackingId"] = payment.ApMessageTrackingId,
                ["ClientCode"] = payment.ClientCode
            });

            ApMessageTracking apMessageTracking = await GetApMessageTrackingFromDb(payment, cancellationToken).ConfigureAwait(false);

            if (apMessageTracking.Status == ApMessageTrackingStatus.Completed)
            {
                return;
            }

            GetInboundPaymentWithClobByIdSql.Result inboundPayment = await GetInboundPaymentFromDb(apMessageTracking, cancellationToken).ConfigureAwait(false);

            try
            {
                _logger.LogInformation("Building Outbound835 for payment {UserKey}", inboundPayment.UserKey);

                // Step 1: Update OutboundRevEo
                TS835 ts835 = await _clobConverter.ClobTo835(inboundPayment.PaymentClob);

                if (ts835 == null)
                {
                    throw new InvalidDataException($"Unable to decode Payment record [{apMessageTracking.InboundPaymentId}]");
                }

                string paymentRevEv =
                    string.IsNullOrWhiteSpace(ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02)
                        ? DefaultRevEv
                        : ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02;

                await _mediator.Send(new UpdateInboundPaymentRefEoSql.Command
                {
                    InboundPaymentId = apMessageTracking.InboundPaymentId,
                    OutboundRefEo = paymentRevEv,
                    ModifiedBy = _modifiedBy
                }, cancellationToken);

                // Step 2: Update 835
                ts835 = await Update835(ts835, apMessageTracking, inboundPayment.PayerSubpayerId, payment.ClientCode).ConfigureAwait(false);

                // Step 3: Save 835
                string outboundPaymentClob = await _clobConverter.Ts835ToClob(ts835);

                await _mediator.Send(new InsertOutboundPaymentClobSql.Command
                {
                    InboundPaymentId = apMessageTracking.InboundPaymentId,
                    Clob = outboundPaymentClob
                }, cancellationToken);

                // Update payment status to OUT835BUILT
                await _mediator.Send(new UpdateInboundPaymentStatusSql.Command
                {
                    InboundPaymentId = apMessageTracking.InboundPaymentId,
                    Status = PaymentStatus.Out835Built,
                    ModifiedBy = _modifiedBy
                }, cancellationToken);

                // Update ApMessageTracking status to completed
                await _mediator.Send(new UpdateApMessageTrackingStatus.Command
                {
                    ApMessageTrackingId = payment.ApMessageTrackingId,
                    Status = ApMessageTrackingStatus.Completed,
                    ModifiedBy = _modifiedBy
                }, cancellationToken);

                _logger.LogInformation("Completed Outbound835 for payment {UserKey}", inboundPayment.UserKey);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new("ERROR occurred in Outbound835 Consumer. ");
                sb.AppendLine($"Unable to perform operation (for payment {inboundPayment.UserKey}). ");
                sb.AppendLine("Check Rabbit and Azure queues, as those may have messages. ");
                sb.AppendLine("");

                _logger.LogError(ex, sb.ToString() + "{ErrorDescription}: {ErrorMessage} ({UserKey})", ex.GetType().ToString(), ex.Message, inboundPayment.UserKey);

                throw; // Re-throw so that MassTransit can do whatever it needs
            }
        }

        /// <summary>
        /// Gets the ApMessageTracking record based on the payment, validates it's in
        /// proper status to be processed, <see cref="ApMessageTrackingStatus.ApSent"/>
        /// or <see cref="ApMessageTrackingStatus.Started"/> and
        /// sets to <see cref="ApMessageTrackingStatus.Started"/>
        /// </summary>
        /// <param name="payment">The outbound payment info.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns>The ApMessageTracking record for the given ApMessageTrackingId. </returns>

        private async Task<ApMessageTracking> GetApMessageTrackingFromDb(BuildOutbound835 payment, CancellationToken cancellationToken)
        {
            // JAH (02/22/2022)- Figure out if there's a way, with mediator, to enlist a transaction to prevent a possible race condition. Will add too much scope at this point in project.
            ApMessageTracking apMessageTracking =
                await _mediator.Send(
                    new GetApMessageTrackingByIdSql.Query { Id = payment.ApMessageTrackingId }, cancellationToken).ConfigureAwait(false);

            if (apMessageTracking == null)
            {
                throw new KeyNotFoundException($"Unable to find ApMessageTracking with Id: [{payment.ApMessageTrackingId}]");
            }
            // Should these be smart enums like payment status?  Look at Jacobs stuff
            if (apMessageTracking.Status == ApMessageTrackingStatus.Completed)
            {
                _logger.LogWarning("Skipped completed Outbound835 request (ApMessageTrackingId: {ApMessageTrackingId})", payment.ApMessageTrackingId);

                return apMessageTracking;
            }
            if (apMessageTracking.Status != ApMessageTrackingStatus.ApSent && apMessageTracking.Status != ApMessageTrackingStatus.Started)
            {
                StringBuilder sb = new("ApMessageTracking is not in correct status for outbound processing. ");
                sb.AppendLine($"The status is expected to be APSENT or START835BUILD (ApStatus: {apMessageTracking.Status}). ");
                sb.AppendLine($"The conflicted key value is (ApMessageTrackingId: {payment.ApMessageTrackingId} | InPaymentId: {apMessageTracking.InboundPaymentId}). ");

                throw new BadRequestException($"{sb}");
            }

            // Set status to START835BUILD
            await _mediator.Send(new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = payment.ApMessageTrackingId,
                Status = ApMessageTrackingStatus.Started,
                ModifiedBy = _modifiedBy
            }, cancellationToken);

            return apMessageTracking;
        }

        /// <summary>
        /// Gets the InboundPayment record based on the message.
        /// </summary>
        /// <param name="apMessageTracking">The message tracking info.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns>The InboundPayment record for the given InboundPaymentId.</returns>
        private async Task<GetInboundPaymentWithClobByIdSql.Result> GetInboundPaymentFromDb(ApMessageTracking apMessageTracking, CancellationToken cancellationToken)
        {
            var inboundPayment = await _mediator.Send(new GetInboundPaymentWithClobByIdSql.Query { InboundPaymentId = apMessageTracking.InboundPaymentId }, cancellationToken).ConfigureAwait(false);
            if (inboundPayment == null)
            {
                throw new KeyNotFoundException($"Unable to find InboundPayment with Id: [{apMessageTracking.InboundPaymentId}]");
            }

            return inboundPayment;
        }

        private async Task<TS835> Update835(TS835 ts835, ApMessageTracking apMessageTracking, string subpayerId, string clientCode)
        {
            RemoveEoRefs(ts835);

            PaymentInformation bprInfo = await GetPaymentInfoForTransaction(apMessageTracking);

            if (bprInfo.PaymentType != PaymentType.CheckToACH)
            {
                // Update BPR
                ts835.BPR_FinancialInformation = bprInfo.GenerateBPRFromPaymentInfo();

                // Update TRN
                ts835.TRN_ReassociationTraceNumber = bprInfo.GenerateTRN();
            }

            // Update Loop 1000A
            ts835.AllN1 ??= new All_N1_835();

            PayerInformation payerInfo = await GetPayerInfoForTransaction(clientCode, subpayerId).ConfigureAwait(false);
            ts835.AllN1.Loop1000A = payerInfo.GenerateLoop1000A();

            return ts835;
        }

        // Either pull TpaCode from Platform Configuration or Update APMessageTracking to have client code
        private async Task<PayerInformation> GetPayerInfoForTransaction(string clientCode, string subpayerId)
        {
            var payerInfo = await _mediator.Send(new OtbBillingEntityDetailQuery(clientCode, $"1{subpayerId}"));

            if (payerInfo == null || payerInfo.Fields == null || !payerInfo.Fields.Any())
            {
                throw new KeyNotFoundException($"Unable to find OTB Payer Information for ClientCode: [{clientCode}] and SubPayerId: [{subpayerId}]");
            }

            var infoMap = payerInfo.Fields.ToDictionary(x => x.FieldName, x => x.FieldValue);
            var adjustedZipCode = infoMap.GetValueOrDefault("PayerZipCode")?.Replace("-", string.Empty);
            var result = new PayerInformation
            {
                PayerName = infoMap.GetValueOrDefault("BillingEntityName"),
                PayerAddressLine1 = infoMap.GetValueOrDefault("PayerAddressLine1"),
                PayerAddressLine2 = infoMap.GetValueOrDefault("PayerAddressLine2 "),
                PayerCityName = infoMap.GetValueOrDefault("PayerCityName"),
                PayerStateCode = infoMap.GetValueOrDefault("PayerStateCode"),
                PayerZipCode = adjustedZipCode,
                REF_2U_02 = infoMap.GetValueOrDefault("PayerID")?.TrimStart('0'),  // Do not include leading zeros! Must match value from TRN04 without the leading zeros.
                //REF_EO_02 = $"1{subpayerId}",                                    // Do not include field in Outbound 835! See RemoveEoRefs() for more information. Must match the subPayerId preceded with a leading '1'.
                CX_02 = infoMap.GetValueOrDefault("PER_CX_02"),
                CX_04 = infoMap.GetValueOrDefault("PER_CX_03"),
                BL_02 = infoMap.GetValueOrDefault("PER_BL_02"),
                BL_03 = infoMap.GetValueOrDefault("PER_BL_03"),
                BL_04 = infoMap.GetValueOrDefault("PER_BL_04"),
                BL_05 = infoMap.GetValueOrDefault("PER_BL_05"),
                BL_06 = infoMap.GetValueOrDefault("PER_BL_06"),
                BL_07 = infoMap.GetValueOrDefault("PER_BL_07"),
                BL_08 = infoMap.GetValueOrDefault("PER_BL_08"),
            };

            var alreadyHasPhoneNbr = false;
            const string PhoneValue = "TE";
            const string FaxValue = "FX";
            const string EmailValue = "EM";

            // The values we are getting back are missing BL_03, 05, and 07 so try to determine which values to use for them
            if (string.IsNullOrWhiteSpace(result.BL_03) && !string.IsNullOrWhiteSpace(result.BL_04))
            {
                if (IsNumeric(result.BL_04))
                {
                    // TE is recommend 1st so using that.
                    result.BL_03 = PhoneValue;
                    alreadyHasPhoneNbr = true;
                }
                else
                {
                    result.BL_03 = EmailValue;
                }
            }

            if (string.IsNullOrWhiteSpace(result.BL_05) && !string.IsNullOrWhiteSpace(result.BL_06))
            {
                if (IsNumeric(result.BL_06))
                {
                    result.BL_05 = alreadyHasPhoneNbr ? FaxValue : PhoneValue;
                    alreadyHasPhoneNbr = true;
                }
                else
                {
                    result.BL_05 = EmailValue;
                }
            }

            if (string.IsNullOrWhiteSpace(result.BL_07) && !string.IsNullOrWhiteSpace(result.BL_08))
            {
                if (IsNumeric(result.BL_08))
                {
                    result.BL_07 = alreadyHasPhoneNbr ? FaxValue : PhoneValue;
                }
                else
                {
                    result.BL_07 = EmailValue;
                }
            }

            return result;
        }

        private static bool IsNumeric(string val)
        {
            return long.TryParse(val, out var _);
        }

        private static void RemoveEoRefs(TS835 ts835)
        {
            // Need to remove a field under the Loop1000A that is REF*EO. This field is only needed in incoming report. NOTE: Some 835's may not have this field configured.
            var allEoRefs = ts835.AllN1?.Loop1000A?.REF_AdditionalPayerIdentification?.Where(x =>
                x.ReferenceIdentificationQualifier_01.Equals("EO")).ToList();

            if (allEoRefs != null && allEoRefs.Any())
            {
                foreach (REF_AdditionalPayerIdentification allEoRef in allEoRefs)
                {
                    ts835.AllN1.Loop1000A.REF_AdditionalPayerIdentification.Remove(allEoRef);
                }
            }

            if (string.IsNullOrWhiteSpace(ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02))
            {
                ts835.AllREF ??= new All_REF_835_3();
                ts835.AllREF.REF_ReceiverIdentification ??= new REF_ReceiverIdentification();
                ts835.AllREF.REF_ReceiverIdentification.MemberGrouporPolicyNumber_02 = DefaultRevEv;
            }
        }

        private async Task<PaymentInformation> GetPaymentInfoForTransaction(ApMessageTracking apMessageTracking, CancellationToken token = default)
        {
            // First element is 'TRN00', then TRN01, TRN02, etc...
            // First 2 elements are hard coded, so we don't need those
            string[] trnSegments = apMessageTracking.TrnSegment.Split('*');
            //TODO: should we throw exception if TrnSegment does not contain all parts?

            var result = new PaymentInformation
            {
                PaymentAmount = apMessageTracking.LoadAmount,
                PaymentDate = apMessageTracking.LoadDate.AddDays(1).Date,
                PaymentType = PaymentTypeCodeHelper.StringToPaymentType(apMessageTracking.PaymentTypeCode),
                TRN02 = trnSegments[2],
                TRN03 = trnSegments[3],
                TRN04 = trnSegments[4]
            };

            if (result.PaymentType == PaymentType.ACH)
            {
                var paymentAccountInfo = await _mediator.Send(new TransactionBankAccountsQuery(apMessageTracking.TransactionId), token).ConfigureAwait(false);
                //TODO: should we throw exception if paymentAccountInfo returns null?

                if (paymentAccountInfo != null)
                {
                    result.PayersBankAccountNumber = paymentAccountInfo.FromAccountNumber;
                    result.PayersRoutingNumber = paymentAccountInfo.FromRouteNumber;
                    result.ReceiverBankAccountNumber = paymentAccountInfo.ToAccountNumber;
                    result.ReceiverRoutingNumber = paymentAccountInfo.ToRouteNumber;
                }
            }

            return result;
        }
    }
}
