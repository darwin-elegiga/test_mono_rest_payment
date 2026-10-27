using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ConfigApi.Queries;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Constants;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Commands
{
    public class AddPaymentRecord
    {
        public class Command : IRequest
        {
            public PaymentRecord PaymentRecord { get; set; } = new();
        }

        public class Handler : IRequestHandler<Command>
        {
            private const string PrefixUserkeyConfigurationName = "PREFIX_USERKEY";
            private const string ACTIVE = "ACTIVE";
            private const string ENABLED = "Y";

            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly ICheckBatchCompleteExecutor _executor;

            public Handler(ILogger<Handler> logger, IMediator mediator, ICheckBatchCompleteExecutor executor)
            {
                _logger = logger;
                _mediator = mediator;
                _executor = executor;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                BatchSummary batchSummary;

                if (request.PaymentRecord.TransactionType == TransactionTypes.Eof)
                {
                    _logger.LogInformation("Processing payment EOF request from batch {OptumBatchId} for payment {UserKey}",
                        request.PaymentRecord.BatchId,
                        request.PaymentRecord.UnconsolidatedPaymentNbr);

                    var existingInboundHeader =
                        await _mediator.Send(new GetInboundHeaderByOptumBatchIdSql.Query { OptumBatchId = request.PaymentRecord.BatchId }, cancellationToken);

                    if (existingInboundHeader != null)
                    {
                        if (existingInboundHeader.HeaderData != request.PaymentRecord.PaymentClob)
                        {
                            throw new VPayAlertableException(new VPayAlert(
                                "Optum PRS",
                                "Payment header mismatch found",
                                $"Header found with matching batchId:{request.PaymentRecord.BatchId}, but different CLOB data",
                                $"batchId: {request.PaymentRecord.BatchId}\n" +
                                $"payerAlias: {request.PaymentRecord.PayerAlias}\n" +
                                "clob data not included due to HIPPA"
                                ));
                        }

                        _logger.LogInformation("Duplicate payment header found, skipping");
                        return Unit.Value;
                    }

                    batchSummary = await _mediator.Send(new InsertInboundHeaderSql.Command(request.PaymentRecord.BatchId,
                        request.PaymentRecord.Payer835Id, request.PaymentRecord.PayerSubpayerId,
                        request.PaymentRecord.FileProcId, request.PaymentRecord.PayerAlias,
                        request.PaymentRecord.PaymentClob), cancellationToken);
                }
                else
                {
                    await HandlePrefixUserKeyOverride(request.PaymentRecord, cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Processing payment PAY request from batch {OptumBatchId} for payment {UserKey}",
                        request.PaymentRecord.BatchId,
                        request.PaymentRecord.UnconsolidatedPaymentNbr);

                    var existingPaymentRecord =
                        await _mediator.Send(new GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query(
                            request.PaymentRecord.UnconsolidatedPaymentNbr, request.PaymentRecord.Payer835Id), cancellationToken);

                    if (existingPaymentRecord != null)
                    {
                        if (existingPaymentRecord.PaymentClob != request.PaymentRecord.PaymentClob
                            || existingPaymentRecord.OptumBatchId != request.PaymentRecord.BatchId)
                        {
                            throw new VPayAlertableException(new VPayAlert(
                                "Optum PRS",
                                "Payment record already exists and cannot be processed again",
                                $"Payment record in batchId: {request.PaymentRecord.BatchId} already exists",
                                    $"original batchId: {existingPaymentRecord.OptumBatchId}\n" +
                                    $"batchId: {request.PaymentRecord.BatchId}\n" +
                                    $"unconsolidatedPaymentNbr: {request.PaymentRecord.UnconsolidatedPaymentNbr}\n" +
                                    $"unconsolidatedPaymentAmount: {request.PaymentRecord.UnconsolidatedPaymentAmount}\n" +
                                    $"payerAlias: {request.PaymentRecord.PayerAlias}\n" +
                                    $"payerSubpayerId: {request.PaymentRecord.PayerSubpayerId}\n" +
                                    $"remitMethod: {request.PaymentRecord.RemitMethod}\n" +
                                    "clob data not included due to HIPPA"
                            ));
                        }

                        _logger.LogInformation("Duplicate payment record found, skipping");
                        return Unit.Value;
                    }

                    await UpdateRemitMethod(request.PaymentRecord, cancellationToken).ConfigureAwait(false);

                    batchSummary = await _mediator.Send(new InsertInboundPaymentSql.Command(request.PaymentRecord.BatchId,
                        request.PaymentRecord.Payer835Id, request.PaymentRecord.PayerSubpayerId,
                        request.PaymentRecord.FileProcId, request.PaymentRecord.PayerAlias,
                        request.PaymentRecord.ProviderTin, request.PaymentRecord.ZipCode,
                        request.PaymentRecord.UnconsolidatedPaymentNbr,
                        Convert.ToDecimal(request.PaymentRecord.UnconsolidatedPaymentAmount),
                        request.PaymentRecord.PaymentClob, request.PaymentRecord.RemitMethod), cancellationToken);
                }

                await _executor.Execute(batchSummary, cancellationToken);

                return Unit.Value;
            }

            /// <summary>
            /// Look up remit method from PlatformConfig.  This is important because a remit method of "DATA" will allow
            /// the payment to move on without waiting for the pdf in RemittanceAdviceFile
            /// </summary>
            /// <param name="paymentRecord"></param>
            /// <param name="cancellationToken"></param>
            private async Task UpdateRemitMethod(PaymentRecord paymentRecord, CancellationToken cancellationToken)
            {
                try
                {
                    var payerAliasMapping = await _mediator.Send(new GetPrsPayerAliasMapping.Query(paymentRecord.PayerAlias), cancellationToken).ConfigureAwait(false);

                    var billingEntityCode = await GetBillingEntity(paymentRecord.PayerSubpayerId, cancellationToken);

                    var praConfig = await _mediator.Send(new GetPayerPraConfigurationQuery.Query(payerAliasMapping.OriginCode, payerAliasMapping.ClientCode, billingEntityCode), cancellationToken).ConfigureAwait(false);

                    paymentRecord.RemitMethod = praConfig.RemitMethod;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred attempting to update the remit method {PayerAlias} {OptumBatchId} {UserKey}. " +
                        "This could result in generation of PDF EOB rather than using the supplied PDF PRA. " +
                        "Make sure connection to platform config is functioning and that the PayerAlias is set up in PlatformConfig." +
                        "See Product Documentation: KB-PRS-001",
                        paymentRecord.BatchId,
                        paymentRecord.UnconsolidatedPaymentNbr,
                        paymentRecord.PayerAlias
                        );
                    throw;
                }
            }

            /// <summary>
            /// Looks up the BE code by PayerSubPayerId in platform config
            /// </summary>
            /// <param name="payerSubpayerId"></param>
            /// <param name="cancellationToken"></param>
            /// <returns>BillingEntityCode</returns>
            private async Task<string> GetBillingEntity(string? payerSubpayerId, CancellationToken cancellationToken)
            {
                try
                {
                    var mapping = await _mediator.Send(new GetPrsSubPayerIdMapping.Query(payerSubpayerId), cancellationToken).ConfigureAwait(false);
                    return mapping.BillingEntityCode;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "An error occurred attempting to retrieve the BillingEntityCode from the following SubPayerId: {SubPayerId}", payerSubpayerId);
                    return string.Empty;
                }
            }

            /// <summary>
            /// Look up PREFIX_USERKEY configuration from PlatformConfig.
            /// This is important because clients that want to use the same User Key (i.e. check number)
            /// across multiple sub-payers will have to have their sub-payer ID prefixed
            /// to the User Key to ensure uniqueness.
            /// Example: {SubPayerID}_{UserKey}
            /// </summary>
            /// <param name="paymentRecord">The inbound payment record.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            private async Task HandlePrefixUserKeyOverride(PaymentRecord paymentRecord, CancellationToken cancellationToken)
            {
                var payerAliasMapping = await _mediator.Send(new GetPrsPayerAliasMapping.Query(paymentRecord.PayerAlias), cancellationToken).ConfigureAwait(false);

                var configuration = await _mediator.Send(new GetClientBoardingConfiguration.Query(PrefixUserkeyConfigurationName, payerAliasMapping.OriginCode, payerAliasMapping.ClientCode), cancellationToken).ConfigureAwait(false);

                // Configuration is active and enabled
                if (ACTIVE.Equals(configuration.Status, StringComparison.InvariantCultureIgnoreCase) &&
                    ENABLED.Equals(configuration.Data1, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Concatenate SubPayerID_UserKey
                    if (paymentRecord.UnconsolidatedPaymentNbr != null && paymentRecord.PayerSubpayerId != null)
                    {
                        // If the UserKey does not already start with the PayerSubPayerId, prepend it.
                        // This check is necessary to avoid duplicating the prefix when re-processing payments after a failure.
                        // This ensures that the UserKey is unique across sub-payers for payers configured to use this feature.
                        if (!paymentRecord.UnconsolidatedPaymentNbr.StartsWith(paymentRecord.PayerSubpayerId + "_", StringComparison.InvariantCultureIgnoreCase))
                        {
                            paymentRecord.UnconsolidatedPaymentNbr = string.Concat(paymentRecord.PayerSubpayerId, "_", paymentRecord.UnconsolidatedPaymentNbr);
                        }
                    }
                }
            }
        }
    }
}
