using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using VPay.Gateway.Common.MemberClaims.Models;

namespace VPay.Gateway.Common.MemberClaims;

public static class ReceivePaymentDecision
{
    public record Command(VendorPaymentDecision PaymentInfo) : IRequest<IApiResponse<VendorPaymentDecisionResponse>>;

    public class CommandHandler : IRequestHandler<Command, IApiResponse<VendorPaymentDecisionResponse>>
    {
        private readonly IMemberClaimsApi _memberClaimsApi;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(IMemberClaimsApi memberClaimsApi, ILogger<CommandHandler> logger)
        {
            _memberClaimsApi = memberClaimsApi ?? throw new ArgumentNullException(nameof(memberClaimsApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IApiResponse<VendorPaymentDecisionResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.PaymentInfo);

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ClientCode"] = request.PaymentInfo.PayerCode,
                ["BillingEntityCode"] = request.PaymentInfo.SubpayerCode,
                ["PreTransactionId"] = request.PaymentInfo.PreTransId,
                ["Action"] = "ReceivePaymentInfo"
            }))
            {
                try
                {
                    // Call the Member Claims API to send the payment decision info received from the vendor partner.
                    var response = await _memberClaimsApi
                    .ReceivePaymentInfo(request.PaymentInfo, cancellationToken)
                    .ConfigureAwait(false);

                    VendorPaymentDecisionResponse newContent = new();
                    var httpResponse = new HttpResponseMessage(response.StatusCode)
                    {
                        RequestMessage = response.RequestMessage
                    };
                    var newResponse = new ApiResponse<VendorPaymentDecisionResponse>(httpResponse, newContent, new RefitSettings());

                    if (response.Content == null && string.IsNullOrEmpty(response.Error?.Content))
                    {
                        // Handle case where both content and error are null/empty
                        _logger.LogInformation("Member Claims API Receive Payment Info responded {StatusCode} for PreTransaction {PreTransactionId}: {Response}", newResponse.StatusCode, request.PaymentInfo.PreTransId, "No content or error content");
                    }
                    else if (response.Content != null)
                    {
                        try
                        {
                            newContent.Status = ExtractValueFromDynamicProperty(response.Content, "Status");
                            newContent.Message = ExtractValueFromDynamicProperty(response.Content, "Message");
                            newContent.TraceId = ExtractValueFromDynamicProperty(response.Content, "TraceId");
                            newContent.Type = ExtractValueFromDynamicProperty(response.Content, "Type");
                            newContent.Title = ExtractValueFromDynamicProperty(response.Content, "Title");
                            newContent.Detail = ExtractValueFromDynamicProperty(response.Content, "Detail");
                            newContent.Instance = ExtractValueFromDynamicProperty(response.Content, "Instance");
                            newContent.Errors = ExtractValueFromDynamicProperty(response.Content, "Errors");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error extracting properties from response content for PreTransaction {PreTransactionId}", request.PaymentInfo.PreTransId);
                        }

                        _logger.LogInformation("Member Claims API Receive Payment Info responded {StatusCode} for PreTransaction {PreTransactionId}: {Response}", newResponse.StatusCode, request.PaymentInfo.PreTransId, newContent?.ToString());
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        if (response.Error?.Content != null)
                        {
                            var errorData = JsonConvert.DeserializeObject<ValidationProblemDetails>(response.Error.Content);
                            newContent.Status = errorData?.Status ?? 0;
                            newContent.Type = errorData?.Type ?? string.Empty;
                            newContent.Title = errorData?.Title ?? string.Empty;
                            newContent.Detail = errorData?.Detail ?? string.Empty;
                            newContent.Instance = errorData?.Instance ?? string.Empty;
                            newContent.Errors = errorData?.Errors ?? new Dictionary<string, string[]>();

                            _logger.LogInformation("Member Claims API Receive Payment Info responded {StatusCode} for PreTransaction {PreTransactionId}: {Response}", newResponse.StatusCode, request.PaymentInfo.PreTransId, response.Error.Content);
                        }
                    }
                    else
                    {
                        if (response.Error?.Content != null)
                        {
                            var errorData = JsonConvert.DeserializeObject<Microsoft.AspNetCore.Mvc.ProblemDetails>(response.Error.Content);
                            newContent.Status = errorData?.Status ?? 0;
                            newContent.Type = errorData?.Type ?? string.Empty;
                            newContent.Title = errorData?.Title ?? string.Empty;
                            newContent.Detail = errorData?.Detail ?? string.Empty;
                            newContent.Instance = errorData?.Instance ?? string.Empty;

                            _logger.LogInformation("Member Claims API Receive Payment Info responded {StatusCode} for PreTransaction {PreTransactionId}: {Response}", newResponse.StatusCode, request.PaymentInfo.PreTransId, response.Error.Content);
                        }
                    }

                    return newResponse;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERROR Member Claims API Receive Payment Info responded failed for PreTransaction {PreTransactionId}", request.PaymentInfo.PreTransId);
                    throw;
                }
            }
        }

        private static object? ExtractValueFromDynamicProperty(dynamic dynamicObject, string propertyName)
        {
            try
            {
                var property = dynamicObject.GetType().GetProperty(propertyName);
                return property?.GetValue(dynamicObject, null); // Returns null if property doesn't exist
            }
            catch (Exception)
            {
                // Property does not exist in the dynamic object, return null
                return null;
            }
        }
    }
}
