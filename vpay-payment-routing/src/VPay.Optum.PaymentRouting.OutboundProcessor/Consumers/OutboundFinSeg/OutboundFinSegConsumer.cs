using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Commands;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSeg
{
    public class OutboundFinSegConsumer : IConsumer<BuildOutboundFinSeg>
    {
        //private const string DefaultRevEv = "411858498";
        private const string DefaultRevEv = ""; // Using blank to insert "empty string" in db rather than NULL
        private readonly IClobConverter _clobConverter;
        private readonly ILogger<OutboundFinSegConsumer> _logger;
        private readonly IMediator _mediator;
 
        private readonly string _modifiedBy;

        public OutboundFinSegConsumer(ILogger<OutboundFinSegConsumer> logger, IMediator mediator,
            IClobConverter clobConverter, IUserInfo userInfo)
        {
            _logger = logger;
            _mediator = mediator;
            _clobConverter = clobConverter;
            _modifiedBy = userInfo.GetCurrentUserName();
        }

        /// <summary>
        /// Handles the incoming <see cref="BuildOutboundFinSeg"/> message
        /// </summary>
        /// <param name="inboundPayment">The inbound payment info containing ApMessageTrackingId and ClientCode</param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<BuildOutboundFinSeg> inboundPayment)
        {
            ArgumentHelpers.ThrowIf.IsNull(inboundPayment, nameof(inboundPayment));
            await ConsumePayment(inboundPayment.Message, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// This method uses the <see cref="BuildOutboundFinSeg"/> message to start building the outbound 835 for payments that
        /// will not have the EFT handled by VPay.  
        /// </summary>
        /// <remarks>
        /// If successful, updates the PaymentStatus to <see cref="PaymentStatus.FinSegGenerated"/>
        /// and updates the ApMessageTrackingStatus to <see cref="ApMessageTrackingStatus.Completed"/>
        /// </remarks>
        /// <param name="payment">The inbound payment info containing ApMessageTrackingId and ClientCode</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns></returns>
        public async Task ConsumePayment(BuildOutboundFinSeg payment, CancellationToken cancellationToken)
        {
            // JAH - 02/23/2022 - determine the feasibility of wrapping this in one or more transactions.
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
                _logger.LogInformation("Building OutboundFinSeg for payment {UserKey}", inboundPayment.UserKey);

                // Step 1: Retrieve OutboundRevEo to update the InboundPayment.OutboundRefEo field
                TS835 ts835 = await _clobConverter.ClobTo835(inboundPayment.PaymentClob);

                if (ts835 == null)
                {
                    // If we don't have a valid 835, we want to throw an error, since it won't be good to generate a file without it
                    throw new InvalidDataException($"Unable to decode Payment record [{apMessageTracking.InboundPaymentId}]");
                }

                string paymentRevEv =
                    string.IsNullOrWhiteSpace(ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02)
                        ? DefaultRevEv
                        : ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02;

                await _mediator.Send(
                    new UpdateInboundPaymentRefEoSql.Command
                    {
                        InboundPaymentId = apMessageTracking.InboundPaymentId,
                        OutboundRefEo = paymentRevEv,
                        ModifiedBy = _modifiedBy
                    }, cancellationToken).ConfigureAwait(false);

                // Step 2: Save unmodified 835 to the OutboundPaymentClob table to be used as the FinSeg/835 
                await _mediator.Send(new InsertOutboundPaymentClobSql.Command
                {
                    InboundPaymentId = apMessageTracking.InboundPaymentId,
                    Clob = inboundPayment.PaymentClob
                }, cancellationToken).ConfigureAwait(false);

                // Update payment status to FinSegBuilt
                await _mediator.Send(new UpdateInboundPaymentStatusSql.Command
                {
                    InboundPaymentId = apMessageTracking.InboundPaymentId,
                    Status = PaymentStatus.FinSegBuilt,
                    ModifiedBy = _modifiedBy
                }, cancellationToken).ConfigureAwait(false);

                // Update ApMessageTracking status to completed (Same ending for both paths)
                await _mediator.Send(new UpdateApMessageTrackingStatus.Command
                {
                    ApMessageTrackingId = payment.ApMessageTrackingId,
                    Status = ApMessageTrackingStatus.Completed,
                    ModifiedBy = _modifiedBy
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Completed OutboundFinSeg for payment {UserKey}", inboundPayment.UserKey);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new("ERROR occurred in OutboundFinSeg Consumer. ");
                sb.AppendLine($"Unable to perform operation (for payment {inboundPayment.UserKey}). ");
                sb.AppendLine("Check Rabbit queue, as it may have messages. ");
                sb.AppendLine("");

                _logger.LogError(ex, sb.ToString() + "{ErrorDescription}: {ErrorMessage} ({UserKey})", ex.GetType().ToString(), ex.Message, inboundPayment.UserKey);
                
                throw; // Re-throw so that MassTransit can do whatever it needs
            }
        }

        /// <summary>
        /// Gets the ApMessageTracking record based on the payment, validates it's in
        /// proper status to be processed, <see cref="ApMessageTrackingStatus.FinSegNeeded"/> and
        /// sets to <see cref="ApMessageTrackingStatus.StartFinSegBuild"/>
        /// </summary>
        /// <param name="payment">The outbound payment info.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns>The ApMessageTracking record for the given ApMessageTrackingId. </returns>

        private async Task<ApMessageTracking> GetApMessageTrackingFromDb(BuildOutboundFinSeg payment, CancellationToken cancellationToken)
        {
            ApMessageTracking apMessageTracking = await _mediator.Send(
                new GetApMessageTrackingByIdSql.Query { Id = payment.ApMessageTrackingId }, cancellationToken).ConfigureAwait(false);
            if (apMessageTracking == null)
            {
                throw new KeyNotFoundException($"Unable to find ApMessageTracking with Id: [{payment.ApMessageTrackingId}]");
            }

            if (apMessageTracking.Status == ApMessageTrackingStatus.Completed)
            {
                _logger.LogWarning("Skipped completed OutboundFinSeg request (ApMessageTrackingId: {ApMessageTrackingId})", payment.ApMessageTrackingId);

                return apMessageTracking;
            }
            if (apMessageTracking.Status != ApMessageTrackingStatus.FinSegNeeded && apMessageTracking.Status != ApMessageTrackingStatus.StartFinSegBuild)
            {
                StringBuilder sb = new("ApMessageTracking is not in correct status for outbound processing. ");
                sb.AppendLine($"The status is expected to be FinSegNeeded (ApStatus: {apMessageTracking.Status}). ");
                sb.AppendLine($"The conflicted key value is (ApMessageTrackingId: {payment.ApMessageTrackingId} | InPaymentId: {apMessageTracking.InboundPaymentId}). ");

                throw new BadRequestException($"{sb}");
            }

            // Update ApMessageTracking status to StartFinSegBuild
            await _mediator.Send(new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = payment.ApMessageTrackingId,
                Status = ApMessageTrackingStatus.StartFinSegBuild,
                ModifiedBy = _modifiedBy
            }, cancellationToken).ConfigureAwait(false);

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
    }
}
