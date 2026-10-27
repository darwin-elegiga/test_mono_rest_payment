using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Ardalis.Result;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.PreferencingApi.Models;
using VPay.Optum.Http.PreferencingApi.Queries;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Constants;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public static class ConsumeApMessage
    {
        public record Command(ApReapMessage Message) : IRequest<VpayApReap>;

        public class Handler : IRequestHandler<Command, VpayApReap>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IApReapVPayToOptumConverter _converter;
            private readonly IOptumBus _bus;
            private readonly IPublishEndpoint _publishEndpoint;
            private readonly IMediator _mediator;
            private readonly OptumQueueNameSettings _settings;
            private readonly string _modifiedBy;

            public Handler(
                ILogger<Handler> logger,
                IApReapVPayToOptumConverter converter,
                IOptumBus bus,
                IPublishEndpoint publishEndpoint,
                IMediator mediator,
                IUserInfo userInfo,
                OptumQueueNameSettings settings)
            {
                _logger = logger;
                _converter = converter;
                _bus = bus;
                _publishEndpoint = publishEndpoint;
                _mediator = mediator;
                _modifiedBy = userInfo.GetCurrentUserName();
                _settings = settings;
            }

            public async Task<VpayApReap> Handle(Command request, CancellationToken cancellationToken)
            {
                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["UserKey"] = request.Message.UserKey,
                    ["ClientCode"] = request.Message.TpaCode,
                    ["PaymentType"] = request.Message.PaymentType,
                    ["MessageType"] = request.Message.MessageType,
                    ["TransactionId"] = request.Message.TransactionId,
                    ["ReapTransactionId"] = request.Message.ReapTransactionId
                });
                _logger.LogInformation("Starting AP message consume {OriginalMessage}", JsonConvert.SerializeObject(request.Message));

                // Convert the iSeries message into an Optum message.
                VpayApReap convertedMessage = await _converter.ConvertApReapMessage(request.Message);

                // Get inbound payment batch information.
                var inbound835 = await GetInbound835Payment(request.Message, convertedMessage, cancellationToken).ConfigureAwait(false);

                // Make sure the original casing for the payment number is preserved.
                // or if the subpayerId is pre-pended, then strip that out
                convertedMessage.unConsolPayNumber = StripSubPayerIdIfExists(inbound835.UserKey, convertedMessage.payerSubpayerID);

                _logger.LogInformation("Converted Message: {ConvertedMessage}", JsonConvert.SerializeObject(convertedMessage));

                // Replace the AP values with the REAP values when message is REAP.
                ApReapMessage message = GetWorkingMessage(request.Message);

                // Create new AP Message Tracking Id for new message, or get existing AP Message Tracking Id for interrupted message.
                var apMessageTracking = await GetApMessageTrackingId(message, inbound835.InboundPaymentId, cancellationToken).ConfigureAwait(false);

                // Process new message or interrupted message not yet processed by another consumer.
                if (apMessageTracking.IsSuccess)
                {
                    await ProcessApMessage(apMessageTracking.Value, inbound835.InboundPaymentId, message, convertedMessage, cancellationToken).ConfigureAwait(false);
                }

                return convertedMessage;
            }

            /// <summary>
            /// Gets the "existing" ApMessageTrackingId (would only exist if error was thrown), or
            /// Inserts a new record in ApMessageTracking database table.
            /// </summary>
            /// <param name="message">The original/working message coming from the iSeries.</param>
            /// <param name="inboundPaymentId">The inbound payment record id.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns>
            /// When the record is new, a Success object with the row ID of the ApMessageTracking table.
            /// When the record is existing and the status is Queued, a Success object with the row ID of the ApMessageTracking table.
            /// When the record is existing and the status is Completed, an Error object with the duplicate record description.
            /// When the record is existing and the status is not Queued or Completed, throws exception.
            /// </returns>
            internal async Task<Result<long>> GetApMessageTrackingId(
                ApReapMessage message,
                long inboundPaymentId,
                CancellationToken cancellationToken)
            {
                long transactionId = (long)message.TransactionId;
                // See if the TransactionId already appears in db.
                var existingApMessageTracking = await _mediator.Send(new GetApMessageTrackingByApMessageId.Query { ApMessageId = transactionId }, cancellationToken).ConfigureAwait(false);
                // If it does AND the status is currently in QUEUED, this is the "new" record, we just somehow broke the chain before actually updating anything
                if (existingApMessageTracking != null)
                {
                    // Continue interrupted processing: The status is still showing as Queued; the message has not completed processing.
                    if (existingApMessageTracking.Status == ApMessageTrackingStatus.Queued)
                    {
                        _logger.LogInformation("Found ApMessageTracking for payment {UserKey}", message.UserKey);
                        return Result<long>.Success(existingApMessageTracking.ApMessageTrackingId);
                    }
                    //// Skip processing: The status is no longer Queued; the message has already been processed by another consumer.
                }

                // Need to make sure the existing transaction is committed.
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                // Insert ApMessageTracking new record.
                var command = new InsertApMessageTracking.Command
                {
                    ApMessageId = transactionId,
                    TransactionId = transactionId,
                    Status = ApMessageTrackingStatus.Queued,
                    InboundPaymentId = inboundPaymentId,
                    TrnSegment = message.TrnSegment,
                    LoadAmount = message.LoadAmount,
                    CreatedBy = _modifiedBy,
                    CheckNumber = message.CheckNumber,
                    LoadDate = message.LoadDate,
                    PaymentTypeCode = message.PaymentType,
                };
                var newApMessageTracking = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

                transaction.Complete();

                // Notify success: New message can start processing.
                if (newApMessageTracking.IsSuccess)
                {
                    _logger.LogInformation("Inserted ApMessageTracking for payment {UserKey}", message.UserKey);
                }
                // Stop processing: The status is intermediate; the message may be processed by another consumer or the database is in inconsistent state.
                else
                {
                    StringBuilder sb = new("ApMessageTracking is not in correct status for AP processing. ");
                    sb.AppendLine("Violation of UNIQUE KEY constraint 'UNQ_ApMessageTracking_ApMessageId'. ");
                    sb.AppendLine("Cannot insert duplicate key in object 'dbo.ApMessageTracking'. ");
                    sb.AppendLine($"The duplicate key value is (ApMessageId: {transactionId} | UserKey: {message.UserKey}). ");

                    throw new ConflictException($"{sb}");
                }

                return newApMessageTracking;
            }

            /// <summary>
            /// When the incoming message is just AP, we use all the "regular" fields, but
            /// when it's a reissue, we do very hard to follow logic (at least SonarQube
            /// complains about the cognitive complexity).
            /// Once determined if the payment is Original or Reissue, update appropriate
            /// fields with the "working" values.
            /// </summary>
            /// <param name="message">The original message received from the iSeries.</param>
            /// <returns>The AP message with the original values, or the REAP message
            /// with the working values instead of the original values.</returns>
            internal ApReapMessage GetWorkingMessage(ApReapMessage message)
            {
                if (message.MessageType == MessageTypes.Reap)
                {
                    return message with
                    {
                        TransactionId = message.ReapTransactionId ?? -1,
                        CheckNumber = message.ReapCheckNumber,
                        LoadDate = message.ReapLoadDate.GetValueOrDefault(DateTime.Today),
                        PaymentType = message.ReapPaymentType
                    };
                }
                return message;
            }

            /// <summary>
            /// Process the AP/REAP message and follows distinct payment routing according to payment type.
            /// There are three distinct routing types:
            ///   1) OPACH:  For Optum ACH payments, send to Optum so they can build the 835 file (sent via PRF File Builder).
            ///              Set AP message tracking status to COMPLETED and Inbound payment status to APSENTSKIP.
            ///   2) FINSEG: For CLCFT payments or enrolled CLNPF (no pays), build the FINSEG 835 and FINSEG TXT files.
            ///              Set AP message tracking status to FinSegNeeded and Inbound payment status to FinSegQueued.
            ///   3) AZURE:  For all other payments, send AP Info to Optum via Azure Service Bus and build the Consolidated 835 file.
            ///              Set AP message tracking status to APSENT and Inbound payment status to APSENT.
            /// The process needs to update both the AP message tracking status and the Inbound payment status accordingly,
            /// after sending the Azure message to Optum, if applicable, and before sending the message to build the 835 files.
            /// There are three more consumers involved to complete the end-to-end process for AP messages:
            ///   1) ApReapTrackingConsumer: Updates the transaction Status in the ApMessageTracking table.
            ///   2) ApReapPaymentConsumer : Updates the payment Status in the InboundPayment table.
            ///   3) ApReapRoutingConsumer : Queues the message to build the 835 files, if applicable.
            /// These three consumers are daisy-chained in the above order to complete processing.
            /// </summary>
            /// <param name="apMessageTrackingId">The tracking Id of the message.</param>
            /// <param name="inboundPaymentId">The payment Id of the message.</param>
            /// <param name="message">The working message (for AP/REAP).</param>
            /// <param name="convertedMessage">The converted message as expected by Optum.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns></returns>
            internal async Task ProcessApMessage(
                long apMessageTrackingId,
                long inboundPaymentId,
                ApReapMessage message,
                VpayApReap convertedMessage,
                CancellationToken cancellationToken)
            {
                var tracking = new ApReapTracking
                {
                    ClientCode = message.TpaCode,
                    BillingEntityCode = message.BillingEntity,
                    ApMessageType = message.MessageType,
                    InboundPaymentId = inboundPaymentId,
                    ApMessageTrackingId = apMessageTrackingId
                };

                try
                {
                    // Verify if the payment belongs to a provider enrolled with Optum.
                    bool isOptumEnrolledCLNPF = await IsOptumEnrolledNoPay(message, cancellationToken).ConfigureAwait(false);
                    tracking.IsOptumEnrolled = isOptumEnrolledCLNPF;

                    if (message.PaymentType == PaymentTypes.Opach)
                    {
                        // Set AP message track status to COMPLETED
                        // Set Inbound payment status to APSENTSKIP

                        tracking.ApRoutingType = RoutingTypes.Opach;
                        tracking.ApMessageTrackingStatus = ApMessageTrackingStatus.Completed.Name;
                        tracking.InboundPaymentStatus = PaymentStatus.ApSentSkip.Name;

                        // Queue to update the AP message tracking status, then the inbound payment status, and finally build the 835 files (via PRF File Builder).
                        await _publishEndpoint.Publish(tracking, cancellationToken).ConfigureAwait(false);
                    }
                    //If payment is CLCFT or enrolled CLNPF
                    else if (message.PaymentType == PaymentTypes.Clcft || isOptumEnrolledCLNPF)
                    {
                        // Set AP message track status to FinSegNeeded 
                        // Set Inbound payment status to FinSegQueued

                        tracking.ApRoutingType = RoutingTypes.FinSeg;
                        tracking.ApMessageTrackingStatus = ApMessageTrackingStatus.FinSegNeeded.Name;
                        tracking.InboundPaymentStatus = PaymentStatus.FinSegQueued.Name;

                        // Queue to update the AP message tracking status, then the inbound payment status, and finally build the FINSEG 835 files (via Outbound FinSeg 835 processor).
                        await _publishEndpoint.Publish(tracking, cancellationToken).ConfigureAwait(false);
                    }
                    //If payment is neither OPACH nor CLCFT nor enrolled CLNPF
                    else
                    {
                        // This is the touch-point with Optum for "AP Info". 
                        await SendApInfoToOptumViaAzureBus(message, convertedMessage, cancellationToken).ConfigureAwait(false);
                        if (message.MessageType == MessageTypes.Ap)
                        {
                            // Set AP message track status to APSENT
                            // Set Inbound payment status to APSENT
                            tracking.ApRoutingType = RoutingTypes.Azure;
                            tracking.ApMessageTrackingStatus = ApMessageTrackingStatus.ApSent.Name;
                            tracking.InboundPaymentStatus = PaymentStatus.ApSent.Name;

                            // Queue to update the AP message tracking status, then the inbound payment status, and finally build the Consolidated 835 files (via Outbound 835 processor).
                            await _publishEndpoint.Publish(tracking, cancellationToken).ConfigureAwait(false);
                        }
                        else  // message.MessageType == MessageTypes.Reap
                        {
                            // Set AP message track status to COMPLETED
                            tracking.ApRoutingType = RoutingTypes.Azure;
                            tracking.ApMessageTrackingStatus = ApMessageTrackingStatus.Completed.Name;

                            // No need to update the payment status or to create 835 files.
                            await UpdateTrackingStatus(apMessageTrackingId, ApMessageTrackingStatus.Completed, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    _logger.LogInformation("Routing is done for payment ({UserKey}) - TPA: {ClientCode} | BE: {BillingEntityCode} | PayType: {PaymentType} | AP/REAP: {MessageType} | RouteType: {RoutingType} | Enrolled:{EnrollmentType}",
                        message.UserKey, message.TpaCode, message.BillingEntity, message.PaymentType, message.MessageType, tracking.ApRoutingType, isOptumEnrolledCLNPF);

                    ApReapOutboundAzure outboundAzure = CreateApMessageForStorage(message, convertedMessage, tracking);
                    await _publishEndpoint.Publish(outboundAzure, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERROR occurred in ConsumeApMessage {ErrorDescription}: {ErrorMessage} ({UserKey}) - TPA: {ClientCode} | BE: {BillingEntityCode} | PayType: {PaymentType} | AP/REAP: {MessageType} | RouteType: {RoutingType}",
                        ex.GetType().ToString(), ex.Message, message.UserKey, message.TpaCode, message.BillingEntity, message.PaymentType, message.MessageType, tracking.ApRoutingType);

                    throw;
                }
            }

            #region Deprecated ProcessApMessage
            /*
            private async Task ProcessApMessage(
                long apMessageTrackingId,
                long inboundPaymentId,
                ApReapMessage message,
                VpayApReap convertedMessage,
                CancellationToken cancellationToken)
            {
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        // I'm placing this outside the conditionals, since it will not evaluate unless it needs to
                        // and it would be more confusing to have an awaited call in "simplified" logic
                        bool isOptumEnrolledCLNPF = await IsOptumEnrolledNoPay(message, cancellationToken).ConfigureAwait(false);

                        if (message.PaymentType == PaymentTypes.Opach)
                        {
                            //set AP message status to COMPLETED
                            await UpdateTrackingStatus(apMessageTrackingId, ApMessageTrackingStatus.Completed, cancellationToken).ConfigureAwait(false);
                            //Set Inbound payment to APSENTSKIP
                            await UpdatePaymentStatus(inboundPaymentId, PaymentStatus.ApSentSkip, cancellationToken).ConfigureAwait(false);
                        }
                        //If payment is CLCFT or enrolled CLNPF
                        else if (message.PaymentType == PaymentTypes.Clcft || isOptumEnrolledCLNPF)
                        {
                            //set AP message status to FinSegNeeded, indicating that it should go to the BuildOutboundFinSeg Queue 
                            await UpdateTrackingStatus(apMessageTrackingId, ApMessageTrackingStatus.FinSegNeeded, cancellationToken).ConfigureAwait(false);
                            //Set Inbound payment to FINSEGQUEUED
                            await UpdatePaymentStatus(inboundPaymentId, PaymentStatus.FinSegQueued, cancellationToken).ConfigureAwait(false);
                            // Queue to generate FinSeg
                            await _publishEndpoint.Publish(new BuildOutboundFinSeg(apMessageTrackingId, message.TpaCode), cancellationToken).ConfigureAwait(false);
                        }
                        //If payment is neither OPACH nor CLCFT nor enrolled CLNPF
                        else
                        {
                            // This is the touch-point with Optum for "AP Info". 
                            await SendApInfoToOptumViaAzureBus(message, convertedMessage, cancellationToken).ConfigureAwait(false);
                            if (message.MessageType == MessageTypes.Ap)
                            {
                                await UpdateTrackingStatus(apMessageTrackingId, ApMessageTrackingStatus.ApSent, cancellationToken).ConfigureAwait(false);
                                //Set Inbound payment to APSENT
                                await UpdatePaymentStatus(inboundPaymentId, PaymentStatus.ApSent, cancellationToken).ConfigureAwait(false);
                                //Send message to build 835
                                await _publishEndpoint.Publish(new BuildOutbound835(apMessageTrackingId, message.TpaCode), cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await UpdateTrackingStatus(apMessageTrackingId, ApMessageTrackingStatus.Completed, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        transaction.Complete();

                        _logger.LogInformation("Routing is done for payment {UserKey} ({PaymentType})", message.UserKey, message.PaymentType);
                    }
                    catch (Exception ex)
                    {
                        StringBuilder sb = new("ERROR occurred in ConsumeApMessage within transaction context. ");
                        sb.AppendLine($"Rolling back transaction for payment {message.UserKey}. ");
                        sb.AppendLine("Check Rabbit and Azure queues, as those may have messages. ");
                        sb.AppendLine("");

                        _logger.LogError(ex, sb.ToString() + "{ErrorDescription}: {ErrorMessage} ({UserKey})", ex.GetType().ToString(), ex.Message, message.UserKey);
                        // Exiting the transaction's scope without .Complete() will roll it back
                        throw; // Re-throw so that MassTransit can do whatever it needs
                    }
                }
            }              
            */
            #endregion

            /// <summary>
            /// Creates the AP/REAP outbound message to store in the database in case it needs to be resent to Optum via Azure Service Bus.
            /// </summary>
            /// <param name="message">The original AP/REAP message coming from the iSeries.</param>
            /// <param name="convertedMessage">The converted AP/REAP message that can be sent to Optum via Azure Service Bus.</param>
            /// <param name="tracking">The AP/REAP tracking records.</param>
            /// <returns></returns>
            private static ApReapOutboundAzure CreateApMessageForStorage(ApReapMessage message, VpayApReap convertedMessage, ApReapTracking tracking)
            {
                long transactionId = Convert.ToInt64(message.MessageType == MessageTypes.Reap ? message.ReapTransactionId : message.TransactionId);
                var outboundAzure = new ApReapOutboundAzure
                {
                    TransactionId = transactionId,
                    ClientCode = tracking.ClientCode,
                    BillingEntityCode = tracking.BillingEntityCode,
                    ApMessageType = tracking.ApMessageType,
                    ApRoutingType = tracking.ApRoutingType,
                    IsOptumEnrolled = tracking.IsOptumEnrolled,
                    // Avro Fields
                    PayerId = convertedMessage.payr835ID,
                    SubPayerId = convertedMessage.payerSubpayerID,
                    PaymentSource = convertedMessage.paymentSource,
                    PaymentType = convertedMessage.paymentType,
                    ProviderNumber = convertedMessage.providerNumber,
                    ProviderName = convertedMessage.providerName,
                    ProviderNpiNumber = convertedMessage.provNPINumber,
                    UnConsolPayNumber = convertedMessage.unConsolPayNumber,
                    UnConsolPayAmount = convertedMessage.unConsolPayAmount,
                    DisplayConsolPayNumber = convertedMessage.displConsolPayNumber,
                    ConsolAmount = convertedMessage.consolAmount,
                    PayMethodCode = convertedMessage.payMethodCode,
                    SettlementDate = convertedMessage.settlementDate,
                    CheckNumber = convertedMessage.checkNumber,
                    ReturnReasonCode = convertedMessage.returnReasonCode,
                    RevisedMethodCode = convertedMessage.revisedMethodCode,
                    ReOriginatedDate = convertedMessage.reoriginatedDate
                };
                return outboundAzure;
            }

            /// <summary>
            /// If the AP message is not CLNPF, will shortcut evaluate
            /// to false and immediately return with no further action.
            /// Otherwise will call the memory-cached (on both our local process and
            /// the remote API) "solve" to determine if the CLNPF should be included
            /// in the FINSEG files we send back to Optum.
            /// </summary>
            /// <param name="message">The working message (for AP/REAP).</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            internal async Task<bool> IsOptumEnrolledNoPay(ApReapMessage message, CancellationToken cancellationToken)
            {
                if (message.PaymentType != PaymentTypes.Clnpf)
                {
                    return false;
                }

                bool isEnrolled = (await _mediator.Send(
                    new OptumAchEnrollmentSearchQuery(
                        new OptumAchEnrollmentSearchParam(message.TpaCode, message.ProviderTin)
                    ), cancellationToken).ConfigureAwait(false)).IsEnrolled;

                return isEnrolled;
            }

            /// <summary>
            /// Get the Optum inbound payment information for the payment number coming from iSeries.
            /// </summary>
            /// <param name="userKey">The payment number coming from iSeries.</param>
            /// <param name="payer835Id">The client code as provided by Optum.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns></returns>
            internal async Task<GetInboundPaymentByUserKeyPayer835IdSql.Result> GetInbound835Payment(
                ApReapMessage message,
                VpayApReap convertedMessage,
                CancellationToken cancellationToken)
            {
                var inbound835 =
                    await _mediator.Send(
                        new GetInboundPaymentByUserKeyPayer835IdSql.Query(message.UserKey, convertedMessage.payr835ID), cancellationToken).ConfigureAwait(false);

                //If not found
                if (inbound835 == null)
                {
                    //message will be sent to the error queue (throw an exception/move it there manually)
                    throw new ItemNotFoundException($"Could not find inbound payment for payment {message.UserKey}");
                }

                return inbound835;
            }

            /// <summary>
            /// Sends the converted AP/REAP message to Optum via Azure.
            /// </summary>
            /// <param name="message">The working message (for AP/REAP).</param>
            /// <param name="convertedMessage">The converted message that complies with the format expected by Optum.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns></returns>
            internal async Task SendApInfoToOptumViaAzureBus(
                ApReapMessage message,
                VpayApReap convertedMessage,
                CancellationToken cancellationToken)
            {
                //send message to Optum
                //queue processed message on the IOptumServiceBus
                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{_settings.AccountPayable}")).ConfigureAwait(false);
                await endpoint.Send(convertedMessage, cancellationToken).ConfigureAwait(false);

                if (message.MessageType == MessageTypes.Ap)
                {
                    _logger.LogInformation("Sending AP message to Optum via Azure for payment {UserKey}", message.UserKey);
                }
                else
                {
                    _logger.LogInformation("Sending REAP message to Optum via Azure for payment {UserKey}", message.UserKey);
                }
            }

            /// <summary>
            /// Updates the message tracking status.
            /// </summary>
            /// <param name="apMessageTrackingId">The tracking Id of the message.</param>
            /// <param name="apMessageTrackingStatus">The new status for the message.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns></returns>
            internal async Task UpdateTrackingStatus(long apMessageTrackingId, ApMessageTrackingStatus apMessageTrackingStatus, CancellationToken cancellationToken)
            {
                await _mediator.Send(new UpdateApMessageTrackingStatus.Command
                {
                    ApMessageTrackingId = apMessageTrackingId,
                    Status = apMessageTrackingStatus,
                    ModifiedBy = _modifiedBy
                }, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// Updates the inbound payment status.
            /// </summary>
            /// <param name="inboundPaymentId">The payment Id of the payment.</param>
            /// <param name="status">The new status for the message.</param>
            /// <param name="cancellationToken">A token to cancel the work.</param>
            /// <returns></returns>
            internal async Task UpdatePaymentStatus(long inboundPaymentId, PaymentStatus status, CancellationToken cancellationToken)
            {
                await _mediator.Send(new UpdateInboundPaymentStatusSql.Command
                {
                    InboundPaymentId = inboundPaymentId,
                    Status = status,
                    ModifiedBy = _modifiedBy
                }, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// If the user key has the pre-pended SubPayerId, this method strips it out for the outbound message
            /// </summary>
            /// <remarks>
            /// the pre-pended sub-payer ID is internal to vpay only so optum wont be aware of that ID, which is why we convert it back to the check number here
            /// </remarks>
            /// <param name="userKey">The User Key</param>
            /// <param name="subPayerId">The Sub-Payer ID</param>
            /// <returns></returns>
            internal static string StripSubPayerIdIfExists(string userKey, string subPayerId)
            {
                if (userKey.StartsWith(subPayerId + "_"))
                {
                    return userKey.Replace(subPayerId + "_", "");
                }

                return userKey;
            }
        }
    }
}
