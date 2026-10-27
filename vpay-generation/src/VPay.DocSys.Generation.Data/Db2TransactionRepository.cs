using System;
using System.Data;
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public class Db2TransactionRepository : IDb2TransactionRepository
    {
        private readonly IDataConnection<OdbcConnection> _connection;
        private readonly ILogger _logger;

        public Db2TransactionRepository(IDataConnection<OdbcConnection> connection, ILogger<Db2TransactionRepository> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PaymentTransaction> GetPayerInfoByTransactionIdAsync(long transactionId, CancellationToken cancellationToken = default)
        {
            var connection = await _connection.GetOpenConnectionAsync(cancellationToken);
            const string Query = @"SELECT
     TMTXID
    ,TMCLIC
    ,TMBILC
    ,TMUSKY
FROM
    SEWCPS.CPSTMF
WHERE
    TMTXID = ?";

            using (var cmd = new OdbcCommand(Query, connection))
            {
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@TMTXID", transactionId);

                var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                PaymentTransaction result = null;

                if (await reader.ReadAsync(cancellationToken))
                {
                    result = new PaymentTransaction()
                    {
                        Id = 0,
                        TransactionId = Convert.ToInt64(reader.GetNullableDecimal("TMTXID").GetValueOrDefault(0)),
                        ClientCode = reader.GetTrimmedString("TMCLIC"),
                        BillingEntityCode = reader.GetTrimmedString("TMBILC"),
                        PaymentId = reader.GetTrimmedString("TMUSKY")
                    };
                }

                return result;
            }
        }
    }
}
