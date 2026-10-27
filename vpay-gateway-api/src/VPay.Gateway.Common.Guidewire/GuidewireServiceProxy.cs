using Polly;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;
using OptumAPIPortTypeClient = VPay.Gateway.Common.Guidewire.OptumAPI.OptumAPIPortTypeClient;

namespace VPay.Gateway.Common.Guidewire;

public class GuidewireServiceProxy : IGuidewireServiceProxy
{
    private readonly IOptumAPIPortTypeClientPolicyRegistry _policyRegistry;
    private readonly IOptumAPIPortTypeClientFactory _apiClientFactory;
    private readonly ILogger<GuidewireServiceProxy> _logger;

    private readonly locale _locale = new() { Value = "en_US" };

    public GuidewireServiceProxy(
        IOptumAPIPortTypeClientPolicyRegistry policyRegistry,
        IOptumAPIPortTypeClientFactory apiClientFactory,
        ILogger<GuidewireServiceProxy> logger)
    {
        _policyRegistry = policyRegistry;
        _apiClientFactory = apiClientFactory;
        _logger = logger;
    }

    public async Task<GuidewireResult> UpdatePaymentDetails(OptumPaymentUpdateDetails details, GuidewireSettings settings) =>
        await Execute(
            settings,
            CreateContext(nameof(OptumAPIPortTypeClient.updatePaymentDetailsAsync)),
            async client =>
            {
                var result = await client.updatePaymentDetailsAsync(CreateAuth(settings), _locale, details).ConfigureAwait(false);
                return result.@return;
            }).ConfigureAwait(false);

    public async Task<GuidewireResult> NotifyDocumentDetails(OptumNotifyDocumentDetails details, GuidewireSettings settings) =>
        await Execute(
            settings,
            CreateContext(nameof(OptumAPIPortTypeClient.notifyDocumentDetailsAsync)),
            async client =>
            {
                var result = await client.notifyDocumentDetailsAsync(CreateAuth(settings), _locale, details).ConfigureAwait(false);
                return result.@return;
            }).ConfigureAwait(false);

    public async Task<GuidewireResult> UpdateEnrollmentDetails(OptumEnrollmentUpdateDetails details, GuidewireSettings settings) =>
        await Execute(
            settings,
            CreateContext(nameof(OptumAPIPortTypeClient.updateEnrollmentDetailsAsync)),
            async client =>
            {
                var result = await client.updateEnrollmentDetailsAsync(CreateAuth(settings), _locale, details).ConfigureAwait(false);
                return result.@return;
            }).ConfigureAwait(false);

    public async Task<GuidewireResult> CreateNote(NoteDetails details, GuidewireSettings settings) =>
        await Execute(
            settings,
            CreateContext(nameof(OptumAPIPortTypeClient.createNoteAsync)),
            async client =>
            {
                var result = await client.createNoteAsync(CreateAuth(settings), _locale, details).ConfigureAwait(false);
                return result.@return;
            }).ConfigureAwait(false);

    private async Task<GuidewireResult> Execute(
        GuidewireSettings settings,
        Context context,
        Func<OptumAPIPortTypeClient, Task<OptumPaymentDetailsResponse>> action)
    {
        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = context.OperationKey
        });

        OptumAPIPortTypeClient client = null;
        try
        {
            _logger.Log(LogLevel.Information, "Guidewire call to URL {Url} from user {UserName}", settings.Url, settings.UserName);

            client = _apiClientFactory.CreateClient(settings.Url);

            _logger.Log(LogLevel.Information, "Guidewire server proxy created endpoint {Endpoint} with state {State}.", client.Endpoint.Name, client.State);

            await client.OpenAsync().ConfigureAwait(false);

            _logger.Log(LogLevel.Information, "Guidewire server proxy opened endpoint {Endpoint} with state {State} and policy {PolicyHolder}.", client.Endpoint.Name, client.State, _policyRegistry.GetExecutionRetryPolicy().PolicyKey);

            var result = await _policyRegistry.GetExecutionRetryPolicy().ExecuteAsync(
                async _ => await action(client).ConfigureAwait(false),
                context
            ).ConfigureAwait(false);

            var logLevel = result.Status == OptumStatus.FAILED ? LogLevel.Error : LogLevel.Information;
            _logger.Log(logLevel, "Guidewire result for status update is: {Result} - {ResultMessage}", result.Status, result.Message);

            return new GuidewireResult
            {
                Status = result.Status.ToString(),
                Message = result.Message
            };
        }
        finally
        {
            if (client != null)
            {
                await client.CloseOrAbortServiceChannel().ConfigureAwait(false);
            }
        }
    }

    private static Context CreateContext(string apiMethod) =>
        new Context($"{nameof(OptumAPIPortTypeClient)}.{apiMethod}");

    private static authentication CreateAuth(GuidewireSettings settings) =>
        new authentication
        {
            username = settings.UserName,
            password = settings.Password
        };
}
