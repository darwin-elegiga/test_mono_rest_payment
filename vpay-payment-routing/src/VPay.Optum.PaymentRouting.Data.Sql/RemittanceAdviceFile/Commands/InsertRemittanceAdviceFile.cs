using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
namespace VPay.Optum.PaymentRouting.Data.Sql.RemittanceAdviceFile.Commands
{

    [ExcludeFromCodeCoverage]
    /// <summary>
    /// Inserts RemittanceAdviceFile for PDFs when extracting Zip files
    /// </summary>
    public class InsertRemittanceAdviceFile
    {
        public record Response(long RemittanceAdviceFileId, long? InboundBatchId) ;

        public record Command(
            string Payer835Id,
            string InboundZipfile,
            string UserKey,
            string PayerAlias,
            string OriginCode,
            string ClientCode,
            string BillingEntityCode,
            string RemitMethod,
            string PdfExtractPath,
            string CreatedBy) : IRequest<Result<Response>>;

        public class Handler : IRequestHandler<Command, Result<Response>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                try
                {
                    var response = await _connection.SaveFirstOrDefaultAsync<Response>(
                        "dbo.usp_RemittanceAdviceFile_Insert",
                        request,
                        cancellationToken
                    );
                    return Result<Response>.Success(response);
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number is SqlErrorCode.UNIQUE_INDEX_VIOLATION or SqlErrorCode.DUPLICATE_KEY_VIOLATION)
                    {
                        // Allow consumer to ACK duplicate records. Duplication is acceptable.
                        return Result<Response>.Error(sqlEx.Message);
                    }

                    // Re-throw to any other SqlException (any Non-SqlException will simply throw at the caller level)
                    throw;
                }
            }
        }
    }
}
