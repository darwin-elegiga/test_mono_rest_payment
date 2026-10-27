using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands
{
    public class UpdateApMessage
    {
        public class Command : IRequest<Unit>
        {
            public long Id { get; set; }
            public string ClientCode { get; set; } = string.Empty;
            public string BillingEntityCode { get; set; } = string.Empty;
            public string ApMessageType { get; set; } = string.Empty;
            public string ApRoutingType { get; set; } = string.Empty;
            public bool IsOptumEnrolled { get; set; }

            // Avro Fields
            public string PayerId { get; set; } = string.Empty;
            public string SubPayerId { get; set; } = string.Empty;
            public string PaymentSource { get; set; } = string.Empty;
            public string PaymentType { get; set; } = string.Empty;
            public string ProviderNumber { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public string ProviderNpiNumber { get; set; } = string.Empty;
            public string UnConsolPayNumber { get; set; } = string.Empty;
            public string UnConsolPayAmount { get; set; } = string.Empty;
            public string DisplayConsolPayNumber { get; set; } = string.Empty;
            public string ConsolAmount { get; set; } = string.Empty;
            public string PayMethodCode { get; set; } = string.Empty;
            public string SettlementDate { get; set; } = string.Empty;
            public string CheckNumber { get; set; } = string.Empty;
            public string ReturnReasonCode { get; set; } = string.Empty;
            public string RevisedMethodCode { get; set; } = string.Empty;
            public string ReOriginatedDate { get; set; } = string.Empty;

            // Auditing fields
            public string ModifiedBy { get; set; } = string.Empty;
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync(
                    "dbo.usp_ApMessage_Update",
                    new
                    {
                        request.Id,
                        request.ClientCode,
                        request.BillingEntityCode,
                        request.ApMessageType,
                        request.ApRoutingType,
                        request.IsOptumEnrolled,
                        request.PayerId,
                        request.SubPayerId,
                        request.PaymentSource,
                        request.PaymentType,
                        request.ProviderNumber,
                        request.ProviderName,
                        request.ProviderNpiNumber,
                        request.UnConsolPayNumber,
                        request.UnConsolPayAmount,
                        request.DisplayConsolPayNumber,
                        request.ConsolAmount,
                        request.PayMethodCode,
                        request.SettlementDate,
                        request.CheckNumber,
                        request.ReturnReasonCode,
                        request.RevisedMethodCode,
                        request.ReOriginatedDate,
                        request.ModifiedBy
                    },
                    cancellationToken
                ).ConfigureAwait(false);

                return Unit.Value;
            }
        }
    }
}
