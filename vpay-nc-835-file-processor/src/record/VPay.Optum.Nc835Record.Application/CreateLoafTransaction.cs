using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Refit;
using VPay.Optum.Http.DocSysGeneration;
using VPay.Optum.Http.DocSysGeneration.Models;
using VPay.Optum.Http.Loaf;
using VPay.Optum.Http.Loaf.Models;

namespace VPay.Optum.Nc835Record.Application
{
    public static class CreateLoafTransaction
    {
        public record Command(TransactionBankInfoUpsert BankInfo) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly ILoafApi _api;
            private readonly IDocSysGenerationApi _docSysGenerationApi;
            private readonly ILogger<Handler> _logger;

            public Handler(
                ILoafApi api,
                IDocSysGenerationApi docSysGenerationApi,
                ILogger<Handler> logger)
            {
                _api = api;
                _docSysGenerationApi = docSysGenerationApi;
                _logger = logger;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                bool processed = false;

                var details = await _api.Upsert(request.BankInfo, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(details.ReferenceId))
                {
                    processed = true;
                }

                await PublishTransaction(request, cancellationToken).ConfigureAwait(false);

                return processed;
            }

            private async Task PublishTransaction(Command request, CancellationToken cancellationToken)
            {
                try
                {
                    var transactionId = long.Parse(request.BankInfo.ReferenceId);
                    var result = await _docSysGenerationApi.PublishTransactions(
                            new Edi835TransactionsDto
                            {
                                TransactionIds = new List<long>(1)
                                {
                                    transactionId
                                }
                            }, cancellationToken)
                        .ConfigureAwait(false);

                    if (result.TryGetValue(transactionId, out string? resultDescription) && !string.IsNullOrWhiteSpace(resultDescription))
                    {
                        _logger.LogInformation("Published transaction {ReferenceId} with result {Description}", transactionId, resultDescription);
                    }
                    else
                    {
                        _logger.LogWarning("Published transaction {ReferenceId} with empty result", transactionId);
                    }
                }
                catch (ApiException apiEx)
                {
                    _logger.LogError(apiEx, "API Exception encountered while publishing transactions for {ReferenceId}", request.BankInfo.ReferenceId);
                    throw;
                }
                catch (FormatException)
                {
                    _logger.LogError("Could not convert {ReferenceId} to number", request.BankInfo.ReferenceId);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception encountered while publishing transactions for {ReferenceId}", request.BankInfo.ReferenceId);
                    throw;
                }
            }
        }
    }
}
