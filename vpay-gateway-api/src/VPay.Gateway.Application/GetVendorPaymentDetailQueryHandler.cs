using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vpay.Gateway.Common.Vpay;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;

namespace VPay.Gateway.Application;

public class GetVendorPaymentDetailQueryHandler : IRequestHandler<GetVendorPaymentDetailQuery, TransactionSearchResponseVendor>
{
    private readonly IMediator _mediator;
    private readonly IVPayApi _vpayApi;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<GetVendorPaymentDetailQueryHandler> _logger;

    public GetVendorPaymentDetailQueryHandler(IMediator mediator, IVPayApi vpayApi, IUserInfo userInfo, ILogger<GetVendorPaymentDetailQueryHandler> logger)
    {
        _mediator = mediator;
        _vpayApi = vpayApi;
        _userInfo = userInfo;
        _logger = logger;
    }

    public async Task<TransactionSearchResponseVendor> Handle(GetVendorPaymentDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Invoke three endpoints in parallel
            var transactionDetailTask = _vpayApi.GetTransactionDetailsByTransactionID(request.TransactionId, true, cancellationToken);
            var transactionChainTask = _vpayApi.GetTransactionChain(request.TransactionId, cancellationToken);
            var transactionCorrespondanceTask = _vpayApi.GetTranCorrespondanceDetails(request.TransactionId, true, cancellationToken);


            // Wait for all tasks to complete
            await Task.WhenAll(transactionDetailTask, transactionChainTask, transactionCorrespondanceTask).ConfigureAwait(false);

            // Retrieve results
            var transactionDetail = await transactionDetailTask.ConfigureAwait(false);
            var transactionChain = await transactionChainTask.ConfigureAwait(false);
            var transactionCorrespondanceList = await transactionCorrespondanceTask.ConfigureAwait(false);
            transactionChain = transactionChain?
                         .Where(x => !string.Equals(x.TransactionId, request.TransactionId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                         .ToList();
            var response = new TransactionSearchResponseVendor
            {
                vpayTransactionID = request.TransactionId,
                GroupNumber = transactionDetail?.GroupNumber,
                Bank = transactionDetail?.BankName,
                BillingEntityCode = transactionDetail?.BillingEntity?.Code,
                ClientCode = transactionDetail?.BillingEntity?.Tpa?.Code,
                AvailableBalance = transactionDetail.AvailableBalance,
                CurrentBalance = transactionDetail.CurrentBalance,
                PaymentModality = MapTransactionTypeToModality(transactionDetail.TransactionType ?? string.Empty),
                AchSettlementDate = MapTransactionTypeToDate(transactionDetail.TransactionType ?? string.Empty, transactionDetail.Events, "PURCHASE"),
                CheckClearDate = MapTransactionTypeToDate(transactionDetail.TransactionType ?? string.Empty, transactionDetail.Events, "PURCHASE", isCheck: true),
                AchInitiatedDate = MapTransactionTypeToDate(transactionDetail.TransactionType ?? string.Empty, transactionDetail.Events, "LOADED"),
                CheckCreatedDate = transactionDetail.TransactionType == TransactionTypeEnum.CLCHK.ToString()
                    ? transactionDetail.LoadDate
                    : null,
                CheckNumber = transactionDetail.PosPayCheckNumber,
                Amount = transactionDetail.Amount,
                TransactionChainResponseInternal = transactionChain, // Populate the internal property
                ActionList = transactionDetail.Events.Select(e => new TransactionEvent
                {
                    Status = e.Status,
                    RequesterName = e.RequesterName,
                    RequesterId = e.RequesterId,
                    AuthCode = e.AuthCode,
                    AccountingType = e.AccountingType,
                    Amount = e.Amount,
                    ShippingClass = e.ShippingClass,
                    TrackingNumber = e.TrackingNumber,
                    Timestamp = e.Timestamp,
                    MerchantInfo = e.MerchantInfo,
                    ReasonCode = e.ReasonCode,
                    ReasonDescription = e.ReasonDescription,
                    MerchantCode = e.MerchantCode,
                    BatchId = e.BatchId
                })
                .OrderByDescending(e => e.Timestamp)
                .ToList(),
                Correspondence = transactionCorrespondanceList.FirstOrDefault()

            };
            string preTransactionId = "";

            if (!string.IsNullOrEmpty(request.PretransactionId) && request.PretransactionId != "0")
            {
                if (request.PretransactionId.Length > 0 && request.PretransactionId.StartsWith("Z", StringComparison.OrdinalIgnoreCase))
                {
                    preTransactionId = request.PretransactionId;
                }
                else
                {
                    preTransactionId = string.Concat("Z", request.PretransactionId);
                }
            }
            else if (transactionChain != null && transactionChain.Count != 0 && string.IsNullOrEmpty(preTransactionId))
            {
                var transactionIds = transactionChain
                                    .Select(transaction => transaction.TransactionId)
                                    .Where(id => !string.IsNullOrEmpty(id) && long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                                    .ToList();

                foreach (var transactionId in transactionIds)
                {
                    if (long.TryParse(transactionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var transactionIdLong))
                    {
                        GetPreTransIdbyTransactionQuery queryPreTransaction = new(transactionIdLong);
                        preTransactionId = await _mediator.Send(queryPreTransaction, cancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(preTransactionId))
                        {
                            preTransactionId = preTransactionId.StartsWith("Z", StringComparison.OrdinalIgnoreCase)
                                 ? preTransactionId
                                 : string.Concat("Z", preTransactionId);
                            break;
                        }
                    }
                }
            }
            response.PreTransID = preTransactionId;
            return response;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Transaction not found for request: {Request}", JsonConvert.SerializeObject(request));
            throw new ItemNotFoundException("Transaction not found for the given request.", ex);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogError(ex, "API error occurred while processing the request: {Request}", JsonConvert.SerializeObject(request));
            throw new BadRequestException("An error occurred while retrieving transaction details from the API.", ex);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogError(ex, "API error occurred while processing the request: {Request}", JsonConvert.SerializeObject(request));
            throw new ConflictException("An error occurred while retrieving transaction details from the API.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request: {Request}", JsonConvert.SerializeObject(request));
            throw;
        }
    }

    private static string MapTransactionTypeToModality(string transactionType)
    {
        return Enum.TryParse<TransactionTypeEnum>(transactionType, true, out var typeEnum) switch
        {
            true when typeEnum == TransactionTypeEnum.CLEFT || typeEnum == TransactionTypeEnum.CLSDA => TransactionTypeEnum.ACH.ToString(),
            true when typeEnum == TransactionTypeEnum.CLCHK => TransactionTypeEnum.CHECK.ToString(),
            _ => transactionType
        };
    }

    private static DateTime? MapTransactionTypeToDate(string transactionType, ICollection<TransactionEvent> actionList, string status, bool isCheck = false)
    {
        if (Enum.TryParse<TransactionTypeEnum>(transactionType, true, out var typeEnum))
        {
            if ((typeEnum == TransactionTypeEnum.CLEFT || typeEnum == TransactionTypeEnum.CLSDA) && !isCheck)
            {
                return actionList.FirstOrDefault(e => e.Status == status)?.Timestamp;
            }
            if (typeEnum == TransactionTypeEnum.CLCHK && isCheck)
            {
                return actionList.FirstOrDefault(e => e.Status == status)?.Timestamp;
            }
            else
            {
                return null;
            }
        }
        return null;
    }
}
