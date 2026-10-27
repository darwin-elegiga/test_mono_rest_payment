#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands
{
    public static class UpdateOutbound835FileStatus
    {
        public record Command(
            long Id,
            Outbound835FileStatus Status,
            string? UpdatedBy = null,
            DateTime? UpdatedDateTime = null,
            DateTime? SentDateTime = null
        ) : IRequest<Models.Outbound835File?>;

        public class Handler : IRequestHandler<Command, Models.Outbound835File?>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Models.Outbound835File?> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.SaveFirstOrDefaultAsync<Models.Outbound835File>(
                    "[dbo].[usp_Outbound835File_UpdateStatus]", request, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
